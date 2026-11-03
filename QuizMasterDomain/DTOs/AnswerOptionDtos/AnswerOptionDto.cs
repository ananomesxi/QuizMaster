using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.AnswerOptionDtos
{
    public class AnswerOptionDto
    {
        // არ სჭირდება ატრიბუტები, რადგან მეთოდი ქმნის DTO-ს ბაზიდან
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int QuestionId { get; set; }
    }
}
