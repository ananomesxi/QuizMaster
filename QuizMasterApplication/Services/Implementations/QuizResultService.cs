using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.QuizResultDtos;
using QuizMasterDomain.Interfaces;

namespace QuizMasterApplication.Services.Implementations
{
    public class QuizResultService : IQuizResultService
    {
        private readonly IQuizResultRepository _quizResultRepository;
        private readonly IQuizRepository _quizRepository;
        public QuizResultService(IQuizResultRepository quizResultRepository, IQuizRepository quizRepository)
        {
            _quizResultRepository = quizResultRepository;
            _quizRepository = quizRepository;
        }
        public async Task<ICollection<LeaderboardEntryDto>> GetLeaderboardResultsByQuizIdAsync(int quizId, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {quizId} not found.");
            }
            var results = await _quizResultRepository.GetLeaderboardResultsByQuizIdAsync(quizId, cancellationToken);
            var bestResults = results
                .GroupBy(result => result.QuizAttempt.UserId)
                .Select(group => group.OrderByDescending(result => result.Score).First())
                .OrderByDescending(result => result.Score)
                .ThenBy(result => result.QuizAttempt.User.UserName)
                .ToList();
            return bestResults.Select((result, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserName = result.QuizAttempt.User.UserName ?? "Unknown User",
                Score = result.Score,
                MaxScore = result.MaxScore,
            }).ToList();
        }

        public async Task<QuizStatisticsDto> GetQuizStatisticsAsync(int quizId, CancellationToken cancellationToken = default)
        {
            var quiz = await _quizRepository.GetByIdAsync(quizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {quizId} not found.");
            }  
            return await _quizResultRepository.GetQuizStatisticsAsync(quizId, cancellationToken);
        }

    }
}
