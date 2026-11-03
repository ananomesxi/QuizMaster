using QuizMasterDomain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Interfaces
{
    public interface IStudentAnswerRepository
    {
        Task<ICollection<StudentAnswer>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<StudentAnswer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<StudentAnswer>> GetByQuizAttemptIdAsync(int quizAttemptId, CancellationToken cancellationToken = default);
        Task AddAsync(StudentAnswer studentAnswer, CancellationToken cancellationToken = default);
        void Update(StudentAnswer studentAnswer);
        void Delete(StudentAnswer studentAnswer);
    }
}
