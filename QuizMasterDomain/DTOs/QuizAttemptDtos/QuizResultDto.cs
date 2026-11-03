using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizAttemptDtos
{
    public class QuizResultDto
    {
        public int QuizAttemptId { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
    }
}
