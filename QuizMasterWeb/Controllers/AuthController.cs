using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AuthDtos;

namespace QuizMasterWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        public AuthController(IUserService userService, IAuthService authService)
        {
            _userService = userService;
            _authService = authService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register ([FromBody] RegisterDto dto, CancellationToken cancellationToken = default)
        {
            await _userService.RegisterUserAsync(dto, cancellationToken);
            return Ok("User registered successfully. Please check your email for verification code.");
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail ([FromBody] VerifyEmailDto dto, CancellationToken cancellationToken = default)
        {
            await _userService.VerifyEmailAsync(dto, cancellationToken);
            return Ok("Email verified successfully. You can now log in.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login ([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _authService.LoginUserAsync(dto, cancellationToken);
            return Ok(response);
        }

    }
}
