using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AdminDtos;

namespace QuizMasterWeb.Controllers
{
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
        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> ChangeUserRole(string userId, [FromBody] ChangeUserRoleDto dto, CancellationToken cancellationToken)
        {
            await _adminService.ChangeUserRoleAsync(userId, dto, cancellationToken);
            return NoContent();
        }
    }
}
