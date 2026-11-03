using QuizMasterDomain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace QuizMasterDomain.DTOs.QuestionDtos
{
    public class CreateQuestionDto
    {
        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = string.Empty;
        [Range(1, int.MaxValue)]
        public int Points { get; set; }
        [EnumDataType(typeof(QuestionType))]
        public QuestionType Type { get; set; }
    }
}
