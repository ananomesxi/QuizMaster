using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuizMasterApplication.Services.Implementations;
using QuizMasterDomain.DTOs.QuestionDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using Xunit;

namespace QuizMasterTest.ApplicationTest
{
    public class QuestionServiceTests
    {
        private readonly Mock<IQuestionRepository> _questions = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<IQuizAttemptRepository> _attempts = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly QuestionService _service;

        private static readonly CancellationToken Token = CancellationToken.None;

        public QuestionServiceTests()
        {
            _service = new QuestionService(
                _questions.Object,
                _unitOfWork.Object,
                _quizzes.Object,
                _attempts.Object);

            _unitOfWork.Setup(unit => unit.SaveChangesAsync(Token)).ReturnsAsync(1);
        }

        [Fact]
        public async Task GetAllQuestionsAsync_ReturnsMappedQuestions()
        {
            var question = CreateQuestion();
            _questions.Setup(repo => repo.GetAllAsync(Token))
                .ReturnsAsync(new List<Question> { question });

            var result = Assert.Single(await _service.GetAllQuestionsAsync(Token));

            Assert.Equal(question.Id, result.Id);
            Assert.Equal(question.QuizId, result.QuizId);
            Assert.Equal(question.Text, result.Text);
            Assert.Equal(question.Points, result.Points);
            Assert.Equal(question.Type, result.Type);
        }

        [Fact]
        public async Task GetQuestionByIdAsync_ReturnsMatchingQuestion()
        {
            var question = CreateQuestion();
            _questions.Setup(repo => repo.GetByIdAsync(question.Id, Token))
                .ReturnsAsync(question);

            var result = await _service.GetQuestionByIdAsync(question.Id, Token);

            Assert.Equal(question.Id, result.Id);
            Assert.Equal(question.Text, result.Text);
        }

        [Fact]
        public async Task GetQuestionByIdAsync_Throws_WhenQuestionIsMissing()
        {
            _questions.Setup(repo => repo.GetByIdAsync(99, Token))
                .ReturnsAsync((Question?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetQuestionByIdAsync(99, Token));
        }

        [Fact]
        public async Task GetQuestionsByQuizIdAsync_ReturnsQuestionsFromRepository()
        {
            var question = CreateQuestion();
            _questions.Setup(repo => repo.GetByQuizIdAsync(question.QuizId, Token))
                .ReturnsAsync(new List<Question> { question });

            var result = Assert.Single(
                await _service.GetQuestionsByQuizIdAsync(question.QuizId, Token));

            Assert.Equal(question.Id, result.Id);
            Assert.Equal(question.QuizId, result.QuizId);
        }

        [Fact]
        public async Task AddQuestionAsync_SavesQuestion_WhenQuizIsEditable()
        {
            var question = CreateQuestion();
            SetUpQuiz();
            _questions.Setup(repo => repo.AddAsync(It.IsAny<Question>(), Token))
                .Returns(Task.CompletedTask);

            var dto = new CreateQuestionDto
            {
                Text = question.Text,
                Points = question.Points,
                Type = question.Type
            };

            var result = await _service.AddQuestionAsync(question.QuizId, dto, Token);

            Assert.Equal(question.QuizId, result.QuizId);
            Assert.Equal(dto.Text, result.Text);
            Assert.Equal(dto.Points, result.Points);
            Assert.Equal(dto.Type, result.Type);
            _questions.Verify(repo => repo.AddAsync(
                It.Is<Question>(added => added.QuizId == question.QuizId &&
                                         added.Text == dto.Text &&
                                         added.Points == dto.Points &&
                                         added.Type == dto.Type),
                Token), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task AddQuestionAsync_Throws_WhenQuizIsMissing()
        {
            _quizzes.Setup(repo => repo.GetByIdAsync(99, Token))
                .ReturnsAsync((Quiz?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.AddQuestionAsync(99, new CreateQuestionDto(), Token));

            VerifyNoWrites();
        }

        [Fact]
        public async Task UpdateQuestionAsync_SavesChanges_WhenQuizIsEditable()
        {
            var question = CreateQuestion();
            SetUpQuiz();
            _questions.Setup(repo => repo.GetByIdAsync(question.Id, Token))
                .ReturnsAsync(question);

            var dto = new UpdateQuestionDto
            {
                Text = "What is 3 + 3?",
                Points = 10,
                Type = question.Type
            };

            var result = await _service.UpdateQuestionAsync(question.Id, dto, Token);

            Assert.Equal(question.Id, result.Id);
            Assert.Equal(dto.Text, result.Text);
            Assert.Equal(dto.Points, result.Points);
            _questions.Verify(repo => repo.Update(question), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task DeleteQuestionAsync_DeletesAndSaves_WhenQuizIsEditable()
        {
            var question = CreateQuestion();
            SetUpQuiz();
            _questions.Setup(repo => repo.GetByIdAsync(question.Id, Token))
                .ReturnsAsync(question);

            await _service.DeleteQuestionAsync(question.Id, Token);

            _questions.Verify(repo => repo.Delete(question), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Theory]
        [InlineData("update")]
        [InlineData("delete")]
        public async Task EditQuestion_Throws_WhenQuestionIsMissing(string operation)
        {
            _questions.Setup(repo => repo.GetByIdAsync(99, Token))
                .ReturnsAsync((Question?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => EditQuestionAsync(operation, 99, quizId: 1));

            VerifyNoWrites();
        }

        [Theory]
        [InlineData("add", true, false)]
        [InlineData("update", true, false)]
        [InlineData("delete", true, false)]
        [InlineData("add", false, true)]
        [InlineData("update", false, true)]
        [InlineData("delete", false, true)]
        public async Task EditQuestion_Throws_WhenQuizIsPublishedOrHasAttempts(
            string operation, bool isPublished, bool hasAttempts)
        {
            var question = CreateQuestion();
            SetUpQuiz(isPublished, hasAttempts);
            _questions.Setup(repo => repo.GetByIdAsync(question.Id, Token))
                .ReturnsAsync(question);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => EditQuestionAsync(operation, question.Id, question.QuizId));

            VerifyNoWrites();
            Assert.Equal("What is 2 + 2?", question.Text);
            Assert.Equal(5, question.Points);
        }

        private Task EditQuestionAsync(string operation, int questionId, int quizId)
        {
            return operation switch
            {
                "add" => _service.AddQuestionAsync(quizId, new CreateQuestionDto
                {
                    Text = "New question",
                    Points = 10
                }, Token),
                "update" => _service.UpdateQuestionAsync(questionId, new UpdateQuestionDto
                {
                    Text = "Updated question",
                    Points = 10
                }, Token),
                "delete" => _service.DeleteQuestionAsync(questionId, Token),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
        }

        private void SetUpQuiz(bool isPublished = false, bool hasAttempts = false)
        {
            var quiz = new Quiz
            {
                Id = 1,
                Title = "Math quiz",
                TimeLimitMinutes = 15,
                IsPublished = isPublished
            };

            _quizzes.Setup(repo => repo.GetByIdAsync(quiz.Id, Token)).ReturnsAsync(quiz);
            _attempts.Setup(repo => repo.HasAttemptsForQuizAsync(quiz.Id, Token))
                .ReturnsAsync(hasAttempts);
        }

        private static Question CreateQuestion()
        {
            return new Question
            {
                Id = 7,
                QuizId = 1,
                Text = "What is 2 + 2?",
                Points = 5
            };
        }

        private void VerifyNoWrites()
        {
            _questions.Verify(repo => repo.AddAsync(
                It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Never);
            _questions.Verify(repo => repo.Update(It.IsAny<Question>()), Times.Never);
            _questions.Verify(repo => repo.Delete(It.IsAny<Question>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}