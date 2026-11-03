using QuizMasterDomain.DTOs.QuizResultDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IQuizResultService
    {
        Task<ICollection<LeaderboardEntryDto>> GetLeaderboardResultsByQuizIdAsync(int quizId, CancellationToken cancellationToken = default);
        Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, CancellationToken cancellationToken = default);
    }
}
