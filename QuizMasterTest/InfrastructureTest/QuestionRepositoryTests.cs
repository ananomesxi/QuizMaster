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
    public class QuestionRepositoryTests
    {
        private static readonly CancellationToken Token = CancellationToken.None;

        [Fact]
        public async Task GetAllAsync_ReturnsQuestionsFromDifferentQuizzes()
        {
            await using var context = CreateContext();
            var math = CreateQuestion("What is 2 + 2?");
            var history = CreateQuestion("When did the Berlin Wall fall?");

            context.Questions.AddRange(math, history);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuestionRepository(context);
            var questions = await repository.GetAllAsync(Token);

            Assert.Equal(2, questions.Count);
            Assert.Contains(questions, question => question.Id == math.Id);
            Assert.Contains(questions, question => question.Id == history.Id);
        }

        [Fact]
        public async Task GetByIdAsync_LoadsAnswerOptions()
        {
            await using var context = CreateContext();
            var question = CreateQuestion("What is 2 + 2?");
            question.AnswerOptions.Add(new AnswerOption
            {
                Text = "Four",
                IsCorrect = true
            });

            context.Questions.Add(question);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuestionRepository(context);
            var result = await repository.GetByIdAsync(question.Id, Token);

            Assert.NotNull(result);
            Assert.Equal(question.Id, result.Id);

            var option = Assert.Single(result.AnswerOptions);
            Assert.Equal("Four", option.Text);
            Assert.True(option.IsCorrect);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenQuestionIsMissing()
        {
            await using var context = CreateContext();
            var repository = new QuestionRepository(context);

            var result = await repository.GetByIdAsync(99, Token);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByQuizIdAsync_ReturnsOnlyQuestionsForRequestedQuiz()
        {
            await using var context = CreateContext();
            var requestedQuestion = CreateQuestion("What is 2 + 2?");
            var otherQuestion = CreateQuestion("What is 3 + 3?");

            context.Questions.AddRange(requestedQuestion, otherQuestion);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuestionRepository(context);
            var result = Assert.Single(
                await repository.GetByQuizIdAsync(requestedQuestion.QuizId, Token));

            Assert.Equal(requestedQuestion.Id, result.Id);
            Assert.Equal(requestedQuestion.QuizId, result.QuizId);
        }

        [Fact]
        public async Task AddAsync_SavesQuestion_WhenChangesAreCommitted()
        {
            await using var context = CreateContext();
            var repository = new QuestionRepository(context);
            var question = CreateQuestion("What is 2 + 2?");

            await repository.AddAsync(question, Token);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var savedQuestion = Assert.Single(await context.Questions.ToListAsync(Token));

            Assert.Equal(question.Id, savedQuestion.Id);
            Assert.Equal(question.QuizId, savedQuestion.QuizId);
            Assert.Equal("What is 2 + 2?", savedQuestion.Text);
            Assert.Equal(5, savedQuestion.Points);
        }

        [Fact]
        public async Task Update_SavesChangesToDetachedQuestion()
        {
            await using var context = CreateContext();
            var question = CreateQuestion("What is 2 + 2?");

            context.Questions.Add(question);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var repository = new QuestionRepository(context);
            question.Text = "What is 3 + 3?";
            question.Points = 10;

            repository.Update(question);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var savedQuestion = Assert.Single(await context.Questions.ToListAsync(Token));

            Assert.Equal(question.Id, savedQuestion.Id);
            Assert.Equal("What is 3 + 3?", savedQuestion.Text);
            Assert.Equal(10, savedQuestion.Points);
        }

        [Fact]
        public async Task Delete_RemovesOnlyTheRequestedQuestion()
        {
            await using var context = CreateContext();
            var questionToDelete = CreateQuestion("What is 2 + 2?");
            var questionToKeep = CreateQuestion("What is 3 + 3?");

            context.Questions.AddRange(questionToDelete, questionToKeep);
            await context.SaveChangesAsync(Token);

            var repository = new QuestionRepository(context);
            repository.Delete(questionToDelete);
            await context.SaveChangesAsync(Token);
            context.ChangeTracker.Clear();

            var remainingQuestion = Assert.Single(
                await context.Questions.ToListAsync(Token));

            Assert.Equal(questionToKeep.Id, remainingQuestion.Id);
        }

        private static QuizMasterDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<QuizMasterDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new QuizMasterDbContext(options);
        }

        private static Question CreateQuestion(string text)
        {
            return new Question
            {
                Text = text,
                Points = 5,
                Quiz = new Quiz
                {
                    Title = "Practice quiz",
                    TimeLimitMinutes = 15
                }
            };
        }
    }
}