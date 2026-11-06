using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuizMasterApplication.Services.Implementations;
using QuizMasterDomain.DTOs.AnswerOptionDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using Xunit;

namespace QuizMasterTest.ApplicationTest
{
    public class AnswerOptionServiceTests
    {
        private readonly Mock<IAnswerOptionRepository> _options = new();
        private readonly Mock<IQuestionRepository> _questions = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly Mock<IQuizAttemptRepository> _attempts = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly AnswerOptionService _service;

        private static readonly CancellationToken Token = CancellationToken.None;

        public AnswerOptionServiceTests()
        {
            _service = new AnswerOptionService(
                _options.Object,
                _unitOfWork.Object,
                _questions.Object,
                _quizzes.Object,
                _attempts.Object);

            _unitOfWork.Setup(unit => unit.SaveChangesAsync(Token))
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task GetByQuestionIdAsync_ReturnsMappedOptions()
        {
            var option = CreateOption();
            SetUpQuestion();

            _options.Setup(repo => repo.GetByQuestionIdAsync(option.QuestionId, Token))
                .ReturnsAsync(new List<AnswerOption> { option });

            var result = Assert.Single(
                await _service.GetByQuestionIdAsync(option.QuestionId, Token));

            Assert.Equal(option.Id, result.Id);
            Assert.Equal(option.QuestionId, result.QuestionId);
            Assert.Equal(option.Text, result.Text);
            Assert.Equal(option.IsCorrect, result.IsCorrect);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMatchingOption()
        {
            var option = CreateOption();
            _options.Setup(repo => repo.GetByIdAsync(option.Id, Token))
                .ReturnsAsync(option);

            var result = await _service.GetByIdAsync(option.Id, Token);

            Assert.Equal(option.Id, result.Id);
            Assert.Equal(option.Text, result.Text);
            Assert.True(result.IsCorrect);
        }

        [Fact]
        public async Task GetByIdAsync_Throws_WhenOptionIsMissing()
        {
            _options.Setup(repo => repo.GetByIdAsync(99, Token))
                .ReturnsAsync((AnswerOption?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetByIdAsync(99, Token));
        }

        [Fact]
        public async Task AddAsync_SavesOption_WhenQuizIsEditable()
        {
            SetUpQuestion();
            SetUpQuiz();
            _options.Setup(repo => repo.AddAsync(It.IsAny<AnswerOption>(), Token))
                .Returns(Task.CompletedTask);

            var dto = new CreateAnswerOptionDto
            {
                Text = "Four",
                IsCorrect = true
            };

            var result = await _service.AddAsync(7, dto, Token);

            Assert.Equal(7, result.QuestionId);
            Assert.Equal(dto.Text, result.Text);
            Assert.True(result.IsCorrect);
            _options.Verify(repo => repo.AddAsync(
                It.Is<AnswerOption>(option =>
                    option.QuestionId == 7 &&
                    option.Text == dto.Text &&
                    option.IsCorrect),
                Token), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_SavesChanges_WhenQuizIsEditable()
        {
            var option = CreateOption();
            SetUpQuestion();
            SetUpQuiz();
            _options.Setup(repo => repo.GetByIdAsync(option.Id, Token))
                .ReturnsAsync(option);

            var dto = new UpdateAnswerOptionDto
            {
                Text = "Five",
                IsCorrect = false
            };

            var result = await _service.UpdateAsync(option.Id, dto, Token);

            Assert.Equal(option.Id, result.Id);
            Assert.Equal("Five", result.Text);
            Assert.False(result.IsCorrect);
            _options.Verify(repo => repo.Update(option), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_DeletesAndSaves_WhenQuizIsEditable()
        {
            var option = CreateOption();
            SetUpQuestion();
            SetUpQuiz();
            _options.Setup(repo => repo.GetByIdAsync(option.Id, Token))
                .ReturnsAsync(option);

            await _service.DeleteAsync(option.Id, Token);

            _options.Verify(repo => repo.Delete(option), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Theory]
        [InlineData("add", true, false)]
        [InlineData("update", true, false)]
        [InlineData("delete", true, false)]
        [InlineData("add", false, true)]
        [InlineData("update", false, true)]
        [InlineData("delete", false, true)]
        public async Task EditOption_Throws_WhenQuizIsPublishedOrHasAttempts(
            string operation, bool isPublished, bool hasAttempts)
        {
            var option = CreateOption();
            SetUpQuestion();
            SetUpQuiz(isPublished, hasAttempts);
            _options.Setup(repo => repo.GetByIdAsync(option.Id, Token))
                .ReturnsAsync(option);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => EditOptionAsync(operation, option));

            _options.Verify(repo => repo.AddAsync(
                It.IsAny<AnswerOption>(), It.IsAny<CancellationToken>()), Times.Never);
            _options.Verify(repo => repo.Update(It.IsAny<AnswerOption>()), Times.Never);
            _options.Verify(repo => repo.Delete(It.IsAny<AnswerOption>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(
                It.IsAny<CancellationToken>()), Times.Never);

            Assert.Equal("Four", option.Text);
            Assert.True(option.IsCorrect);
        }

        private Task EditOptionAsync(string operation, AnswerOption option)
        {
            return operation switch
            {
                "add" => _service.AddAsync(option.QuestionId, new CreateAnswerOptionDto
                {
                    Text = "Five",
                    IsCorrect = false
                }, Token),
                "update" => _service.UpdateAsync(option.Id, new UpdateAnswerOptionDto
                {
                    Text = "Five",
                    IsCorrect = false
                }, Token),
                "delete" => _service.DeleteAsync(option.Id, Token),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
        }

        private void SetUpQuestion()
        {
            var question = new Question
            {
                Id = 7,
                QuizId = 1,
                Text = "What is 2 + 2?",
                Points = 5
            };

            _questions.Setup(repo => repo.GetByIdAsync(question.Id, Token))
                .ReturnsAsync(question);
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

            _quizzes.Setup(repo => repo.GetByIdAsync(quiz.Id, Token))
                .ReturnsAsync(quiz);
            _attempts.Setup(repo => repo.HasAttemptsForQuizAsync(quiz.Id, Token))
                .ReturnsAsync(hasAttempts);
        }

        private static AnswerOption CreateOption()
        {
            return new AnswerOption
            {
                Id = 3,
                QuestionId = 7,
                Text = "Four",
                IsCorrect = true
            };
        }
    }
}