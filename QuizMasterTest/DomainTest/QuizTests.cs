using System;
using QuizMasterDomain.Entities;
using Xunit;

namespace QuizMasterTest.DomainTest
{
    public class QuizTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Title_Throws_WhenBlank(string? title)
        {
            var quiz = new Quiz();

            Assert.Throws<ArgumentException>(() =>
            {
                quiz.Title = title!;
            });
        }

        [Fact]
        public void Title_AcceptsExactly100Characters()
        {
            var quiz = new Quiz();
            var title = new string('A', 100);

            quiz.Title = title;

            Assert.Equal(title, quiz.Title);
        }

        [Fact]
        public void Title_Throws_WhenLongerThan100Characters()
        {
            var quiz = new Quiz();
            var title = new string('A', 101);

            Assert.Throws<ArgumentException>(() =>
            {
                quiz.Title = title;
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void TimeLimitMinutes_Throws_WhenZeroOrNegative(int minutes)
        {
            var quiz = new Quiz();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                quiz.TimeLimitMinutes = minutes;
            });
        }

        [Fact]
        public void TimeLimitMinutes_AcceptsOneMinute()
        {
            var quiz = new Quiz();

            quiz.TimeLimitMinutes = 1;

            Assert.Equal(1, quiz.TimeLimitMinutes);
        }
    }
}