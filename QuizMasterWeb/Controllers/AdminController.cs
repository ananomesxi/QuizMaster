using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AdminDtos;

namespace QuizMasterWeb.Controllers
{
    /// <summary>
    /// მართავს ადმინისტრატორის ფუნქციებთან დაკავშირებულ API მოთხოვნებს.
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }
        /// <summary>
        /// ანიჭებს მომხმარებელს მითითებულ როლს.
        /// </summary>
        /// <param name="userId">მომხმარებლის იდენტიფიკატორი.</param>
        /// <param name="dto">მომხმარებლისთვის მისანიჭებელი როლი.</param>
        /// <param name="cancellationToken">ტოკენი მოთხოვნის გაუქმებისთვის.</param>
        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(string userId, [FromBody] ChangeUserRoleDto dto, CancellationToken cancellationToken)
        {
            await _adminService.ChangeUserRoleAsync(userId, dto, cancellationToken);
            return NoContent();
        }
    }
}
