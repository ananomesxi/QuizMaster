using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.Quiz;
using QuizMasterDomain.DTOs.QuizDtos;
using QuizMasterDomain.DTOs.QuizResultDtos;
namespace QuizMasterWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizService _quizService;
        private readonly IQuizResultService _quizResultService;
        public QuizzesController(IQuizService quizService, IQuizResultService quizResultService)
        {
            _quizService = quizService;
            _quizResultService = quizResultService;
        }
        [HttpGet("published")]
        public async Task<ActionResult<ICollection<QuizDto>>> GetPublishedQuizzes(CancellationToken cancellationToken)
        {
            var quizzes = await _quizService.GetPublishedQuizzesAsync(cancellationToken);
            return Ok(quizzes);
        }
        [HttpGet]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<ICollection<QuizDto>>> GetAllQuizzes(CancellationToken cancellationToken)
        {
            var quizzes = await _quizService.GetAllQuizzesAsync(cancellationToken);
            return Ok(quizzes);
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuizDto>> GetQuizById (int id, CancellationToken cancellationToken)
        {
            var quiz = await _quizService.GetQuizByIdAsync(id, cancellationToken);
            if (quiz == null)
            {
                return NotFound();
            }
            return Ok(quiz);
        }
        [HttpPost]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuizDto>> CreateQuiz([FromBody] CreateQuizDto dto, CancellationToken cancellationToken)
        {
            var quiz = await _quizService.AddQuizAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetQuizById), new { id = quiz.Id }, quiz);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuizDto>> UpdateQuiz(int id, [FromBody] UpdateQuizDto dto, CancellationToken cancellationToken)
        {
            var updatedQuiz = await _quizService.UpdateQuizAsync(id, dto, cancellationToken);
            if (updatedQuiz == null) {
                return NotFound();
            }
            return Ok(updatedQuiz);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> DeleteQUiz (int id, CancellationToken cancellationToken)
        {
            var quiz = await _quizService.GetQuizByIdAsync(id, cancellationToken);
            if (quiz == null)
            {
                return NotFound();
            }
            var deleted = await _quizService.DeleteQuizAsync(id, cancellationToken);
            if (!deleted)
            {
                return Conflict("A quiz with attempt history cannot be deleted.");
            }
            return NoContent();
        }
        [HttpPatch("{id:int}/publish")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> PublishQuiz(int id, CancellationToken cancellationToken)
        {
            var published = await _quizService.PublishQuizAsync(id, cancellationToken);
            if (!published)
            {
                return NotFound();
            }
            return NoContent();
        }
        [HttpPatch("{id:int}/unpublish")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> UnpublishQuiz(int id, CancellationToken cancellationToken)
        {
            var closed = await _quizService.UnpublishQuizAsync(id, cancellationToken);
            if (!closed)
            {
                return NotFound();
            }
            return NoContent();
        }

        [HttpGet("{quizId:int}/leaderboard")]
        [Authorize(Roles = "Student,Lecturer,Admin")]
        public async Task<ActionResult<ICollection<LeaderboardEntryDto>>> GetLeaderboard(int quizId, CancellationToken cancellationToken)
        {
            var leaderboard = await _quizResultService.GetLeaderboardResultsByQuizIdAsync(quizId, cancellationToken);
            return Ok(leaderboard);
        }
        [HttpGet("{quizId:int}/statistics")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuizStatisticsDto>> GetStatistics(int quizId, CancellationToken cancellationToken)
        {
            var statistics = await _quizResultService.GetQuizStatisticsAsync(quizId, cancellationToken);
            return Ok(statistics);
        }
    }
}
