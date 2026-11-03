using QuizMasterDomain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Interfaces
{
    public interface IQuestionRepository
    {
        Task<ICollection<Question>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<ICollection<Question>> GetByQuizIdAsync(int quizId, CancellationToken cancellationToken = default);
        Task AddAsync(Question question, CancellationToken cancellationToken = default);
        void Update(Question question);
        void Delete(Question question);
    }
}
