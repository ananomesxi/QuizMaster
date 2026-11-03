using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizAttemptDtos
{
    public class AttemptOptionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
