using QuizMasterDomain.DTOs.QuizAttemptDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IQuizAttemptService
    {
        Task<QuizAttemptDto> StartAttemptAsync(int quizId, string studentId, CancellationToken cancellationToken);
        Task<ICollection<AttemptQuestionDto>> GetQuestionsForAttemptAsync(int attemptId, string studentId, CancellationToken cancellationToken);
        Task SaveAnswerAsync(int attemptId, string studentId, SaveAnswerDto dto, CancellationToken cancellationToken);
        Task<QuizResultDto> FinishAttemptAsync(int attemptId, string studentId, CancellationToken cancellationToken);
        Task<ICollection<QuizAttemptDto>> GetMyAttemptsAsync(string studentId, CancellationToken cancellationToken);

    }
}
