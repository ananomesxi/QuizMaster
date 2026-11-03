using QuizMasterDomain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Interfaces
{
    public interface IQuizRepository
    {
        Task<ICollection<Quiz>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Quiz?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task AddAsync(Quiz quiz, CancellationToken cancellationToken = default);
        void Update(Quiz quiz);
        void Delete(Quiz quiz);
        Task<ICollection<Quiz>> GetPublishedQuizzesAsync(CancellationToken cancellationToken = default);
    }
}
