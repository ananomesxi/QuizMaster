using QuizMasterDomain.DTOs.Quiz;
using QuizMasterDomain.DTOs.QuizDtos;
using QuizMasterDomain.Entities;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IQuizService
    {
        Task<ICollection<QuizDto>> GetAllQuizzesAsync(CancellationToken cancellationToken);

        Task<QuizDto?> GetQuizByIdAsync(int id, CancellationToken cancellationToken);
        Task<QuizDto> AddQuizAsync(CreateQuizDto createQuizDto, CancellationToken cancellationToken);
        Task<QuizDto?> UpdateQuizAsync(int id, UpdateQuizDto updateQuizDto, CancellationToken cancellationToken);
        Task<bool> DeleteQuizAsync(int id, CancellationToken cancellationToken);
        Task<bool> PublishQuizAsync(int id, CancellationToken cancellationToken);
        Task<bool> UnpublishQuizAsync(int id, CancellationToken cancellationToken);
        Task<ICollection<QuizDto>> GetPublishedQuizzesAsync(CancellationToken cancellationToken);
    }
}
