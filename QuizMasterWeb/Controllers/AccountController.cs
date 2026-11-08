using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.UserDtos;
using QuizMasterWeb.Models;
using System.Security.Claims;
using QuizMasterWeb.Models;
namespace QuizMasterWeb.Controllers
{
    /// <summary>
    /// მართავს მომხმარებლის პროფილისა და პაროლის API მოთხოვნებს.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        /// <summary>
        /// აბრუნებს ავტორიზებული მომხმარებლის პროფილს.
        /// </summary>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
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

        /// <summary>
        /// აახლებს ავტორიზებული მომხმარებლის პროფილის მონაცემებს.
        /// </summary>
        /// <param name="updateDto">პროფილის განახლებული მონაცემები.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
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

        /// <summary>
        /// ცვლის ავტორიზებული მომხმარებლის პაროლს.
        /// </summary>
        /// <param name="dto">მიმდინარე და ახალი პაროლები.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
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

        /// <summary>
        /// აახლებს ავტორიზებული მომხმარებლის პროფილის ფოტოს.
        /// </summary>
        /// <param name="request">ასატვირთი ფოტო.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPut("me/photo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(3 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 2 * 1024 * 1024)]
        public async Task<ActionResult<UserProfileDto>> UpdateProfilePhoto( [FromForm] UploadProfilePhotoRequest request, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized();
            }

            var file = request.File;

            if (file.Length == 0)
            {
                throw new ArgumentException("Choose a non-empty image.");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException( "The profile photo must be 2 MB or smaller.");
            }

            await using var stream = file.OpenReadStream();

            var profile = await _accountService.UpdateProfilePhotoAsync( userIdClaim.Value, stream, file.FileName, file.ContentType, cancellationToken);

            return Ok(profile);
        }

        /// <summary>
        /// შლის ავტორიზებული მომხმარებლის პროფილის ფოტოს.
        /// </summary>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpDelete("me/photo")]
        public async Task<ActionResult<UserProfileDto>> DeleteProfilePhoto( CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier); 
            if (userIdClaim == null)
            {
                return Unauthorized();
            } 
            var profile = await _accountService.DeleteProfilePhotoAsync( userIdClaim.Value, cancellationToken); 
            return Ok(profile);
        }

    }
}
