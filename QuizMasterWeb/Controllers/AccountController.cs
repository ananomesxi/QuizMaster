using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.UserDtos;
using System.Security.Claims;

namespace QuizMasterWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var profile = await _accountService.GetUserProfileAsync(
                userIdClaim.Value, cancellationToken);

            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<ActionResult<UserProfileDto>> UpdateMyProfile([FromBody] UpdateProfileDto updateDto, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var profile = await _accountService.UpdateUserProfileAsync(
                userIdClaim.Value, updateDto, cancellationToken);

            return Ok(profile);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordDto dto,
            CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            await _accountService.ChangePasswordAsync(
                userIdClaim.Value, dto, cancellationToken);

            return NoContent();
        }

    }
}
