using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.Quiz;
using QuizMasterDomain.DTOs.QuizDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using QuizMasterDomain.Enums;

namespace QuizMasterApplication.Services.Implementations
{
    public class QuizService : IQuizService
    {
        private readonly IQuizRepository _quizRepository;
        private readonly IQuizAttemptRepository _quizAttemptRepository;
        private readonly IUnitOfWork _unitOfWork;
        public QuizService(IQuizRepository quizRepository, IUnitOfWork unitOfWork, IQuizAttemptRepository quizAttemptRepository)
        {
            _quizRepository = quizRepository;
            _unitOfWork = unitOfWork;
            _quizAttemptRepository = quizAttemptRepository;
        }

        public async Task<ICollection<QuizDto>> GetAllQuizzesAsync(CancellationToken cancellationToken)
        {
            var quizzes = await _quizRepository.GetAllAsync(cancellationToken);
            return quizzes.Select(MapToDto).ToList();
        }

        public async Task<QuizDto> GetQuizByIdAsync(int id, CancellationToken cancellationToken)
        {
            var quiz = await _quizRepository.GetByIdAsync(id, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {id} not found.");
            }
            return MapToDto(quiz);
        }
        public async Task<QuizDto> AddQuizAsync(CreateQuizDto createQuizDto, CancellationToken cancellationToken, string? createdById = null)
        {
            if (createdById != null && string.IsNullOrWhiteSpace(createdById))
            {
                throw new ArgumentException("Creator ID cannot be empty.");
            } 
            var quiz = new Quiz
            {
                Title = createQuizDto.Title,
                TimeLimitMinutes = createQuizDto.TimeLimitMinutes,
                IsPublished = false,
                CreatedById = createdById
            }; 
            await _quizRepository.AddAsync(quiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken); 
            if (createdById != null)
            {
                var savedQuiz = await _quizRepository.GetByIdAsync( quiz.Id, cancellationToken); 
                if (savedQuiz != null)
                {
                    return MapToDto(savedQuiz);
                }
            } 
            return MapToDto(quiz);
        }
        public async Task<QuizDto> UpdateQuizAsync(int id, UpdateQuizDto updateQuizDto, CancellationToken cancellationToken)
        {
            var quiz = await _quizRepository.GetByIdAsync(id, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {id} not found.");
            }
            quiz.Title = updateQuizDto.Title;
            quiz.TimeLimitMinutes = updateQuizDto.TimeLimitMinutes;
            _quizRepository.Update(quiz);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(quiz);
        }
        public async Task<bool> DeleteQuizAsync(int id, CancellationToken cancellationToken)
        {
            var quiz = await _quizRepository.GetByIdAsync(id, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {id} not found.");
            }
            if (await _quizAttemptRepository.HasAttemptsForQuizAsync(quiz.Id, cancellationToken))
            {
                throw new InvalidOperationException($"Quiz with ID {id} has existing attempts and cannot be deleted.");
            }
            _quizRepository.Delete(quiz);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<bool> PublishQuizAsync(int id, CancellationToken cancellationToken)
        {
            var quiz = await _quizRepository.GetByIdAsync( id, cancellationToken); 
            if (quiz == null)
            {
                throw new KeyNotFoundException( $"Quiz with ID {id} not found.");
            } 
            foreach (var question in quiz.Questions)
            {
                if (question.Type != QuestionType.TrueFalse)
                {
                    continue;
                } 
                var options = question.AnswerOptions; 
                var hasTrue = options.Any(option => option.Text == "True");
                var hasFalse = options.Any(option => option.Text == "False");
                var correctCount = options.Count(option => option.IsCorrect); 
                if (options.Count != 2 || !hasTrue || !hasFalse || correctCount != 1)
                {
                    throw new InvalidOperationException( $"True/False question with ID {question.Id} " + "must have exactly two options, True and False, " + "with exactly one correct answer.");
                }
            } 
            quiz.IsPublished = true; 
            _quizRepository.Update(quiz); 
            await _unitOfWork.SaveChangesAsync(cancellationToken); 
            return true;
        }
        public async Task<bool> UnpublishQuizAsync(int id, CancellationToken cancellationToken)
        {
            var quiz = await _quizRepository.GetByIdAsync(id, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {id} not found.");
            }
            quiz.IsPublished = false;
            _quizRepository.Update(quiz);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        public async Task<ICollection<QuizDto>> GetPublishedQuizzesAsync(CancellationToken cancellationToken)
        {
            var quizzes = await _quizRepository.GetPublishedQuizzesAsync(cancellationToken);
            return quizzes.Select(MapToDto).ToList();
        }
        private static QuizDto MapToDto(Quiz quiz)
        {
            return new QuizDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                IsPublished = quiz.IsPublished,
                CreatedById = quiz.CreatedById,
                CreatedByUserName = quiz.CreatedBy?.UserName,
                CreatedByProfileImageUrl = quiz.CreatedBy?.ProfileImageUrl
            };
        }
    }
}
