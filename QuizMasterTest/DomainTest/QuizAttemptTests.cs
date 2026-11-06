using System;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Enums;
using Xunit;

namespace QuizMasterTest.DomainTest
{
    public class QuizAttemptTests
    {
        [Fact]
        public void Status_AcceptsCompleted()
        {
            var attempt = new QuizAttempt
            {
                Status = QuizAttemptStatus.InProgress
            };

            attempt.Status = QuizAttemptStatus.Completed;

            Assert.Equal(QuizAttemptStatus.Completed, attempt.Status);
        }

        [Fact]
        public void Status_Throws_WhenValueIsUndefined()
        {
            var attempt = new QuizAttempt();

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                attempt.Status = (QuizAttemptStatus)999;
            });

            Assert.Equal(nameof(QuizAttempt.Status), exception.ParamName);
        }
    }
}