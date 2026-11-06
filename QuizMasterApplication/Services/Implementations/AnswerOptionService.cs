using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AnswerOptionDtos;
using QuizMasterDomain.DTOs.QuestionDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Text;

namespace QuizMasterApplication.Services.Implementations
{
    public class AnswerOptionService : IAnswerOptionService
    {
        private readonly IAnswerOptionRepository _answerOptionRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuizAttemptRepository _quizAttemptRepository;
        public AnswerOptionService (IAnswerOptionRepository answerOptionRepository, IUnitOfWork unitOfWork, IQuestionRepository questionRepository, IQuizRepository quizRepository, IQuizAttemptRepository quizAttemptRepository)
        {
            _answerOptionRepository = answerOptionRepository;
            _unitOfWork = unitOfWork;
            _questionRepository = questionRepository;
            _quizRepository = quizRepository;
            _quizAttemptRepository = quizAttemptRepository;
        }
        public async Task<ICollection<AnswerOptionDto>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken)
        {
            var question = await _questionRepository.GetByIdAsync(questionId, cancellationToken);
            if (question == null)
            {
                throw new KeyNotFoundException($"Question with ID {questionId} not found.");
            }
            var options = await _answerOptionRepository.GetByQuestionIdAsync(questionId, cancellationToken);
            return options.Select(MapToDto).ToList();   
        }

        public async Task<AnswerOptionDto> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var option = await _answerOptionRepository.GetByIdAsync(id, cancellationToken);
            if (option == null)
            {
                throw new KeyNotFoundException($"Answer option with ID {id} not found.");
            }
            return MapToDto(option);
        }
        
        public async Task<AnswerOptionDto> AddAsync(int questionId, CreateAnswerOptionDto createAnswerOptionDto, CancellationToken cancellationToken)
        {
            var question = await _questionRepository.GetByIdAsync(questionId, cancellationToken);
            if (question == null)
            {
                throw new KeyNotFoundException($"Question with ID {questionId} not found.");
            }
            var quiz = await _quizRepository.GetByIdAsync(question.QuizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {question.QuizId} not found.");
            }
            await EnsureQuizIsEditableAsync(quiz, cancellationToken);
            var option = new AnswerOption
            {
                QuestionId = questionId,
                Text = createAnswerOptionDto.Text,
                IsCorrect = createAnswerOptionDto.IsCorrect
            };
            await _answerOptionRepository.AddAsync(option, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(option);
        }
        public async Task<AnswerOptionDto> UpdateAsync(int id, UpdateAnswerOptionDto updateAnswerOptionDto, CancellationToken cancellationToken)
        {
            var option = await _answerOptionRepository.GetByIdAsync(id, cancellationToken);
            if (option == null)
            {
                throw new KeyNotFoundException($"Answer option with ID {id} not found.");
            }
            var question = await _questionRepository.GetByIdAsync(option.QuestionId, cancellationToken);

            if (question == null)
            {
                throw new KeyNotFoundException(
                    $"Question with ID {option.QuestionId} not found.");
            }

            var quiz = await _quizRepository.GetByIdAsync(question.QuizId, cancellationToken);

            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {question.QuizId} not found.");
            }

            await EnsureQuizIsEditableAsync(quiz, cancellationToken);
            option.Text = updateAnswerOptionDto.Text;
            option.IsCorrect = updateAnswerOptionDto.IsCorrect;
            _answerOptionRepository.Update(option);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(option);
        }
        public async Task DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var option = await _answerOptionRepository.GetByIdAsync(id, cancellationToken);
            if (option == null)
            {
                throw new KeyNotFoundException($"Answer option with ID {id} not found.");
            }
            var question = await _questionRepository.GetByIdAsync(option.QuestionId, cancellationToken);

            if (question == null)
            {
                throw new KeyNotFoundException(
                    $"Question with ID {option.QuestionId} not found.");
            }

            var quiz = await _quizRepository.GetByIdAsync(
                question.QuizId, cancellationToken);

            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {question.QuizId} not found.");
            }

            await EnsureQuizIsEditableAsync(quiz, cancellationToken);
            _answerOptionRepository.Delete(option);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        private AnswerOptionDto MapToDto(AnswerOption answerOption)
        {
            return new AnswerOptionDto
            {
                Id = answerOption.Id,
                Text = answerOption.Text,
                IsCorrect = answerOption.IsCorrect,
                QuestionId = answerOption.QuestionId
            };
        }

        private async Task EnsureQuizIsEditableAsync( Quiz quiz, CancellationToken cancellationToken)
        {
            if (quiz.IsPublished)
            {
                throw new InvalidOperationException( "You can't change content in a published quiz.");
            }
            var hasAttempts = await _quizAttemptRepository.HasAttemptsForQuizAsync( quiz.Id, cancellationToken);
            if (hasAttempts)
            {
                throw new InvalidOperationException( "You can't change quiz content after an attempt has started.");
            }
        }

    }
}
