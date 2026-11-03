using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Entities
{
    public class StudentAnswer
    {

        public int Id { get; set; }
        public int QuizAttemptId { get; set; } // Foreign key to Attempt entity
        public QuizAttempt QuizAttempt { get; set; } = null!; // Navigation property to Attempt entity
        public int QuestionId { get; set; } // Foreign key to Question entity
        public Question Question { get; set; } = null!; // Navigation property to Question entity
        public int AnswerOptionId { get; set; } // Foreign key to AnswerOption entity
        public AnswerOption AnswerOption { get; set; } = null!; // Navigation property to AnswerOption entity


    }
}
