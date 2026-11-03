using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.QuestionDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Implementations
{
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuizAttemptRepository _quizAttemptRepository;
        public QuestionService(IQuestionRepository questionRepository, IUnitOfWork unitOfWork, IQuizRepository quizRepository, IQuizAttemptRepository quizAttemptRepository)
        {
            _questionRepository = questionRepository;
            _unitOfWork = unitOfWork;
            _quizRepository = quizRepository;
            _quizAttemptRepository = quizAttemptRepository;
        }


        public async Task<ICollection<QuestionDto>> GetAllQuestionsAsync(CancellationToken cancellationToken)
        {
            var questions = await _questionRepository.GetAllAsync(cancellationToken);
            return questions.Select(MapToDto).ToList();
        }

        public async Task<QuestionDto> GetQuestionByIdAsync(int id, CancellationToken cancellationToken)
        {
            var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
            if (question == null)
            {
                throw new KeyNotFoundException($"Question with ID {id} not found.");
            }
            return MapToDto(question);
        }
        public async Task<ICollection<QuestionDto>> GetQuestionsByQuizIdAsync(int quizId, CancellationToken cancellationToken)
        {
            var questions = await _questionRepository.GetByQuizIdAsync(quizId, cancellationToken);
            return questions.Select(MapToDto).ToList();
        }
        public async Task<QuestionDto> AddQuestionAsync(int quizId, CreateQuestionDto createQuestionDto, CancellationToken cancellationToken)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {quizId} not found.");
            }
            await EnsureQuizIsEditableAsync(quiz, cancellationToken);
            var question = new QuizMasterDomain.Entities.Question
            {
                QuizId = quizId,
                Text = createQuestionDto.Text,
                Points = createQuestionDto.Points,
                Type = createQuestionDto.Type
            };
            await _questionRepository.AddAsync(question, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(question);
        }
        public async Task<QuestionDto> UpdateQuestionAsync(int id, UpdateQuestionDto updateQuestionDto, CancellationToken cancellationToken)
        {

            var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
            if (question == null)
            {
                throw new KeyNotFoundException($"Question with ID {id} not found.");
            }
            var quiz = await _quizRepository.GetByIdAsync(question.QuizId, cancellationToken);

            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {question.QuizId} not found.");
            }

            await EnsureQuizIsEditableAsync(quiz, cancellationToken);
            question.Text = updateQuestionDto.Text;
            question.Points = updateQuestionDto.Points;
            question.Type = updateQuestionDto.Type;
            _questionRepository.Update(question);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(question);
        }
        public async Task DeleteQuestionAsync(int id, CancellationToken cancellationToken)
        {
            var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
            if (question == null)
            {
                throw new KeyNotFoundException($"Question with ID {id} not found.");
            }
            var quiz = await _quizRepository.GetByIdAsync(question.QuizId, cancellationToken);

            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {question.QuizId} not found.");
            }

            await EnsureQuizIsEditableAsync(quiz, cancellationToken);
            _questionRepository.Delete(question);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        private QuestionDto MapToDto(Question question)
        {
            return new QuestionDto
            {
                Id = question.Id,
                QuizId = question.QuizId,
                Text = question.Text,
                Points = question.Points,
                Type = question.Type
            };
        }
        private async Task EnsureQuizIsEditableAsync(Quiz quiz, CancellationToken cancellationToken)
        {
            if (quiz.IsPublished)
            {
                throw new InvalidOperationException("You can't change content in a published quiz.");
            }
            var hasAttempts = await _quizAttemptRepository.HasAttemptsForQuizAsync(quiz.Id, cancellationToken);
            if (hasAttempts)
            {
                throw new InvalidOperationException("You can't change quiz content after an attempt has started.");
            }
        }
    }
}
