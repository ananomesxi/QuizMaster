using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.Quiz;
using QuizMasterDomain.DTOs.QuizDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

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
        public async Task<QuizDto> AddQuizAsync(CreateQuizDto createQuizDto, CancellationToken cancellationToken)
        {
            var quiz = new Quiz
            {
                Title = createQuizDto.Title,
                TimeLimitMinutes = createQuizDto.TimeLimitMinutes,
                IsPublished = false
            };
            await _quizRepository.AddAsync(quiz, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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
            var quiz = await _quizRepository.GetByIdAsync(id, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {id} not found.");
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
                IsPublished = quiz.IsPublished
            };
        }
    }
}
