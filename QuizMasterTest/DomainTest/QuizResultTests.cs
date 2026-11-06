using System;
using QuizMasterDomain.Entities;
using Xunit;

namespace QuizMasterTest.DomainTest
{
    public class QuizResultTests
    {
        [Fact]
        public void Score_AcceptsZero()
        {
            var result = new QuizResult { Score = 5 };

            result.Score = 0;

            Assert.Equal(0, result.Score);
        }

        [Fact]
        public void Score_Throws_WhenNegative()
        {
            var result = new QuizResult();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                result.Score = -1;
            });

            Assert.Equal(nameof(QuizResult.Score), exception.ParamName);
        }

        [Fact]
        public void MaxScore_AcceptsZero()
        {
            var result = new QuizResult { MaxScore = 10 };

            result.MaxScore = 0;

            Assert.Equal(0, result.MaxScore);
        }

        [Fact]
        public void MaxScore_Throws_WhenNegative()
        {
            var result = new QuizResult();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                result.MaxScore = -1;
            });

            Assert.Equal(nameof(QuizResult.MaxScore), exception.ParamName);
        }
    }
}