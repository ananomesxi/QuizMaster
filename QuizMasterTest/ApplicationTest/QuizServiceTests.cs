using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuizMasterApplication.Services.Implementations;
using QuizMasterDomain.DTOs.QuizDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using Xunit;

namespace QuizMasterTest.ApplicationTest
{
    public class QuizServiceTests
    {
        private const int MissingQuizId = 99;
        private static readonly CancellationToken Token = CancellationToken.None;

        private readonly Mock<IQuizRepository> _quizRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IQuizAttemptRepository> _quizAttemptRepository = new();
        private readonly QuizService _service;

        public QuizServiceTests()
        {
            _service = new QuizService(
                _quizRepository.Object,
                _unitOfWork.Object,
                _quizAttemptRepository.Object);

            _unitOfWork.Setup(unit => unit.SaveChangesAsync(Token))
                .ReturnsAsync(1);
        }

        [Fact]
        public async Task GetAllQuizzesAsync_ReturnsMappedQuizDtos()
        {
            var first = CreateQuiz(1, "Biology", 20, isPublished: true);
            var second = CreateQuiz(2, "History", 15);

            _quizRepository.Setup(repo => repo.GetAllAsync(Token))
                .ReturnsAsync(new List<Quiz> { first, second });

            var result = await _service.GetAllQuizzesAsync(Token);

            Assert.Collection(result,
                quiz =>
                {
                    Assert.Equal(1, quiz.Id);
                    Assert.Equal("Biology", quiz.Title);
                    Assert.Equal(20, quiz.TimeLimitMinutes);
                    Assert.True(quiz.IsPublished);
                },
                quiz =>
                {
                    Assert.Equal(2, quiz.Id);
                    Assert.Equal("History", quiz.Title);
                    Assert.Equal(15, quiz.TimeLimitMinutes);
                    Assert.False(quiz.IsPublished);
                });
        }

        [Fact]
        public async Task GetQuizByIdAsync_ReturnsQuizDto_WhenQuizExists()
        {
            var quiz = CreateQuiz(3, "Geography", 25, isPublished: true);
            SetUpQuizLookup(quiz.Id, quiz);

            var result = await _service.GetQuizByIdAsync(quiz.Id, Token);

            Assert.NotNull(result);
            Assert.Equal(quiz.Id, result.Id);
            Assert.Equal(quiz.Title, result.Title);
            Assert.Equal(quiz.TimeLimitMinutes, result.TimeLimitMinutes);
            Assert.True(result.IsPublished);
        }

        [Fact]
        public async Task GetQuizByIdAsync_Throws_WhenQuizDoesNotExist()
        {
            SetUpQuizLookup(MissingQuizId, null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetQuizByIdAsync(MissingQuizId, Token));
        }

        [Fact]
        public async Task AddQuizAsync_CreatesQuizAsUnpublished_AndSavesChanges()
        {
            var dto = new CreateQuizDto
            {
                Title = "Practice quiz",
                TimeLimitMinutes = 15
            };

            _quizRepository.Setup(repo => repo.AddAsync(It.IsAny<Quiz>(), Token))
                .Returns(Task.CompletedTask);

            var result = await _service.AddQuizAsync(dto, Token);

            Assert.Equal(dto.Title, result.Title);
            Assert.Equal(dto.TimeLimitMinutes, result.TimeLimitMinutes);
            Assert.False(result.IsPublished);

            _quizRepository.Verify(repo => repo.AddAsync(
                It.Is<Quiz>(quiz =>
                    quiz.Title == dto.Title &&
                    quiz.TimeLimitMinutes == dto.TimeLimitMinutes &&
                    !quiz.IsPublished),
                Token), Times.Once);

            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizAsync_UpdatesQuiz_WhenQuizExists()
        {
            var quiz = CreateQuiz(4, "Old title", 10, isPublished: true);
            SetUpQuizLookup(quiz.Id, quiz);

            var dto = new UpdateQuizDto
            {
                Title = "New title",
                TimeLimitMinutes = 30
            };

            var result = await _service.UpdateQuizAsync(quiz.Id, dto, Token);

            Assert.NotNull(result);
            Assert.Equal(quiz.Id, result.Id);
            Assert.Equal(dto.Title, result.Title);
            Assert.Equal(dto.TimeLimitMinutes, result.TimeLimitMinutes);
            Assert.True(result.IsPublished);

            _quizRepository.Verify(repo => repo.Update(quiz), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task UpdateQuizAsync_Throws_WhenQuizDoesNotExist()
        {
            SetUpQuizLookup(MissingQuizId, null);

            var dto = new UpdateQuizDto
            {
                Title = "New title",
                TimeLimitMinutes = 30
            };

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UpdateQuizAsync(MissingQuizId, dto, Token));

            _quizRepository.Verify(repo => repo.Update(It.IsAny<Quiz>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task DeleteQuizAsync_Throws_WhenQuizDoesNotExist()
        {
            SetUpQuizLookup(MissingQuizId, null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.DeleteQuizAsync(MissingQuizId, Token));

            _quizAttemptRepository.Verify(repo => repo.HasAttemptsForQuizAsync(
                It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            _quizRepository.Verify(repo => repo.Delete(It.IsAny<Quiz>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task DeleteQuizAsync_Throws_WhenQuizHasAttempts()
        {
            var quiz = CreateQuiz(5, "Quiz with attempts", 20);
            SetUpQuizLookup(quiz.Id, quiz);

            _quizAttemptRepository.Setup(repo => repo.HasAttemptsForQuizAsync(
                quiz.Id, Token)).ReturnsAsync(true);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.DeleteQuizAsync(quiz.Id, Token));

            _quizRepository.Verify(repo => repo.Delete(It.IsAny<Quiz>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task DeleteQuizAsync_DeletesQuiz_WhenItHasNoAttempts()
        {
            var quiz = CreateQuiz(6, "Quiz without attempts", 20);
            SetUpQuizLookup(quiz.Id, quiz);

            _quizAttemptRepository.Setup(repo => repo.HasAttemptsForQuizAsync(
                quiz.Id, Token)).ReturnsAsync(false);

            var result = await _service.DeleteQuizAsync(quiz.Id, Token);

            Assert.True(result);
            _quizRepository.Verify(repo => repo.Delete(quiz), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task PublishQuizAsync_PublishesQuiz_WhenQuizExists()
        {
            var quiz = CreateQuiz(7, "Draft quiz", 20);
            SetUpQuizLookup(quiz.Id, quiz);

            var result = await _service.PublishQuizAsync(quiz.Id, Token);

            Assert.True(result);
            Assert.True(quiz.IsPublished);
            _quizRepository.Verify(repo => repo.Update(quiz), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task PublishQuizAsync_Throws_WhenQuizDoesNotExist()
        {
            SetUpQuizLookup(MissingQuizId, null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.PublishQuizAsync(MissingQuizId, Token));

            _quizRepository.Verify(repo => repo.Update(It.IsAny<Quiz>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task UnpublishQuizAsync_UnpublishesQuiz_WhenQuizExists()
        {
            var quiz = CreateQuiz(8, "Published quiz", 20, isPublished: true);
            SetUpQuizLookup(quiz.Id, quiz);

            var result = await _service.UnpublishQuizAsync(quiz.Id, Token);

            Assert.True(result);
            Assert.False(quiz.IsPublished);
            _quizRepository.Verify(repo => repo.Update(quiz), Times.Once);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Once);
        }

        [Fact]
        public async Task UnpublishQuizAsync_Throws_WhenQuizDoesNotExist()
        {
            SetUpQuizLookup(MissingQuizId, null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.UnpublishQuizAsync(MissingQuizId, Token));

            _quizRepository.Verify(repo => repo.Update(It.IsAny<Quiz>()), Times.Never);
            _unitOfWork.Verify(unit => unit.SaveChangesAsync(Token), Times.Never);
        }

        [Fact]
        public async Task GetPublishedQuizzesAsync_ReturnsMappedQuizDtos()
        {
            var publishedQuiz = CreateQuiz(9, "Published quiz", 20, isPublished: true);

            _quizRepository.Setup(repo => repo.GetPublishedQuizzesAsync(Token))
                .ReturnsAsync(new List<Quiz> { publishedQuiz });

            var result = Assert.Single(await _service.GetPublishedQuizzesAsync(Token));

            Assert.Equal(9, result.Id);
            Assert.Equal("Published quiz", result.Title);
            Assert.Equal(20, result.TimeLimitMinutes);
            Assert.True(result.IsPublished);
        }

        private void SetUpQuizLookup(int id, Quiz? quiz)
        {
            _quizRepository.Setup(repo => repo.GetByIdAsync(id, Token))
                .ReturnsAsync(quiz);
        }

        private static Quiz CreateQuiz(
            int id,
            string title,
            int timeLimitMinutes,
            bool isPublished = false)
        {
            return new Quiz
            {
                Id = id,
                Title = title,
                TimeLimitMinutes = timeLimitMinutes,
                IsPublished = isPublished
            };
        }
    }
}