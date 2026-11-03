using QuizMasterDomain.DTOs.QuestionDtos;
using QuizMasterDomain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IQuestionService
    {
        Task<ICollection<QuestionDto>> GetAllQuestionsAsync(CancellationToken cancellationToken);

        Task<QuestionDto> GetQuestionByIdAsync(int id, CancellationToken cancellationToken);

        Task<ICollection<QuestionDto>> GetQuestionsByQuizIdAsync(int quizId, CancellationToken cancellationToken);
        Task<QuestionDto> AddQuestionAsync(int quizId, CreateQuestionDto createQuestionDto, CancellationToken cancellationToken);
        Task<QuestionDto> UpdateQuestionAsync(int id, UpdateQuestionDto updateQuestionDto, CancellationToken cancellationToken);
        Task DeleteQuestionAsync(int id, CancellationToken cancellationToken);
        
    }
}
