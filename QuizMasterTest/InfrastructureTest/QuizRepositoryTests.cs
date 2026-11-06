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
    public class QuizRepositoryTests
    {
        private static readonly CancellationToken Token = CancellationToken.None;

        [Fact]
        public async Task GetAllAsync_ReturnsPublishedAndUnpublishedQuizzes()
        {
            await using var context = new QuizMasterDbContext(CreateOptions());

            var publishedQuiz = CreateQuiz("Math quiz", isPublished: true);
            var draftQuiz = CreateQuiz("History draft");

            context.Quizzes.AddRange(publishedQuiz, draftQuiz);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuizRepository(context);
            var result = await repository.GetAllAsync(Token);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, quiz => quiz.Id == publishedQuiz.Id);
            Assert.Contains(result, quiz => quiz.Id == draftQuiz.Id);
        }

        [Fact]
        public async Task GetByIdAsync_LoadsQuestionsAndAnswerOptions()
        {
            await using var context = new QuizMasterDbContext(CreateOptions());

            var quiz = CreateQuiz("Math quiz");
            var question = new Question
            {
                Text = "What is 2 + 2?",
                Points = 5
            };

            question.AnswerOptions.Add(new AnswerOption
            {
                Text = "Four",
                IsCorrect = true
            });

            quiz.Questions.Add(question);
            context.Quizzes.AddRange(quiz, CreateQuiz("Other quiz"));
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuizRepository(context);
            var result = await repository.GetByIdAsync(quiz.Id, Token);

            Assert.NotNull(result);
            Assert.Equal(quiz.Id, result.Id);
            Assert.Equal("Math quiz", result.Title);

            var savedQuestion = Assert.Single(result.Questions);
            Assert.Equal(question.Id, savedQuestion.Id);

            var savedOption = Assert.Single(savedQuestion.AnswerOptions);
            Assert.Equal("Four", savedOption.Text);
            Assert.True(savedOption.IsCorrect);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenQuizIsMissing()
        {
            await using var context = new QuizMasterDbContext(CreateOptions());
            var repository = new QuizRepository(context);

            var result = await repository.GetByIdAsync(99, Token);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_SavesQuiz_WhenChangesAreCommitted()
        {
            var options = CreateOptions();
            var quiz = CreateQuiz("Biology quiz", timeLimitMinutes: 20);

            await using (var writeContext = new QuizMasterDbContext(options))
            {
                var repository = new QuizRepository(writeContext);

                await repository.AddAsync(quiz, Token);
                await writeContext.SaveChangesAsync(Token);
            }

            await using var readContext = new QuizMasterDbContext(options);
            var savedQuiz = Assert.Single(await readContext.Quizzes.ToListAsync(Token));

            Assert.Equal(quiz.Id, savedQuiz.Id);
            Assert.Equal("Biology quiz", savedQuiz.Title);
            Assert.Equal(20, savedQuiz.TimeLimitMinutes);
            Assert.False(savedQuiz.IsPublished);
        }

        [Fact]
        public async Task Update_SavesChangesToDetachedQuiz()
        {
            var options = CreateOptions();
            var quiz = CreateQuiz("Original title");

            await using (var seedContext = new QuizMasterDbContext(options))
            {
                seedContext.Quizzes.Add(quiz);
                await seedContext.SaveChangesAsync(Token);
            }

            quiz.Title = "Updated title";
            quiz.TimeLimitMinutes = 30;

            await using (var writeContext = new QuizMasterDbContext(options))
            {
                var repository = new QuizRepository(writeContext);

                repository.Update(quiz);
                await writeContext.SaveChangesAsync(Token);
            }

            await using var readContext = new QuizMasterDbContext(options);
            var savedQuiz = Assert.Single(await readContext.Quizzes.ToListAsync(Token));

            Assert.Equal(quiz.Id, savedQuiz.Id);
            Assert.Equal("Updated title", savedQuiz.Title);
            Assert.Equal(30, savedQuiz.TimeLimitMinutes);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheRequestedQuiz()
        {
            await using var context = new QuizMasterDbContext(CreateOptions());

            var quizToDelete = CreateQuiz("Quiz to delete");
            var quizToKeep = CreateQuiz("Quiz to keep");
            context.Quizzes.AddRange(quizToDelete, quizToKeep);
            await context.SaveChangesAsync(Token);

            var repository = new QuizRepository(context);
            repository.Delete(quizToDelete);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var remainingQuiz = Assert.Single(await context.Quizzes.ToListAsync(Token));
            Assert.Equal(quizToKeep.Id, remainingQuiz.Id);
        }

        [Fact]
        public async Task GetPublishedQuizzesAsync_ReturnsOnlyPublishedQuizzes()
        {
            await using var context = new QuizMasterDbContext(CreateOptions());

            var publishedQuiz = CreateQuiz("Published quiz", isPublished: true);
            var draftQuiz = CreateQuiz("Draft quiz");
            context.Quizzes.AddRange(publishedQuiz, draftQuiz);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuizRepository(context);
            var result = Assert.Single(await repository.GetPublishedQuizzesAsync(Token));

            Assert.Equal(publishedQuiz.Id, result.Id);
            Assert.True(result.IsPublished);
        }

        private static DbContextOptions<QuizMasterDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<QuizMasterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private static Quiz CreateQuiz(
            string title,
            int timeLimitMinutes = 15,
            bool isPublished = false)
        {
            return new Quiz
            {
                Title = title,
                TimeLimitMinutes = timeLimitMinutes,
                IsPublished = isPublished
            };
        }
    }
}