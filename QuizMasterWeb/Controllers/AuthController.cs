using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AuthDtos;

namespace QuizMasterWeb.Controllers
{
    /// <summary>
    /// მართავს მომხმარებლის რეგისტრაციას, ელფოსტის დადასტურებას და სისტემაში შესვლას.
    /// </summary>
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

        /// <summary>
        /// არეგისტრირებს ახალ მომხმარებელს და ელფოსტაზე აგზავნის დადასტურების კოდს.
        /// </summary>
        /// <param name="dto">მომხმარებლის რეგისტრაციის დეტალები.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPost("register")]
        public async Task<IActionResult> Register ([FromBody] RegisterDto dto, CancellationToken cancellationToken = default)
        {
            await _userService.RegisterUserAsync(dto, cancellationToken);
            return Ok("User registered successfully. Please check your email for verification code.");
        }

        /// <summary>
        /// ადასტურებს მომხმარებლის ელფოსტას მიღებული კოდით.
        /// </summary>
        /// <param name="dto">მომხმარებლის ელფოსტა და ვერიფიკაციის კოდი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail ([FromBody] VerifyEmailDto dto, CancellationToken cancellationToken = default)
        {
            await _userService.VerifyEmailAsync(dto, cancellationToken);
            return Ok("Email verified successfully. You can now log in.");
        }

        /// <summary>
        /// ავტორიზებს მომხმარებელს და აბრუნებს წვდომის ტოკენს.
        /// </summary>
        /// <param name="dto">მომხმარებლის ელფოსტა და პაროლი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPost("login")]
        public async Task<IActionResult> Login ([FromBody] LoginDto dto, CancellationToken cancellationToken = default)
        {
            var response = await _authService.LoginUserAsync(dto, cancellationToken);
            return Ok(response);
        }

    }
}
