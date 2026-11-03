using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using QuizMasterInfrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterInfrastructure.Repositories
{
    public class StudentAnswerRepository : IStudentAnswerRepository
    {
        private readonly QuizMasterDbContext _context;
        public StudentAnswerRepository(QuizMasterDbContext context)
        {
            _context = context;
        }
        public async Task<ICollection<StudentAnswer>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StudentAnswers.ToListAsync(cancellationToken);
        }
        public async Task<StudentAnswer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.StudentAnswers.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }
        public async Task<ICollection<StudentAnswer>> GetByQuizAttemptIdAsync(int quizAttemptId, CancellationToken cancellationToken = default)
        {
            return await _context.StudentAnswers
                .Where(sa => sa.QuizAttemptId == quizAttemptId)
                .ToListAsync(cancellationToken);
        }
        public async Task AddAsync(StudentAnswer studentAnswer, CancellationToken cancellationToken = default)
        {
            await _context.StudentAnswers.AddAsync(studentAnswer, cancellationToken);
        }
        public void Update(StudentAnswer studentAnswer)
        {
            _context.StudentAnswers.Update(studentAnswer);
        }
        public void Delete(StudentAnswer studentAnswer)
        {
            _context.StudentAnswers.Remove(studentAnswer);
        }

    }
}
