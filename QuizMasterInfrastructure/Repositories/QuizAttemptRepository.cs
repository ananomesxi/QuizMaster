using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuizMasterInfrastructure.Repositories
{
    public class QuizAttemptRepository : IQuizAttemptRepository
    {
        private readonly QuizMasterDbContext _context;
        public QuizAttemptRepository(QuizMasterDbContext context)
        {
            _context = context;
        }
        public async Task<ICollection<QuizAttempt>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.QuizAttempts.ToListAsync(cancellationToken);
        }
        public async Task<QuizAttempt?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.QuizAttempts.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }
        public async Task<ICollection<QuizAttempt>> GetByUserIdAsync( string userId, CancellationToken cancellationToken = default)
        {
            return await _context.QuizAttempts
                .Where(attempt => attempt.UserId == userId)
                .Include(attempt => attempt.StudentAnswers)
                .Include(attempt => attempt.Quiz)
                    .ThenInclude(quiz => quiz.Questions)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);
        }
        public async Task AddAsync(QuizAttempt quizAttempt, CancellationToken cancellationToken = default)
        {
            await _context.QuizAttempts.AddAsync(quizAttempt, cancellationToken);
        }
        public void Update(QuizAttempt quizAttempt)
        {
            _context.QuizAttempts.Update(quizAttempt);
        }
        public void Delete(QuizAttempt quizAttempt)
        {
            _context.QuizAttempts.Remove(quizAttempt);
        }
        public async Task<bool> HasAttemptsForQuizAsync(int quizId, CancellationToken cancellationToken = default)
        {
            return await _context.QuizAttempts
                .AnyAsync(q=> q.QuizId == quizId, cancellationToken);
        }
    }
}
