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
    public class QuizAttemptRepositoryTests
    {
        private static readonly CancellationToken Token = CancellationToken.None;

        [Fact]
        public async Task GetAllAsync_ReturnsAttemptsFromDifferentStudents()
        {
            await using var context = CreateContext();
            var first = CreateAttempt("student-1");
            var second = CreateAttempt("student-2");

            context.QuizAttempts.AddRange(first, second);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuizAttemptRepository(context);
            var attempts = await repository.GetAllAsync(Token);

            Assert.Equal(2, attempts.Count);
            Assert.Contains(attempts, attempt => attempt.Id == first.Id);
            Assert.Contains(attempts, attempt => attempt.Id == second.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMatchingAttempt()
        {
            await using var context = CreateContext();
            var expected = CreateAttempt("student-1");

            context.QuizAttempts.AddRange(expected, CreateAttempt("student-2"));
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuizAttemptRepository(context);
            var attempt = await repository.GetByIdAsync(expected.Id, Token);

            Assert.NotNull(attempt);
            Assert.Equal(expected.Id, attempt.Id);
            Assert.Equal(expected.QuizId, attempt.QuizId);
            Assert.Equal("student-1", attempt.UserId);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenAttemptIsMissing()
        {
            await using var context = CreateContext();
            var repository = new QuizAttemptRepository(context);

            var attempt = await repository.GetByIdAsync(99, Token);

            Assert.Null(attempt);
        }

        [Fact]
        public async Task GetByUserIdAsync_ReturnsOnlyRequestedStudentsAttempts()
        {
            await using var context = CreateContext();
            var first = CreateAttempt("student-1");
            var second = new QuizAttempt
            {
                User = first.User,
                Quiz = first.Quiz,
                StartedAt = first.StartedAt.AddMinutes(20),
                Status = QuizAttemptStatus.InProgress
            };

            context.QuizAttempts.AddRange(first, second, CreateAttempt("student-2"));
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuizAttemptRepository(context);
            var attempts = await repository.GetByUserIdAsync("student-1", Token);

            Assert.Equal(2, attempts.Count);
            Assert.All(attempts, attempt => Assert.Equal("student-1", attempt.UserId));
            Assert.Contains(attempts, attempt => attempt.Id == first.Id);
            Assert.Contains(attempts, attempt => attempt.Id == second.Id);
        }

        [Fact]
        public async Task AddAsync_SavesAttempt_WhenChangesAreCommitted()
        {
            await using var context = CreateContext();
            var repository = new QuizAttemptRepository(context);
            var attempt = CreateAttempt("student-1");

            await repository.AddAsync(attempt, Token);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.QuizAttempts.ToListAsync(Token));

            Assert.Equal(attempt.Id, saved.Id);
            Assert.Equal(attempt.QuizId, saved.QuizId);
            Assert.Equal("student-1", saved.UserId);
            Assert.Equal(attempt.StartedAt, saved.StartedAt);
            Assert.Equal(QuizAttemptStatus.InProgress, saved.Status);
        }

        [Fact]
        public async Task Update_SavesCompletionDetailsOnDetachedAttempt()
        {
            await using var context = CreateContext();
            var original = CreateAttempt("student-1");

            context.QuizAttempts.Add(original);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var finishedAt = original.StartedAt.AddMinutes(5);
            var updated = new QuizAttempt
            {
                Id = original.Id,
                QuizId = original.QuizId,
                UserId = original.UserId,
                StartedAt = original.StartedAt,
                FinishedAt = finishedAt,
                Status = QuizAttemptStatus.Completed
            };

            var repository = new QuizAttemptRepository(context);
            repository.Update(updated);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.QuizAttempts.ToListAsync(Token));

            Assert.Equal(original.Id, saved.Id);
            Assert.Equal(QuizAttemptStatus.Completed, saved.Status);
            Assert.Equal<DateTime?>(finishedAt, saved.FinishedAt);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheRequestedAttempt()
        {
            await using var context = CreateContext();
            var attemptToDelete = CreateAttempt("student-1");
            var attemptToKeep = CreateAttempt("student-2");

            context.QuizAttempts.AddRange(attemptToDelete, attemptToKeep);
            await context.SaveChangesAsync(Token);

            var repository = new QuizAttemptRepository(context);
            repository.Delete(attemptToDelete);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var remaining = Assert.Single(await context.QuizAttempts.ToListAsync(Token));
            Assert.Equal(attemptToKeep.Id, remaining.Id);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HasAttemptsForQuizAsync_ChecksOnlyTheRequestedQuiz(
            bool expected)
        {
            await using var context = CreateContext();
            var attempt = CreateAttempt("student-1");
            var quizWithoutAttempts = new Quiz
            {
                Title = "History quiz",
                TimeLimitMinutes = 20
            };

            context.QuizAttempts.Add(attempt);
            context.Quizzes.Add(quizWithoutAttempts);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var quizId = expected ? attempt.QuizId : quizWithoutAttempts.Id;
            var repository = new QuizAttemptRepository(context);

            var hasAttempts = await repository.HasAttemptsForQuizAsync(quizId, Token);

            Assert.Equal(expected, hasAttempts);
        }

        private static QuizMasterDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<QuizMasterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new QuizMasterDbContext(options);
        }

        private static QuizAttempt CreateAttempt(string studentId)
        {
            return new QuizAttempt
            {
                User = new ApplicationUser
                {
                    Id = studentId,
                    UserName = studentId
                },
                Quiz = new Quiz
                {
                    Title = "Math quiz",
                    TimeLimitMinutes = 15
                },
                StartedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Status = QuizAttemptStatus.InProgress
            };
        }
    }
}