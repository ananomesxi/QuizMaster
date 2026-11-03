using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AnswerOptionDtos;

namespace QuizMasterWeb.Controllers
{
    [ApiController]
    [Route("api")]
    public class AnswerOptionsController : ControllerBase
    {
        private readonly IAnswerOptionService _answerOptionService;
        public AnswerOptionsController(IAnswerOptionService answerOptionService)
        {
            _answerOptionService = answerOptionService;
        }

        [HttpGet("questions/{questionId:int}/answer-options")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<ICollection<AnswerOptionDto>>> GetByQuestionId(int questionId, CancellationToken cancellationToken)
        {
            var answerOptions = await _answerOptionService.GetByQuestionIdAsync(questionId, cancellationToken);
            return Ok(answerOptions);
        }

        [HttpGet("answer-options/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<AnswerOptionDto>> GetById (int id, CancellationToken cancellationToken)
        {
            var answerOption = await _answerOptionService.GetByIdAsync(id, cancellationToken);
            return Ok(answerOption);
        }

        [HttpPost("questions/{questionId:int}/answer-options")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<AnswerOptionDto>> AddAnswerOption(int questionId, [FromBody] CreateAnswerOptionDto dto, CancellationToken cancellationToken)
        {
            var answerOption = await _answerOptionService.AddAsync(questionId, dto, cancellationToken);

            return Created($"/api/answer-options/{answerOption.Id}", answerOption);
        }

        [HttpPut("answer-options/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<AnswerOptionDto>> UpdateAnswerOption(int id, [FromBody] UpdateAnswerOptionDto dto, CancellationToken cancellationToken)
        {
            var updatedAnswerOption = await _answerOptionService.UpdateAsync(id, dto, cancellationToken);
            return Ok(updatedAnswerOption);
        }

        [HttpDelete("answer-options/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> DeleteAnswerOption(int id, CancellationToken cancellationToken)
        {
            await _answerOptionService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

    }
}
