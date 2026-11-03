using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizDtos
{
    public class CreateQuizDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        [Range(1, int.MaxValue)]
        public int TimeLimitMinutes { get; set; }
        
    }
}
