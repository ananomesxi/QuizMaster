using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Enums;
using QuizMasterDomain.Identity;
using QuizMasterInfrastructure.Data;
using QuizMasterInfrastructure.Repositories;
using Xunit;

namespace QuizMasterTest.InfrastructureTest
{
    public class QuizResultRepositoryTests
    {
        private static readonly CancellationToken Token = CancellationToken.None;

        [Fact]
        public async Task GetAllAsync_ReturnsResultsFromDifferentQuizzes()
        {
            await using var context = CreateContext();
            var first = CreateResult(CreateQuiz("Math"), "student-1", 5);
            var second = CreateResult(CreateQuiz("History"), "student-2", 8);

            await SeedResultsAsync(context, first, second);

            var repository = new QuizResultRepository(context);
            var results = await repository.GetAllAsync(Token);

            Assert.Equal(2, results.Count);
            Assert.Contains(results, result => result.Id == first.Id);
            Assert.Contains(results, result => result.Id == second.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMatchingResult()
        {
            await using var context = CreateContext();
            var quiz = CreateQuiz("Math");
            var expected = CreateResult(quiz, "student-1", 8);

            await SeedResultsAsync(context,
                CreateResult(quiz, "student-2", 5), expected);

            var repository = new QuizResultRepository(context);
            var result = await repository.GetByIdAsync(expected.Id, Token);

            Assert.NotNull(result);
            Assert.Equal(expected.Id, result.Id);
            Assert.Equal(8, result.Score);
            Assert.Equal(10, result.MaxScore);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenResultIsMissing()
        {
            await using var context = CreateContext();
            var repository = new QuizResultRepository(context);

            var result = await repository.GetByIdAsync(99, Token);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByQuizAttemptIdAsync_ReturnsResultForRequestedAttempt()
        {
            await using var context = CreateContext();
            var quiz = CreateQuiz("Math");
            var expected = CreateResult(quiz, "student-1", 8);

            await SeedResultsAsync(context,
                CreateResult(quiz, "student-2", 5), expected);

            var repository = new QuizResultRepository(context);
            var result = await repository.GetByQuizAttemptIdAsync(
                expected.QuizAttemptId, Token);

            Assert.NotNull(result);
            Assert.Equal(expected.Id, result.Id);
            Assert.Equal(expected.QuizAttemptId, result.QuizAttemptId);
        }

        [Fact]
        public async Task GetByQuizAttemptIdAsync_ReturnsNull_WhenAttemptHasNoResult()
        {
            await using var context = CreateContext();
            var quiz = CreateQuiz("Math");
            var attemptWithoutResult = CreateAttempt(quiz, "student-1");
            context.QuizAttempts.Add(attemptWithoutResult);

            await SeedResultsAsync(context, CreateResult(quiz, "student-2", 5));

            var repository = new QuizResultRepository(context);
            var result = await repository.GetByQuizAttemptIdAsync(
                attemptWithoutResult.Id, Token);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_SavesResult_WhenChangesAreCommitted()
        {
            await using var context = CreateContext();
            var repository = new QuizResultRepository(context);
            var result = CreateResult(CreateQuiz("Math"), "student-1", 8);

            await repository.AddAsync(result, Token);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.QuizResults.ToListAsync(Token));

            Assert.Equal(result.Id, saved.Id);
            Assert.Equal(result.QuizAttemptId, saved.QuizAttemptId);
            Assert.Equal(8, saved.Score);
            Assert.Equal(10, saved.MaxScore);
        }

        [Fact]
        public async Task Update_SavesScoreChangesToDetachedResult()
        {
            await using var context = CreateContext();
            var original = CreateResult(CreateQuiz("Math"), "student-1", 5);
            await SeedResultsAsync(context, original);

            var updated = new QuizResult
            {
                Id = original.Id,
                QuizAttemptId = original.QuizAttemptId,
                Score = 8,
                MaxScore = 10
            };

            var repository = new QuizResultRepository(context);
            repository.Update(updated);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.QuizResults.ToListAsync(Token));

            Assert.Equal(original.Id, saved.Id);
            Assert.Equal(original.QuizAttemptId, saved.QuizAttemptId);
            Assert.Equal(8, saved.Score);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheRequestedResult()
        {
            await using var context = CreateContext();
            var quiz = CreateQuiz("Math");
            var resultToDelete = CreateResult(quiz, "student-1", 5);
            var resultToKeep = CreateResult(quiz, "student-2", 8);

            context.QuizResults.AddRange(resultToDelete, resultToKeep);
            await context.SaveChangesAsync(Token);

            var repository = new QuizResultRepository(context);
            repository.Delete(resultToDelete);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var remaining = Assert.Single(await context.QuizResults.ToListAsync(Token));

            Assert.Equal(resultToKeep.Id, remaining.Id);
        }

        [Fact]
        public async Task GetLeaderboardResultsByQuizIdAsync_FiltersResultsAndLoadsUsers()
        {
            await using var context = CreateContext();
            var quiz = CreateQuiz("Math");
            var first = CreateResult(quiz, "student-1", 5);
            var second = CreateResult(quiz, "student-2", 8);
            var other = CreateResult(CreateQuiz("History"), "student-3", 10);

            await SeedResultsAsync(context, first, second, other);

            var repository = new QuizResultRepository(context);
            var results = await repository.GetLeaderboardResultsByQuizIdAsync(
                quiz.Id, Token);

            Assert.Equal(2, results.Count);
            Assert.All(results, result => Assert.Equal(quiz.Id, result.QuizAttempt.QuizId));
            Assert.Contains(results, result =>
                result.Id == first.Id && result.QuizAttempt.User.UserName == "student-1");
            Assert.Contains(results, result =>
                result.Id == second.Id && result.QuizAttempt.User.UserName == "student-2");
        }

        [Fact]
        public async Task GetQuizStatisticsAsync_CalculatesStatisticsForRequestedQuiz()
        {
            await using var context = CreateContext();
            var quiz = CreateQuiz("Math");

            await SeedResultsAsync(context,
                CreateResult(quiz, "student-1", 5),
                CreateResult(quiz, "student-2", 8),
                CreateResult(CreateQuiz("History"), "student-3", 10));

            var repository = new QuizResultRepository(context);
            var statistics = await repository.GetQuizStatisticsAsync(quiz.Id, Token);

            Assert.Equal(quiz.Id, statistics.QuizId);
            Assert.Equal(2, statistics.AttemptCount);
            Assert.Equal(6.5, statistics.AverageScore);
            Assert.Equal(8, statistics.HighestScore);
            Assert.Equal(5, statistics.LowestScore);
        }

        [Fact]
        public async Task GetQuizStatisticsAsync_ReturnsZeroValues_WhenQuizHasNoResults()
        {
            await using var context = CreateContext();
            var quiz = CreateQuiz("Math");
            context.Quizzes.Add(quiz);

            await SeedResultsAsync(context,
                CreateResult(CreateQuiz("History"), "student-1", 8));

            var repository = new QuizResultRepository(context);
            var statistics = await repository.GetQuizStatisticsAsync(quiz.Id, Token);

            Assert.Equal(quiz.Id, statistics.QuizId);
            Assert.Equal(0, statistics.AttemptCount);
            Assert.Equal(0d, statistics.AverageScore);
            Assert.Equal(0, statistics.HighestScore);
            Assert.Equal(0, statistics.LowestScore);
        }

        private static async Task SeedResultsAsync(
            QuizMasterDbContext context, params QuizResult[] results)
        {
            context.QuizResults.AddRange(results);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();
        }

        private static QuizMasterDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<QuizMasterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new QuizMasterDbContext(options);
        }

        private static Quiz CreateQuiz(string title)
        {
            return new Quiz { Title = title, TimeLimitMinutes = 15 };
        }

        private static QuizResult CreateResult(Quiz quiz, string studentId, int score)
        {
            var attempt = CreateAttempt(quiz, studentId);
            attempt.Status = QuizAttemptStatus.Completed;
            attempt.FinishedAt = attempt.StartedAt.AddMinutes(5);

            return new QuizResult
            {
                QuizAttempt = attempt,
                Score = score,
                MaxScore = 10
            };
        }

        private static QuizAttempt CreateAttempt(Quiz quiz, string studentId)
        {
            return new QuizAttempt
            {
                Quiz = quiz,
                User = new ApplicationUser { Id = studentId, UserName = studentId },
                StartedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Status = QuizAttemptStatus.InProgress
            };
        }
    }
}