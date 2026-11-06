using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using QuizMasterApplication.Services.Implementations;
using QuizMasterDomain.DTOs.QuizResultDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Identity;
using QuizMasterDomain.Interfaces;
using Xunit;

namespace QuizMasterTest.ApplicationTest
{
    public class QuizResultServiceTests
    {
        private const int QuizId = 1;
        private static readonly CancellationToken Token = CancellationToken.None;

        private readonly Mock<IQuizResultRepository> _results = new();
        private readonly Mock<IQuizRepository> _quizzes = new();
        private readonly QuizResultService _service;

        public QuizResultServiceTests()
        {
            _service = new QuizResultService(_results.Object, _quizzes.Object);
        }

        [Fact]
        public async Task GetLeaderboardResultsByQuizIdAsync_RanksEachStudentsBestScore()
        {
            SetUpQuiz();

            _results.Setup(repo => repo.GetLeaderboardResultsByQuizIdAsync(QuizId, Token))
                .ReturnsAsync(new List<QuizResult>
                {
                    CreateResult("student-1", "Nino", 6),
                    CreateResult("student-2", "Giorgi", 8),
                    CreateResult("student-1", "Nino", 10)
                });

            var leaderboard = await _service.GetLeaderboardResultsByQuizIdAsync(
                QuizId, Token);

            Assert.Collection(leaderboard,
                first =>
                {
                    Assert.Equal(1, first.Rank);
                    Assert.Equal("Nino", first.UserName);
                    Assert.Equal(10, first.Score);
                    Assert.Equal(10, first.MaxScore);
                },
                second =>
                {
                    Assert.Equal(2, second.Rank);
                    Assert.Equal("Giorgi", second.UserName);
                    Assert.Equal(8, second.Score);
                });
        }

        [Fact]
        public async Task GetLeaderboardResultsByQuizIdAsync_ReturnsEmpty_WhenQuizHasNoResults()
        {
            SetUpQuiz();

            _results.Setup(repo => repo.GetLeaderboardResultsByQuizIdAsync(QuizId, Token))
                .ReturnsAsync(new List<QuizResult>());

            var leaderboard = await _service.GetLeaderboardResultsByQuizIdAsync(
                QuizId, Token);

            Assert.Empty(leaderboard);
        }

        [Fact]
        public async Task GetLeaderboardResultsByQuizIdAsync_Throws_WhenQuizIsMissing()
        {
            _quizzes.Setup(repo => repo.GetByIdAsync(QuizId, Token))
                .ReturnsAsync((Quiz?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetLeaderboardResultsByQuizIdAsync(QuizId, Token));

            _results.Verify(repo => repo.GetLeaderboardResultsByQuizIdAsync(
                QuizId, Token), Times.Never);
        }

        [Fact]
        public async Task GetQuizStatisticsAsync_ReturnsStatisticsFromRepository()
        {
            SetUpQuiz();

            var statistics = new QuizStatisticsDto
            {
                QuizId = QuizId,
                AttemptCount = 2,
                AverageScore = 8,
                HighestScore = 10,
                LowestScore = 6
            };

            _results.Setup(repo => repo.GetQuizStatisticsAsync(QuizId, Token))
                .ReturnsAsync(statistics);

            var result = await _service.GetQuizStatisticsAsync(QuizId, Token);

            Assert.Same(statistics, result);
            _results.Verify(repo => repo.GetQuizStatisticsAsync(QuizId, Token),
                Times.Once);
        }

        [Fact]
        public async Task GetQuizStatisticsAsync_Throws_WhenQuizIsMissing()
        {
            _quizzes.Setup(repo => repo.GetByIdAsync(QuizId, Token))
                .ReturnsAsync((Quiz?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _service.GetQuizStatisticsAsync(QuizId, Token));

            _results.Verify(repo => repo.GetQuizStatisticsAsync(QuizId, Token),
                Times.Never);
        }

        private void SetUpQuiz()
        {
            var quiz = new Quiz
            {
                Id = QuizId,
                Title = "Math quiz",
                TimeLimitMinutes = 15
            };

            _quizzes.Setup(repo => repo.GetByIdAsync(QuizId, Token))
                .ReturnsAsync(quiz);
        }

        private static QuizResult CreateResult(string studentId, string userName, int score)
        {
            return new QuizResult
            {
                Score = score,
                MaxScore = 10,
                QuizAttempt = new QuizAttempt
                {
                    QuizId = QuizId,
                    UserId = studentId,
                    User = new ApplicationUser
                    {
                        Id = studentId,
                        UserName = userName
                    }
                }
            };
        }
    }
}