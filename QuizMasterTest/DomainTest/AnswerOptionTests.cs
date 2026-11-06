using System;
using QuizMasterDomain.Entities;
using Xunit;

namespace QuizMasterTest.DomainTest
{
    public class AnswerOptionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Text_Throws_WhenBlank(string? text)
        {
            var option = new AnswerOption();

            Assert.Throws<ArgumentException>(() =>
            {
                option.Text = text!;
            });
        }

        [Fact]
        public void Text_AcceptsExactly500Characters()
        {
            var option = new AnswerOption();
            var text = new string('A', 500);

            option.Text = text;

            Assert.Equal(text, option.Text);
        }

        [Fact]
        public void Text_Throws_WhenLongerThan500Characters()
        {
            var option = new AnswerOption();

            Assert.Throws<ArgumentException>(() =>
            {
                option.Text = new string('A', 501);
            });
        }
    }
}