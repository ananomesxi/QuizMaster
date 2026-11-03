using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.QuizResultDtos
{
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxScore { get; set; }
    }
}
