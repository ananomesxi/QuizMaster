using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.QuizAttemptDtos;
using System.Security.Claims;

namespace QuizMasterWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizAttemptsController : ControllerBase
    {
        private readonly IQuizAttemptService _quizAttemptService;
        public QuizAttemptsController (IQuizAttemptService quizAttemptService)
        {
            _quizAttemptService = quizAttemptService;
        }

        [HttpPost("quizzes/{quizId:int}/attempts")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<QuizAttemptDto>> StartAttempt(int quizId, CancellationToken cancellationToken)
        {
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (studentIdClaim == null)
            {
                return Unauthorized();
            }

            var studentId = studentIdClaim.Value;

            var attempt = await _quizAttemptService.StartAttemptAsync(quizId, studentId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, attempt);
        }

        [HttpGet("attempts/{attemptId:int}/questions")]
        [Authorize(Roles = "Student")]
        public async Task <ActionResult<ICollection<AttemptQuestionDto>>> GetQuestionsForAttempt(int attemptId, CancellationToken cancellationToken)
        {
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (studentIdClaim == null)
            {
                return Unauthorized();
            }
            var studentId = studentIdClaim.Value;
            var questions = await _quizAttemptService.GetQuestionsForAttemptAsync(attemptId, studentId, cancellationToken);
            return Ok(questions);
        }
        [HttpPost("attempts/{attemptId:int}/answers")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SaveAnswer (int attemptId, [FromBody] SaveAnswerDto dto, CancellationToken cancellationToken)
        {
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (studentIdClaim == null)
            {
                return Unauthorized();
            }
            var studentId = studentIdClaim.Value;
            await _quizAttemptService.SaveAnswerAsync(attemptId, studentId, dto, cancellationToken);
            return Ok();
        }

        [HttpPost("attempts/{attemptId:int}/finish")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<QuizResultDto>> FinishAttempt(int attemptId, CancellationToken cancellationToken)
        {
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (studentIdClaim == null)
            {
                return Unauthorized();
            }
            var studentId = studentIdClaim.Value;
            var result = await _quizAttemptService.FinishAttemptAsync(attemptId, studentId, cancellationToken);
            return Ok(result);
        }

        [HttpGet("attempts/my")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ICollection<QuizAttemptDto>>> GetMyattempts(CancellationToken cancellationToken)
        {
            var studentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if ( studentIdClaim == null)
            {
                return Unauthorized();
            }
            var studentId = studentIdClaim.Value;
            var attempts = await _quizAttemptService.GetMyAttemptsAsync(studentId, cancellationToken);
            return Ok(attempts);
        }
    }
}
