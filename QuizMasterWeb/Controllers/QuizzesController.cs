using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.Quiz;
using QuizMasterDomain.DTOs.QuizDtos;
using QuizMasterDomain.DTOs.QuizResultDtos;
using System.Security.Claims;

namespace QuizMasterWeb.Controllers
{
    /// <summary>
    /// მართავს ქვიზების, ლიდერბორდისა და სტატისტიკის API მოთხოვნებს.
    /// </summary>
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

        /// <summary>
        /// აბრუნებს გამოქვეყნებულ ქვიზებს.
        /// </summary>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpGet("published")]
        public async Task<ActionResult<ICollection<QuizDto>>> GetPublishedQuizzes(CancellationToken cancellationToken)
        {
            var quizzes = await _quizService.GetPublishedQuizzesAsync(cancellationToken);
            return Ok(quizzes);
        }

        /// <summary>
        /// აბრუნებს ყველა ქვიზს.
        /// </summary>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpGet]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<ICollection<QuizDto>>> GetAllQuizzes(CancellationToken cancellationToken)
        {
            var quizzes = await _quizService.GetAllQuizzesAsync(cancellationToken);
            return Ok(quizzes);
        }

        /// <summary>
        /// აბრუნებს ქვიზის დეტალებს მისი ID-ის მიხედვით.
        /// </summary>
        /// <param name="id">ქვიზის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
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

        /// <summary>
        /// ამატებს ახალ ქვიზს და მის შემქმნელად ინახავს ავტორიზებულ ლექტორს/ადმინს.
        /// </summary>
        /// <param name="dto">ქვიზის მონაცემები.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPost]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuizDto>> CreateQuiz([FromBody] CreateQuizDto dto, CancellationToken cancellationToken)
        {
            var creatorIdClaim = User.FindFirst( ClaimTypes.NameIdentifier);
            if (creatorIdClaim == null || string.IsNullOrWhiteSpace(creatorIdClaim.Value))
            {
                return Unauthorized();
            }
            var quiz = await _quizService.AddQuizAsync( dto, cancellationToken, creatorIdClaim.Value);
            return CreatedAtAction( nameof(GetQuizById), new { id = quiz.Id }, quiz);
        }

        /// <summary>
        /// აახლებს ქვიზის დეტალებს მისი ID-ის მიხედვით.
        /// </summary>
        /// <param name="id">ქვიზის იდენტიფიკატორი.</param>
        /// <param name="dto">ქვიზის განახლებული მონაცემები.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
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

        /// <summary>
        /// შლის ქვიზს მისი ID-ის მიხედვით, თუ მას არ აქვს მცდელობების ისტორია.
        /// </summary>
        /// <param name="id">ქვიზის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> DeleteQuiz (int id, CancellationToken cancellationToken)
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


        /// <summary>
        /// აქვეყნებს ქვიზს მისი ID-ის მიხედვით.
        /// </summary>
        /// <param name="id">ქვიზის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
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

        /// <summary>
        /// აუქმებს ქვიზის გამოქვეყნებას მისი ID-ის მიხედვით.
        /// </summary>
        /// <param name="id">ქვიზის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
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


        /// <summary>
        /// აბრუნებს ქვიზის ლიდერბორდს მისი ID-ის მიხედვით.
        /// </summary>
        /// <param name="quizId">ქვიზის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpGet("{quizId:int}/leaderboard")]
        [Authorize(Roles = "Student,Lecturer,Admin")]
        public async Task<ActionResult<ICollection<LeaderboardEntryDto>>> GetLeaderboard(int quizId, CancellationToken cancellationToken)
        {
            var leaderboard = await _quizResultService.GetLeaderboardResultsByQuizIdAsync(quizId, cancellationToken);
            return Ok(leaderboard);
        }

        /// <summary>
        /// აბრუნებს ქვიზის სტატისტიკას მისი ID-ის მიხედვით.
        /// </summary>
        /// <param name="quizId">ქვიზის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpGet("{quizId:int}/statistics")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuizStatisticsDto>> GetStatistics(int quizId, CancellationToken cancellationToken)
        {
            var statistics = await _quizResultService.GetQuizStatisticsAsync(quizId, cancellationToken);
            return Ok(statistics);
        }
    }
}
