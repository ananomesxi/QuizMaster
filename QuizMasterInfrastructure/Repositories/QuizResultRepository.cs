using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using QuizMasterDomain.DTOs.QuizResultDtos;

namespace QuizMasterInfrastructure.Repositories
{
    public class QuizResultRepository : IQuizResultRepository
    {
        private readonly QuizMasterDbContext _context;
        public QuizResultRepository(QuizMasterDbContext context)
        {
            _context = context;
        }
        public async Task<ICollection<QuizResult>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.QuizResults.ToListAsync(cancellationToken);
        }
        public async Task<QuizResult?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.QuizResults.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }
        public async Task<QuizResult?> GetByQuizAttemptIdAsync(int quizAttemptId, CancellationToken cancellationToken = default)
        {
            return await _context.QuizResults
                .FirstOrDefaultAsync(q => q.QuizAttemptId == quizAttemptId, cancellationToken);
        }
        public async Task AddAsync(QuizResult quizResult, CancellationToken cancellationToken = default)
        {
            await _context.QuizResults.AddAsync(quizResult, cancellationToken);
        }
        public void Update(QuizResult quizResult)
        {
            _context.QuizResults.Update(quizResult);
        }
        public void Delete(QuizResult quizResult)
        {
            _context.QuizResults.Remove(quizResult);
        }

        public async Task<ICollection<QuizResult>> GetLeaderboardResultsByQuizIdAsync(int quizId, CancellationToken cancellationToken = default)
        {
            return  await _context.QuizResults.Where(result => result.QuizAttempt.QuizId == quizId)
                .Include(result => result.QuizAttempt)
                .ThenInclude(attempt => attempt.User)
                .ToListAsync(cancellationToken);
        }

        public async Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, CancellationToken cancellationToken = default)
        {
            var statistics = await _context.QuizResults
                .Where(result => result.QuizAttempt.QuizId == quizId)
                .GroupBy(result => result.QuizAttempt.QuizId)
                .Select(group => new QuizStatisticsDto
                {
                    QuizId = group.Key,
                    AttemptCount = group.Count(),
                    AverageScore = group.Average(result => (double)result.Score),
                    HighestScore = group.Max(result => result.Score),
                    LowestScore = group.Min(result => result.Score)
                }).FirstOrDefaultAsync(cancellationToken);
            return statistics ?? new QuizStatisticsDto() { QuizId = quizId };
        }

    }
}

