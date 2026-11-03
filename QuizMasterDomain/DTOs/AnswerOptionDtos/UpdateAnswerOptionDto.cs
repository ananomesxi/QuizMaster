using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace QuizMasterDomain.DTOs.AnswerOptionDtos
{
    public class UpdateAnswerOptionDto
    {
        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
