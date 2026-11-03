using QuizMasterDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.QuestionDtos
{
    public class QuestionDto
    {
        // არ სჭირდება ატრიბუტები, რადგან მეთოდი ქმნის DTO-ს ბაზიდან
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Points { get; set; }
        public QuestionType Type { get; set; }
        public int QuizId { get; set; }
    }
}
