using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Entities
{
    public class QuizResult
    {
        private int _score;
        private int _maxScore;

        public int Id { get; set; }
        public int QuizAttemptId { get; set; } // Foreign key to QuizAttempt entity
        public QuizAttempt QuizAttempt { get; set; } = null!; // Navigation property to QuizAttempt entity
        public int Score
        {
            get => _score;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(Score), "Score cannot be negative.");

                _score = value;
            }
        }
        public int MaxScore
        {
            get => _maxScore;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MaxScore), "Maximum score cannot be negative.");

                _maxScore = value;
            }
        }
    }
}
