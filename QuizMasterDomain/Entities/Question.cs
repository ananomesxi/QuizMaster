using QuizMasterDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Entities
{
    public class Question
    {
        private string _text = string.Empty;
        private int _points;
        private QuestionType _type;

        public int Id { get; set; }
        public string Text
        {
            get => _text;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Question text is required.");

                if (value.Length > 1000)
                    throw new ArgumentException("Question text cannot exceed 1000 characters.");

                _text = value;
            }
        }
        public int Points
        {
            get => _points;
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(Points), "Points must be greater than zero.");

                _points = value;
            }
        }
        public QuestionType Type
        {
            get => _type;
            set
            {
                if (!Enum.IsDefined(typeof(QuestionType), value))
                    throw new ArgumentOutOfRangeException(nameof(Type), "Invalid question type.");

                _type = value;
            }
        }
        public int QuizId { get; set; } // Foreign key to Quiz entity
        public Quiz Quiz { get; set; } = null!; // Navigation property to Quiz entity
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>(); // Navigation property to StudentAnswer entity
        public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>(); // Navigation property to AnswerOption entity


    }
}
