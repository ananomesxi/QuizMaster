using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AnswerOptionDtos;

namespace QuizMasterWeb.Controllers
{
    /// <summary>
    /// მართავს პასუხის ვარიანტებთან დაკავშირებულ API მოთხოვნებს.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class AnswerOptionsController : ControllerBase
    {
        private readonly IAnswerOptionService _answerOptionService;
        public AnswerOptionsController(IAnswerOptionService answerOptionService)
        {
            _answerOptionService = answerOptionService;
        }

        /// <summary>
        /// აბრუნებს მითითებული კითხვის პასუხის ვარიანტებს.
        /// </summary>
        /// <param name="questionId">კითხვის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpGet("questions/{questionId:int}/answer-options")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<ICollection<AnswerOptionDto>>> GetByQuestionId(int questionId, CancellationToken cancellationToken)
        {
            var answerOptions = await _answerOptionService.GetByQuestionIdAsync(questionId, cancellationToken);
            return Ok(answerOptions);
        }

        /// <summary>
        /// აბრუნებს პასუხის ვარიანტის დეტალებს მისი ID-ის მიხედვით.
        /// </summary>
        /// <param name="id">პასუხის ვარიანტის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpGet("answer-options/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<AnswerOptionDto>> GetById (int id, CancellationToken cancellationToken)
        {
            var answerOption = await _answerOptionService.GetByIdAsync(id, cancellationToken);
            return Ok(answerOption);
        }

        /// <summary>
        /// უმატებს მითითებულ კითხვას პასუხის ახალ ვარიანტს.
        /// </summary>
        /// <param name="questionId">კითხვის იდენტიფიკატორი.</param>
        /// <param name="dto">შესაქმნელი პასუხის ვარიანტის მონაცემები.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPost("questions/{questionId:int}/answer-options")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<AnswerOptionDto>> AddAnswerOption(int questionId, [FromBody] CreateAnswerOptionDto dto, CancellationToken cancellationToken)
        {
            var answerOption = await _answerOptionService.AddAsync(questionId, dto, cancellationToken);

            return Created($"/api/answer-options/{answerOption.Id}", answerOption);
        }

        /// <summary>
        /// აახლებს მითითებული პასუხის ვარიანტის მონაცემებს.
        /// </summary>
        /// <param name="id">პასუხის ვარიანტის იდენტიფიკატორი.</param>
        /// <param name="dto">პასუხის ვარიანტის განახლებული მონაცემები.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPut("answer-options/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<ActionResult<AnswerOptionDto>> UpdateAnswerOption(int id, [FromBody] UpdateAnswerOptionDto dto, CancellationToken cancellationToken)
        {
            var updatedAnswerOption = await _answerOptionService.UpdateAsync(id, dto, cancellationToken);
            return Ok(updatedAnswerOption);
        }

        /// <summary>
        /// შლის მითითებულ პასუხის ვარიანტს.
        /// </summary>
        /// <param name="id">წასაშლელი პასუხის ვარიანტის იდენტიფიკატორი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpDelete("answer-options/{id:int}")]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> DeleteAnswerOption(int id, CancellationToken cancellationToken)
        {
            await _answerOptionService.DeleteAsync(id, cancellationToken);
            return NoContent();
        }

    }
}
