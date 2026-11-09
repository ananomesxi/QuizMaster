using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using QuizMasterDomain.Entities;

namespace QuizMasterInfrastructure.Repositories
{
    public class AnswerOptionRepository : IAnswerOptionRepository
    {
        private readonly QuizMasterDbContext _context;
        public AnswerOptionRepository(QuizMasterDbContext context)
        {
            _context = context;
        }

        public async Task<ICollection<AnswerOption>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.AnswerOptions.ToListAsync(cancellationToken);
        }
        public async Task<AnswerOption?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.AnswerOptions.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }
        public async Task<ICollection<AnswerOption>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken = default)
        {
            return await _context.AnswerOptions.Where(a => a.QuestionId == questionId).ToListAsync(cancellationToken);
        }
        public async Task AddAsync(AnswerOption answerOption, CancellationToken cancellationToken = default)
        {
            await _context.AnswerOptions.AddAsync(answerOption, cancellationToken);
        }
        public void Update(AnswerOption answerOption)
        {
            _context.AnswerOptions.Update(answerOption);
        }
        public void Delete(AnswerOption answerOption)
        {
            _context.AnswerOptions.Remove(answerOption);
        }
    }
}
