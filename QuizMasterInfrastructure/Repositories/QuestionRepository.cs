using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterInfrastructure.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly QuizMasterDbContext _context;
        public QuestionRepository(QuizMasterDbContext context)
        {
            _context = context;
        }
        public async Task<ICollection<Question>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Questions.ToListAsync(cancellationToken);
        }
        public async Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Questions
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }

        public async Task<ICollection<Question>> GetByQuizIdAsync(int quizId, CancellationToken cancellationToken = default)
        {
            return await _context.Questions
                .Where(q => q.QuizId == quizId)
                .Include(q => q.AnswerOptions)
                .ToListAsync(cancellationToken);
        }
        public async Task AddAsync(Question question, CancellationToken cancellationToken = default)
        {
            await _context.Questions.AddAsync(question, cancellationToken);
        }
        public void Update(Question question)
        {
            _context.Questions.Update(question);
        }
        public void Delete(Question question)
        {
            _context.Questions.Remove(question);
        }
    }
}
