using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizAttemptDtos
{
    public class SaveAnswerDto
    {
        [Range(1, int.MaxValue)]
        public int QuestionId { get; set; }
        [Required]
        public ICollection<int> AnswerOptionIds { get; set; } = new List<int>();
    }
}
