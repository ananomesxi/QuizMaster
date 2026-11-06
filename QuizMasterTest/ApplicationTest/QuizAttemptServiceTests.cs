using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuizMasterApplication.Services.Implementations;
using QuizMasterDomain.DTOs.QuizAttemptDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Enums;
using QuizMasterDomain.Interfaces;
using Xunit;

namespace QuizMasterTest.ApplicationTest
{
    public class QuizAttemptServiceTests
    {
        private const string StudentId = "student-1";
        private const int QuizId = 1;
        private const int AttemptId = 10;

        private static readonly CancellationToken Token = CancellationToken.None;

        private readonly Mock<IQuizAttemptRepository> _attempts = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<IQuestionRepository> _questions = new();
        private readonly Mock<IStudentAnswerRepository> _answers = new();
        private readonly Mock<IQuizResultRepository> _results = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly QuizAttemptService _service;

        public QuizAttemptServiceTests()
        {
            _service = new QuizAttemptService(
                _attempts.Object,
                _unitOfWork.Object,
                _quizzes.Object,
                _answers.Object,
                _questions.Object,
                _results.Object);

            _unitOfWork.Setup(unit => unit.SaveChangesAsync(Token))
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task StartAttemptAsync_CreatesAttempt_ForPublishedQuiz()
        {
            SetUpQuiz();
            _attempts.Setup(repo => repo.AddAsync(It.IsAny<QuizAttempt>(), Token))
                .Returns(Task.CompletedTask);

            var result = await _service.StartAttemptAsync(QuizId, StudentId, Token);

            Assert.Equal(QuizId, result.QuizId);
            Assert.Equal(QuizAttemptStatus.InProgress, result.Status);

            _attempts.Verify(repo => repo.AddAsync(
                It.Is<QuizAttempt>(attempt =>
                    attempt.QuizId == QuizId &&
                    attempt.UserId == StudentId &&
                    attempt.Status == QuizAttemptStatus.InProgress),
                Token), Times.Once);

            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task StartAttemptAsync_Throws_WhenQuizIsMissing()
        {
            _quizzes.Setup(repo => repo.GetByIdAsync(QuizId, Token))
                .ReturnsAsync((Quiz?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.StartAttemptAsync(QuizId, StudentId, Token));

            _attempts.Verify(repo => repo.AddAsync(It.IsAny<QuizAttempt>(), Token),
                Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task StartAttemptAsync_Throws_WhenQuizIsUnpublished()
        {
            SetUpQuiz(isPublished: false);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.StartAttemptAsync(QuizId, StudentId, Token));

            _attempts.Verify(repo => repo.AddAsync(It.IsAny<QuizAttempt>(), Token),
                Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task GetQuestionsForAttemptAsync_Throws_ForAnotherStudentsAttempt()
        {
            SetUpAttempt(studentId: "student-2");

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _service.GetQuestionsForAttemptAsync(AttemptId, StudentId, Token));

            _quizzes.Verify(repo => repo.GetByIdAsync(QuizId, Token), Times.Never);
        }

        [Fact]
        public async Task GetQuestionsForAttemptAsync_Throws_WhenTimeLimitHasExpired()
        {
            SetUpQuiz();
            SetUpAttempt(startedAt: DateTime.UtcNow.AddHours(-1));

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.GetQuestionsForAttemptAsync(AttemptId, StudentId, Token));

            Assert.Equal("The quiz time limit has expired.", exception.Message);
        }

        [Fact]
        public async Task GetQuestionsForAttemptAsync_ReturnsQuestionsAndOptions()
        {
            var quiz = SetUpQuiz();
            SetUpAttempt();

            quiz.Questions.Add(new Question
            {
                Id = 7,
                QuizId = QuizId,
                Text = "What is 2 + 2?",
                Points = 5,
                AnswerOptions = new List<AnswerOption>
                {
                    new()
                    {
                        Id = 3,
                        QuestionId = 7,
                        Text = "Four",
                        IsCorrect = true
                    }
                }
            });

            var question = Assert.Single(
                await _service.GetQuestionsForAttemptAsync(AttemptId, StudentId, Token));
            var option = Assert.Single(question.AnswerOptions);

            Assert.Equal(7, question.Id);
            Assert.Equal("What is 2 + 2?", question.Text);
            Assert.Equal(3, option.Id);
            Assert.Equal("Four", option.Text);
        }

        [Fact]
        public async Task SaveAnswerAsync_ReplacesPreviousAnswer_AndIgnoresDuplicateIds()
        {
            SetUpQuiz();
            SetUpAttempt();
            var question = CreateScoringQuestion(7);

            _questions.Setup(repo => repo.GetByIdAsync(question.Id, Token))
                .ReturnsAsync(question);

            var oldAnswer = new StudentAnswer
            {
                QuizAttemptId = AttemptId,
                QuestionId = question.Id,
                AnswerOptionId = 72
            };

            _answers.Setup(repo => repo.GetByQuizAttemptIdAsync(AttemptId, Token))
                .ReturnsAsync(new List<StudentAnswer> { oldAnswer });
            _answers.Setup(repo => repo.AddAsync(It.IsAny<StudentAnswer>(), Token))
                .Returns(Task.CompletedTask);

            var dto = new SaveAnswerDto
            {
                QuestionId = question.Id,
                AnswerOptionIds = new List<int> { 71, 71 }
            };

            await _service.SaveAnswerAsync(AttemptId, StudentId, dto, Token);

            _answers.Verify(repo => repo.Delete(oldAnswer), Times.Once);
            _answers.Verify(repo => repo.AddAsync(
                It.Is<StudentAnswer>(answer =>
                    answer.QuizAttemptId == AttemptId &&
                    answer.QuestionId == question.Id &&
                    answer.AnswerOptionId == 71),
                Token), Times.Once);
            _answers.Verify(repo => repo.AddAsync(It.IsAny<StudentAnswer>(), Token),
                Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task SaveAnswerAsync_Throws_WhenOptionDoesNotBelongToQuestion()
        {
            SetUpQuiz();
            SetUpAttempt();
            var question = CreateScoringQuestion(7);

            _questions.Setup(repo => repo.GetByIdAsync(question.Id, Token))
                .ReturnsAsync(question);

            var dto = new SaveAnswerDto
            {
                QuestionId = question.Id,
                AnswerOptionIds = new List<int> { 81 }
            };

            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.SaveAnswerAsync(AttemptId, StudentId, dto, Token));

            _answers.Verify(repo => repo.Delete(It.IsAny<StudentAnswer>()), Times.Never);
            _answers.Verify(repo => repo.AddAsync(It.IsAny<StudentAnswer>(), Token),
                Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task SaveAnswerAsync_Throws_WhenTimeLimitHasExpired()
        {
            SetUpQuiz();
            SetUpAttempt(startedAt: DateTime.UtcNow.AddHours(-1));

            var dto = new SaveAnswerDto
            {
                QuestionId = 7,
                AnswerOptionIds = new List<int> { 71 }
            };

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.SaveAnswerAsync(AttemptId, StudentId, dto, Token));

            Assert.Equal("The quiz time limit has expired.", exception.Message);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task FinishAttemptAsync_ScoresSavedAnswers_AndCompletesAttempt()
        {
            var quiz = SetUpQuiz();
            var attempt = SetUpAttempt();
            quiz.Questions.Add(CreateScoringQuestion(7));
            quiz.Questions.Add(CreateScoringQuestion(8));

            _answers.Setup(repo => repo.GetByQuizAttemptIdAsync(AttemptId, Token))
                .ReturnsAsync(new List<StudentAnswer>
                {
                    new()
                    {
                        QuizAttemptId = AttemptId,
                        QuestionId = 7,
                        AnswerOptionId = 71
                    },
                    new()
                    {
                        QuizAttemptId = AttemptId,
                        QuestionId = 8,
                        AnswerOptionId = 82
                    }
                });

            _results.Setup(repo => repo.AddAsync(It.IsAny<QuizResult>(), Token))
                .Returns(Task.CompletedTask);

            var result = await _service.FinishAttemptAsync(AttemptId, StudentId, Token);

            Assert.Equal(AttemptId, result.QuizAttemptId);
            Assert.Equal(5, result.Score);
            Assert.Equal(10, result.MaxScore);
            Assert.Equal(QuizAttemptStatus.Completed, attempt.Status);
            Assert.NotNull(attempt.FinishedAt);

            _results.Verify(repo => repo.AddAsync(
                It.Is<QuizResult>(saved =>
                    saved.QuizAttemptId == AttemptId &&
                    saved.Score == 5 &&
                    saved.MaxScore == 10),
                Token), Times.Once);
            _attempts.Verify(repo => repo.Update(attempt), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task FinishAttemptAsync_Throws_WhenAttemptIsAlreadyCompleted()
        {
            var attempt = SetUpAttempt();
            attempt.Status = QuizAttemptStatus.Completed;

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.FinishAttemptAsync(AttemptId, StudentId, Token));

            _results.Verify(repo => repo.AddAsync(It.IsAny<QuizResult>(), Token),
                Times.Never);
            _attempts.Verify(repo => repo.Update(It.IsAny<QuizAttempt>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        private Quiz SetUpQuiz(bool isPublished = true)
        {
            var quiz = new Quiz
            {
                Id = QuizId,
                Title = "Math quiz",
                TimeLimitMinutes = 15,
                IsPublished = isPublished
            };

            _quizzes.Setup(repo => repo.GetByIdAsync(QuizId, Token))
                .ReturnsAsync(quiz);

            return quiz;
        }

        private QuizAttempt SetUpAttempt(
            string studentId = StudentId,
            DateTime? startedAt = null)
        {
            var attempt = new QuizAttempt
            {
                Id = AttemptId,
                QuizId = QuizId,
                UserId = studentId,
                StartedAt = startedAt ?? DateTime.UtcNow,
                Status = QuizAttemptStatus.InProgress
            };

            _attempts.Setup(repo => repo.GetByIdAsync(AttemptId, Token))
                .ReturnsAsync(attempt);

            return attempt;
        }

        private static Question CreateScoringQuestion(int questionId)
        {
            return new Question
            {
                Id = questionId,
                QuizId = QuizId,
                Text = $"Question {questionId}",
                Points = 5,
                AnswerOptions = new List<AnswerOption>
                {
                    new()
                    {
                        Id = questionId * 10 + 1,
                        QuestionId = questionId,
                        Text = "Correct option",
                        IsCorrect = true
                    },
                    new()
                    {
                        Id = questionId * 10 + 2,
                        QuestionId = questionId,
                        Text = "Incorrect option",
                        IsCorrect = false
                    }
                }
            };
        }
    }
}