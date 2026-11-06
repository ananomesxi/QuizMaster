using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuizMasterDomain.Entities;
using QuizMasterInfrastructure.Data;
using QuizMasterInfrastructure.Repositories;
using Xunit;

namespace QuizMasterTest.InfrastructureTest
{
    public class AnswerOptionRepositoryTests
    {
        private static readonly CancellationToken Token = CancellationToken.None;

        [Fact]
        public async Task GetAllAsync_ReturnsOptionsFromDifferentQuestions()
        {
            await using var context = CreateContext();
            var first = CreateOption("Four");
            var second = CreateOption("Six");
            context.AnswerOptions.AddRange(first, second);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new AnswerOptionRepository(context);
            var result = await repository.GetAllAsync(Token);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, option => option.Id == first.Id);
            Assert.Contains(result, option => option.Id == second.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsMatchingOption()
        {
            await using var context = CreateContext();
            var option = CreateOption("Four", isCorrect: true);
            context.AnswerOptions.AddRange(option, CreateOption("Five"));
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new AnswerOptionRepository(context);
            var result = await repository.GetByIdAsync(option.Id, Token);

            Assert.NotNull(result);
            Assert.Equal(option.Id, result.Id);
            Assert.Equal(option.QuestionId, result.QuestionId);
            Assert.Equal("Four", result.Text);
            Assert.True(result.IsCorrect);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenOptionIsMissing()
        {
            await using var context = CreateContext();
            var repository = new AnswerOptionRepository(context);

            var result = await repository.GetByIdAsync(99, Token);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByQuestionIdAsync_ReturnsOnlyOptionsForRequestedQuestion()
        {
            await using var context = CreateContext();
            var correct = CreateOption("Four", isCorrect: true);
            var incorrect = new AnswerOption
            {
                Question = correct.Question,
                Text = "Five"
            };

            context.AnswerOptions.AddRange(correct, incorrect, CreateOption("Six"));
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new AnswerOptionRepository(context);
            var result = await repository.GetByQuestionIdAsync(correct.QuestionId, Token);

            Assert.Equal(2, result.Count);
            Assert.All(result, option => Assert.Equal(correct.QuestionId, option.QuestionId));
            Assert.Contains(result, option => option.Id == correct.Id);
            Assert.Contains(result, option => option.Id == incorrect.Id);
        }

        [Fact]
        public async Task AddAsync_SavesOption_WhenChangesAreCommitted()
        {
            await using var context = CreateContext();
            var repository = new AnswerOptionRepository(context);
            var option = CreateOption("Four", isCorrect: true);

            await repository.AddAsync(option, Token);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.AnswerOptions.ToListAsync(Token));

            Assert.Equal(option.Id, saved.Id);
            Assert.Equal(option.QuestionId, saved.QuestionId);
            Assert.Equal("Four", saved.Text);
            Assert.True(saved.IsCorrect);
        }

        [Fact]
        public async Task Update_SavesChangesToDetachedOption()
        {
            await using var context = CreateContext();
            var original = CreateOption("Four");
            context.AnswerOptions.Add(original);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var updated = new AnswerOption
            {
                Id = original.Id,
                QuestionId = original.QuestionId,
                Text = "4",
                IsCorrect = true
            };

            var repository = new AnswerOptionRepository(context);
            repository.Update(updated);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var saved = Assert.Single(await context.AnswerOptions.ToListAsync(Token));

            Assert.Equal(original.Id, saved.Id);
            Assert.Equal(original.QuestionId, saved.QuestionId);
            Assert.Equal("4", saved.Text);
            Assert.True(saved.IsCorrect);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheRequestedOption()
        {
            await using var context = CreateContext();
            var optionToDelete = CreateOption("Five");
            var optionToKeep = new AnswerOption
            {
                Question = optionToDelete.Question,
                Text = "Four",
                IsCorrect = true
            };

            context.AnswerOptions.AddRange(optionToDelete, optionToKeep);
            await context.SaveChangesAsync(Token);

            var repository = new AnswerOptionRepository(context);
            repository.Delete(optionToDelete);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var remaining = Assert.Single(await context.AnswerOptions.ToListAsync(Token));
            Assert.Equal(optionToKeep.Id, remaining.Id);
        }

        private static QuizMasterDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<QuizMasterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new QuizMasterDbContext(options);
        }

        private static AnswerOption CreateOption(string text, bool isCorrect = false)
        {
            return new AnswerOption
            {
                Text = text,
                IsCorrect = isCorrect,
                Question = new Question
                {
                    Text = "What is 2 + 2?",
                    Points = 5,
                    Quiz = new Quiz
                    {
                        Title = "Math quiz",
                        TimeLimitMinutes = 15
                    }
                }
            };
        }
    }
}