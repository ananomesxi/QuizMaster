using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Entities
{
    public class Quiz
    {
        private string _title = string.Empty;
        private int _timeLimitMinutes;

        public int Id { get; set; }
        public string Title
        {
            get => _title;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Title is required.");

                if (value.Length > 100)
                    throw new ArgumentException("Title cannot exceed 100 characters.");

                _title = value;
            }
        }
        public int TimeLimitMinutes
        {
            get => _timeLimitMinutes;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(TimeLimitMinutes), "Time limit must be greater than zero.");

                _timeLimitMinutes = value;
            }
        }
        public bool IsPublished { get; set; }
        public ICollection<Question> Questions { get; set; } = new List<Question>(); // Navigation property to Question entity>
        

    }
}
