using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Entities
{
    public class AnswerOption
    {
        private string _text = string.Empty;

        public int Id { get; set; }
        public string Text
        {
            get => _text;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Answer option text is required.");

                if (value.Length > 500)
                    throw new ArgumentException("Answer option text cannot exceed 500 characters.");

                _text = value;
            }
        }
        public bool IsCorrect { get; set; }
        public int QuestionId { get; set; } // Foreign key to Question entity
        public Question Question { get; set; } = null!; // Navigation property to Question entity
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>(); // Navigation property to StudentAnswer entity

    }
}
