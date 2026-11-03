using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterInfrastructure.Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly QuizMasterDbContext _context;

        public QuizRepository(QuizMasterDbContext context)
        {
            _context = context;
        }


        public async Task<ICollection<Quiz>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Quizzes.ToListAsync(cancellationToken);
        }
        public async Task<Quiz?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }

        public async Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default)
        {
            await _context.Quizzes.AddAsync(quiz, cancellationToken);
        }

        public void Update(Quiz quiz)
        {
            _context.Quizzes.Update(quiz);
        }

        public void Delete(Quiz quiz)
        {
            _context.Quizzes.Remove(quiz);
        }

        public async Task<ICollection<Quiz>> GetPublishedQuizzesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Quizzes
                .Where(q => q.IsPublished)
                .ToListAsync(cancellationToken);
        }
    }
}
