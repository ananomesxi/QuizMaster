using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.QuizAttemptDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Enums;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Implementations
{
    public class QuizAttemptService : IQuizAttemptService
    {
        private readonly IQuizAttemptRepository _quizAttemptRepository;
        private readonly IQuizRepository _quizRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IStudentAnswerRepository _studentAnswerRepository;
        private readonly IQuizResultRepository _quizResultRepository;
        private readonly IUnitOfWork _unitOfWork;
        public QuizAttemptService(
            IQuizAttemptRepository quizAttemptRepository,
            IUnitOfWork unitOfWork,
            IQuizRepository quizRepository,
            IStudentAnswerRepository studentAnswerRepository,
            IQuestionRepository questionRepository,
            IQuizResultRepository quizResultRepository)
        {
            _quizAttemptRepository = quizAttemptRepository;
            _unitOfWork = unitOfWork;
            _quizRepository = quizRepository;
            _questionRepository = questionRepository;
            _studentAnswerRepository = studentAnswerRepository;
            _quizResultRepository = quizResultRepository;
        }

        public async Task<QuizAttemptDto> StartAttemptAsync(int quizId, string studentId, CancellationToken cancellationToken)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {quizId} not found.");
            }
            if (!quiz.IsPublished)
            {
                throw new InvalidOperationException("Quiz is not published.");
            }
            var attempt = new QuizAttempt
            {
                QuizId = quizId,
                UserId = studentId,
                StartedAt = DateTime.UtcNow,
                Status = QuizAttemptStatus.InProgress
            };

            await _quizAttemptRepository.AddAsync(attempt, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new QuizAttemptDto
            {
                Id = attempt.Id,
                QuizId = attempt.QuizId,
                StartedAt = attempt.StartedAt,
                Status = attempt.Status,
                AnsweredQuestions = 0,
                TotalQuestions = quiz.Questions.Count
            };
        }
        public async Task<ICollection<AttemptQuestionDto>> GetQuestionsForAttemptAsync(int attemptId, string studentId, CancellationToken cancellationToken)
        {
            var attempt = await _quizAttemptRepository.GetByIdAsync(attemptId, cancellationToken);
            if (attempt == null)
            {
                throw new KeyNotFoundException($"Attempt with ID {attemptId} not found.");
            }
            if (attempt.UserId != studentId)
            {
                throw new UnauthorizedAccessException("This attempt doesn't belong to you.");
            }
            if (attempt.Status != QuizAttemptStatus.InProgress)
            {
                throw new InvalidOperationException("This attempt is no longer in progress.");
            }
            
            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {attempt.QuizId} not found.");
            }
            if (DateTime.UtcNow >= attempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes))
            {
                throw new InvalidOperationException("The quiz time limit has expired.");
            }
            return quiz.Questions.Select(question => new AttemptQuestionDto
            {
                Id = question.Id,
                Text = question.Text,
                Type = question.Type,
                AnswerOptions = question.AnswerOptions.Select(option => new AttemptOptionDto
                {
                    Id = option.Id,
                    Text = option.Text
                }).ToList()
            }).ToList();
        }
        public async Task SaveAnswerAsync(int attemptId, string studentId, SaveAnswerDto dto, CancellationToken cancellationToken)
        {
            var attempt = await _quizAttemptRepository.GetByIdAsync(attemptId, cancellationToken);
            if (attempt == null)
            {
                throw new KeyNotFoundException($"Attempt with ID {attemptId} not found.");
            }
            if (attempt.UserId != studentId)
            {
                throw new UnauthorizedAccessException("This attempt doesn't belong to you.");
            }
            if (attempt.Status == QuizAttemptStatus.Completed)
            {
                throw new InvalidOperationException("This attempt is no longer in progress.");
            }
            var quiz = await _quizRepository.GetByIdAsync(attempt.QuizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {attempt.QuizId} not found.");
            }
            if (DateTime.UtcNow >= attempt.StartedAt.AddMinutes(quiz.TimeLimitMinutes))
            {
                throw new InvalidOperationException("The quiz time limit has expired.");
            }
            var question = await _questionRepository.GetByIdAsync(dto.QuestionId, cancellationToken);
            if (question == null || question.QuizId != attempt.QuizId)
            {
                throw new ArgumentException("That question isn't part of this quiz.");
            }
            var selectedIds = dto.AnswerOptionIds.Distinct().ToList();
            var validIds = question.AnswerOptions.Select(option => option.Id).ToList();

            if (selectedIds.Any(id => !validIds.Contains(id)))
            {
                throw new ArgumentException("One or more selected options don't belong to this question.");
            }

            if (question.Type != QuestionType.MultipleChoice && selectedIds.Count > 1)
            {
                throw new ArgumentException("Select at most one option for this question.");
            }

            var existingAnswers = await _studentAnswerRepository.GetByQuizAttemptIdAsync(attemptId, cancellationToken);
            foreach (var answer in existingAnswers.Where(a => a.QuestionId == question.Id))
            {
                _studentAnswerRepository.Delete(answer);
            }
            foreach (var optionId in selectedIds)
            {
                var studentAnswer = new StudentAnswer
                {
                    QuizAttemptId = attemptId,
                    QuestionId = question.Id,
                    AnswerOptionId = optionId
                };
                await _studentAnswerRepository.AddAsync(studentAnswer, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        public async Task<QuizResultDto> FinishAttemptAsync(int attemptId, string studentId, CancellationToken cancellationToken)
        {
            var attempt = await _quizAttemptRepository.GetByIdAsync(attemptId, cancellationToken);

            if (attempt == null)
            {
                throw new KeyNotFoundException($"Attempt with ID {attemptId} not found.");
            }

            if (attempt.UserId != studentId)
            {
                throw new UnauthorizedAccessException("This attempt doesn't belong to you.");
            }

            if (attempt.Status != QuizAttemptStatus.InProgress)
            {
                throw new InvalidOperationException("This attempt is no longer in progress.");
            }

            var quiz = await _quizRepository.GetByIdAsync(
                attempt.QuizId, cancellationToken);

            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {attempt.QuizId} not found.");
            }

            var studentAnswers = await _studentAnswerRepository.GetByQuizAttemptIdAsync(
                attemptId, cancellationToken);
            var score = 0;
            var maxScore = quiz.Questions.Sum(question => question.Points);
            foreach (var question in quiz.Questions)
            {
                var selectedIds = studentAnswers
                    .Where(answer => answer.QuestionId == question.Id)
                    .Select(answer => answer.AnswerOptionId)
                    .Distinct()
                    .OrderBy(id => id)
                    .ToList();
                var correctIds = question.AnswerOptions
                    .Where(option => option.IsCorrect)
                    .Select(option => option.Id)
                    .OrderBy(id => id)
                    .ToList();

                if (correctIds.Count > 0 && selectedIds.SequenceEqual(correctIds))
                {
                    score += question.Points;
                }
            }
            attempt.Status = QuizAttemptStatus.Completed;
            attempt.FinishedAt = DateTime.UtcNow;
            var result = new QuizResult
            {
                QuizAttemptId = attempt.Id,
                Score = score,
                MaxScore = maxScore
            };

            await _quizResultRepository.AddAsync(result, cancellationToken);
            _quizAttemptRepository.Update(attempt);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new QuizResultDto
            {
                QuizAttemptId = result.QuizAttemptId,
                Score = result.Score,
                MaxScore = result.MaxScore
            };
        }
        public async Task<ICollection<QuizAttemptDto>> GetMyAttemptsAsync( string studentId, CancellationToken cancellationToken)
        {
            var attempts = await _quizAttemptRepository.GetByUserIdAsync( studentId, cancellationToken);

            return attempts.Select(attempt => new QuizAttemptDto
            {
                Id = attempt.Id,
                QuizId = attempt.QuizId,
                StartedAt = attempt.StartedAt,
                Status = attempt.Status,
                AnsweredQuestions = attempt.StudentAnswers.Select(answer => answer.QuestionId).Distinct().Count(),
                TotalQuestions = attempt.Quiz.Questions.Count
            }).ToList();
        }
    }
}
