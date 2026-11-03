using QuizMasterDomain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizAttemptDtos
{
    public class QuizAttemptDto
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public DateTime StartedAt { get; set; }
        public QuizAttemptStatus Status { get; set; }
    }
}
