using QuizMasterDomain.DTOs.QuizResultDtos;
using QuizMasterDomain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Interfaces
{
    public interface IQuizResultRepository
    {
        Task<ICollection<QuizResult>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<QuizResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<QuizResult?> GetByQuizAttemptIdAsync(int quizAttemptId, CancellationToken cancellationToken = default);
        Task AddAsync(QuizResult quizResult, CancellationToken cancellationToken = default);
        void Update(QuizResult quizResult);
        void Delete(QuizResult quizResult);
        Task<ICollection<QuizResult>> GetLeaderboardResultsByQuizIdAsync(int quizId, CancellationToken cancellationToken = default);
        Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, CancellationToken cancellationToken = default);
    }
}

          