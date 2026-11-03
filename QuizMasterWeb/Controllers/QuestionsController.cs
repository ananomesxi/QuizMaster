using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.QuestionDtos;
using QuizMasterDomain.Interfaces;

namespace QuizMasterWeb.Controllers
{
    [ApiController]
    [Route("api")]
    public class QuestionsController : ControllerBase            
    {
        private readonly IQuestionService _questionService;
        public QuestionsController(IQuestionService questionService)
        {
            _questionService = questionService;
        }

        [HttpGet("quizzes/{quizId:int}/questions")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<ICollection<QuestionDto>>> GetQuestionsByQuizId(int quizId, CancellationToken cancellationToken)
        {
            var questions = await _questionService.GetQuestionsByQuizIdAsync(quizId, cancellationToken);
            return Ok(questions);
        }

        [HttpPost("quizzes/{quizId:int}/questions")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuestionDto>> AddQuestion(int quizId, [FromBody] CreateQuestionDto dto, CancellationToken cancellationToken)
        {
            var question = await _questionService.AddQuestionAsync(quizId, dto, cancellationToken);
            return Created($"/api/questions/{question.Id}", question);
        }

        [HttpGet("questions/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuestionDto>> GetQuestionById(int id, CancellationToken cancellationToken)
        {
            var question = await _questionService.GetQuestionByIdAsync(id, cancellationToken);
            return Ok(question);
        }

        [HttpPut("questions/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<QuestionDto>> UpdateQuestion(int id, [FromBody] UpdateQuestionDto dto, CancellationToken cancellationToken)
        {
            var updatedQuestion = await _questionService.UpdateQuestionAsync(id,dto, cancellationToken);
            
            return Ok(updatedQuestion);
        }

        [HttpDelete("questions/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> DeleteQuestion(int id, CancellationToken cancellationToken)
        {
            await _questionService.DeleteQuestionAsync(id, cancellationToken);
      
            return NoContent();
        }
    }
}
