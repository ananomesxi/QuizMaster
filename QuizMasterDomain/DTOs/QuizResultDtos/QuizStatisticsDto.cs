using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizResultDtos
{
    public class QuizStatisticsDto
    {
        public int QuizId { get; set; }
        public int AttemptCount { get; set; }
        public double AverageScore { get; set; }
        public int HighestScore { get; set; }
        public int LowestScore { get; set; }
    }
}
