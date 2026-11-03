using QuizMasterDomain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.Interfaces
{
    public interface IAnswerOptionRepository
    {
        Task<ICollection<AnswerOption>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<AnswerOption?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<AnswerOption>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken = default);
        Task AddAsync(AnswerOption answerOption, CancellationToken cancellationToken = default);
        void Update(AnswerOption answerOption);
        public void Delete(AnswerOption answerOption);
    }
}
