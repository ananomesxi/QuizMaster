using QuizMasterDomain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Interfaces
{
    public interface IQuizAttemptRepository
    {
        Task<ICollection<QuizAttempt>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<QuizAttempt?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<QuizAttempt>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task AddAsync(QuizAttempt quizAttempt, CancellationToken cancellationToken = default);
        void Update(QuizAttempt quizAttempt);
        void Delete(QuizAttempt quizAttempt);
        Task<bool> HasAttemptsForQuizAsync(int quizId, CancellationToken cancellationToken = default);
    }
}
