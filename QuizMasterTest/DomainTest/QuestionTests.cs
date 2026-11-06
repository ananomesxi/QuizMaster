using System;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Enums;
using Xunit;

namespace QuizMasterTest.DomainTest
{
    public class QuestionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Text_Throws_WhenBlank(string? text)
        {
            var question = new Question();

            Assert.Throws<ArgumentException>(() =>
            {
                question.Text = text!;
            });
        }

        [Fact]
        public void Text_AcceptsExactly1000Characters()
        {
            var question = new Question();
            var text = new string('A', 1000);

            question.Text = text;

            Assert.Equal(text, question.Text);
        }

        [Fact]
        public void Text_Throws_WhenLongerThan1000Characters()
        {
            var question = new Question();

            Assert.Throws<ArgumentException>(() =>
            {
                question.Text = new string('A', 1001);
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Points_Throws_WhenZeroOrNegative(int points)
        {
            var question = new Question();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                question.Points = points;
            });
        }

        [Fact]
        public void Points_AcceptsOnePoint()
        {
            var question = new Question();

            question.Points = 1;

            Assert.Equal(1, question.Points);
        }

        [Fact]
        public void Type_AcceptsMultipleChoice()
        {
            var question = new Question();

            question.Type = QuestionType.MultipleChoice;

            Assert.Equal(QuestionType.MultipleChoice, question.Type);
        }

        [Fact]
        public void Type_Throws_WhenValueIsUndefined()
        {
            var question = new Question();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                question.Type = (QuestionType)999;
            });
        }
    }
}