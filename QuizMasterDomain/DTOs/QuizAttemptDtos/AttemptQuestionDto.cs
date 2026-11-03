using QuizMasterDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizAttemptDtos
{
    public class AttemptQuestionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; }
        public ICollection<AttemptOptionDto> AnswerOptions { get; set; } = new List<AttemptOptionDto>();
    }
}
