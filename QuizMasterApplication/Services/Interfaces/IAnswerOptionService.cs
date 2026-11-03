using QuizMasterDomain.DTOs.AnswerOptionDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IAnswerOptionService
    {
        Task<ICollection<AnswerOptionDto>> GetByQuestionIdAsync(int questionId, CancellationToken cancellationToken);
        Task<AnswerOptionDto> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<AnswerOptionDto> AddAsync(int questionId, CreateAnswerOptionDto createAnswerOptionDto, CancellationToken cancellationToken);
        Task<AnswerOptionDto> UpdateAsync(int id, UpdateAnswerOptionDto updateAnswerOptionDto, CancellationToken cancellationToken);
        Task DeleteAsync(int id, CancellationToken cancellationToken);
    }
}
