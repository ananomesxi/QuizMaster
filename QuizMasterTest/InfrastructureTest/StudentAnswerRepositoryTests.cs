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
    public class StudentAnswerRepositoryTests
    {
        private static readonly CancellationToken Token = CancellationToken.None;

        [Fact]
        public async Task GetAllAsync_ReturnsAnswersFromBothAttempts()
        {
            await using var context = CreateContext();
            var first = CreateAnswer();
            var second = CreateAnswerForAnotherAttempt(first);

            context.StudentAnswers.AddRange(first, second);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new StudentAnswerRepository(context);
            var answers = await repository.GetAllAsync(Token);

            Assert.Equal(2, answers.Count);
            Assert.Contains(answers, answer => answer.Id == first.Id);
            Assert.Contains(answers, answer => answer.Id == second.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMatchingAnswer()
        {
            await using var context = CreateContext();
            var expected = CreateAnswer();

            context.StudentAnswers.AddRange(expected, CreateAnswerForAnotherAttempt(expected));
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new StudentAnswerRepository(context);
            var answer = await repository.GetByIdAsync(expected.Id, Token);

            Assert.NotNull(answer);
            Assert.Equal(expected.Id, answer.Id);
            Assert.Equal(expected.QuizAttemptId, answer.QuizAttemptId);
            Assert.Equal(expected.QuestionId, answer.QuestionId);
            Assert.Equal(expected.AnswerOptionId, answer.AnswerOptionId);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenAnswerIsMissing()
        {
            await using var context = CreateContext();
            var repository = new StudentAnswerRepository(context);

            var answer = await repository.GetByIdAsync(99, Token);

            Assert.Null(answer);
        }

        [Fact]
        public async Task GetByQuizAttemptIdAsync_ReturnsOnlyAnswersForRequestedAttempt()
        {
            await using var context = CreateContext();
            var expected = CreateAnswer();
            var other = CreateAnswerForAnotherAttempt(expected);

            context.StudentAnswers.AddRange(expected, other);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new StudentAnswerRepository(context);
            var result = Assert.Single(
                await repository.GetByQuizAttemptIdAsync(expected.QuizAttemptId, Token));

            Assert.Equal(expected.Id, result.Id);
            Assert.Equal(expected.QuizAttemptId, result.QuizAttemptId);
        }

        [Fact]
        public async Task AddAsync_SavesAnswer_WhenChangesAreCommitted()
        {
            await using var context = CreateContext();
            var answer = CreateAnswer();
            var repository = new StudentAnswerRepository(context);

            await repository.AddAsync(answer, Token);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.StudentAnswers.ToListAsync(Token));

            Assert.Equal(answer.Id, saved.Id);
            Assert.Equal(answer.QuizAttemptId, saved.QuizAttemptId);
            Assert.Equal(answer.QuestionId, saved.QuestionId);
            Assert.Equal(answer.AnswerOptionId, saved.AnswerOptionId);
        }

        [Fact]
        public async Task Update_SavesNewOptionForDetachedAnswer()
        {
            await using var context = CreateContext();
            var original = CreateAnswer();
            var newOption = new AnswerOption
            {
                Question = original.Question,
                Text = "Five",
                IsCorrect = false
            };

            context.StudentAnswers.Add(original);
            context.AnswerOptions.Add(newOption);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var updated = new StudentAnswer
            {
                Id = original.Id,
                QuizAttemptId = original.QuizAttemptId,
                QuestionId = original.QuestionId,
                AnswerOptionId = newOption.Id
            };

            var repository = new StudentAnswerRepository(context);
            repository.Update(updated);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.StudentAnswers.ToListAsync(Token));

            Assert.Equal(original.Id, saved.Id);
            Assert.Equal(original.QuizAttemptId, saved.QuizAttemptId);
            Assert.Equal(original.QuestionId, saved.QuestionId);
            Assert.Equal(newOption.Id, saved.AnswerOptionId);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheRequestedAnswer()
        {
            await using var context = CreateContext();
            var answerToDelete = CreateAnswer();
            var answerToKeep = CreateAnswerForAnotherAttempt(answerToDelete);

            context.StudentAnswers.AddRange(answerToDelete, answerToKeep);
            await context.SaveChangesAsync(Token);

            var repository = new StudentAnswerRepository(context);
            repository.Delete(answerToDelete);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var remaining = Assert.Single(await context.StudentAnswers.ToListAsync(Token));
            Assert.Equal(answerToKeep.Id, remaining.Id);
        }

        private static QuizMasterDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<QuizMasterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new QuizMasterDbContext(options);
        }

        private static StudentAnswer CreateAnswer()
        {
            var quiz = new Quiz { Title = "Math quiz", TimeLimitMinutes = 15 };
            var question = new Question
            {
                Quiz = quiz,
                Text = "What is 2 + 2?",
                Points = 5
            };

            return new StudentAnswer
            {
                QuizAttempt = CreateAttempt(quiz, "student-1"),
                Question = question,
                AnswerOption = new AnswerOption
                {
                    Question = question,
                    Text = "Four",
                    IsCorrect = true
                }
            };
        }

        private static StudentAnswer CreateAnswerForAnotherAttempt(StudentAnswer answer)
        {
            return new StudentAnswer
            {
                QuizAttempt = CreateAttempt(answer.QuizAttempt.Quiz, "student-2"),
                Question = answer.Question,
                AnswerOption = answer.AnswerOption
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