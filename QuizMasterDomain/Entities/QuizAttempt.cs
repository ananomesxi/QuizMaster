using QuizMasterDomain.Enums;
using QuizMasterDomain.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Entities
{
    public class QuizAttempt
    {
        private QuizAttemptStatus _status;

        public int Id { get; set; }
        public int QuizId { get; set; } // Foreign key to Quiz entity
        public Quiz Quiz { get; set; } = null!; // Navigation property to Quiz entity
        public string UserId { get; set; } = string.Empty; // Foreign key to User entity
        public ApplicationUser User { get; set; } = null!; // Navigation property to User entity
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public QuizAttemptStatus Status
        {
            get => _status;
            set
            {
                if (!Enum.IsDefined(typeof(QuizAttemptStatus), value))
                    throw new ArgumentOutOfRangeException(nameof(Status), "Invalid attempt status.");

                _status = value;
            }
        }
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>(); // Navigation property to StudentAnswer entity
        public QuizResult? QuizResult { get; set; } // Navigation property to QuizResult entity

    }
}
