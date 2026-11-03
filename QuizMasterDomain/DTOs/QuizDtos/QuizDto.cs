using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.Quiz
{
    public class QuizDto
    {
        // არ სჭირდება ატრიბუტები, რადგან მეთოდი ქმნის DTO-ს ბაზიდან 
        public int Id { get; set; } 
        public string Title { get; set; } = string.Empty;
        public int TimeLimitMinutes { get; set; }
        public bool IsPublished { get; set; }
    }
}
