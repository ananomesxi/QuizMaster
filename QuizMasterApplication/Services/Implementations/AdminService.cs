using Microsoft.AspNetCore.Identity;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AdminDtos;
using QuizMasterDomain.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AdminService (UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public async Task ChangeUserRoleAsync(string userId, ChangeUserRoleDto dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.RoleName))
            {
                throw new ArgumentException("Role name is required.");
            }
            var user = _userManager.FindByIdAsync(userId).Result;
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }
            var roleName = dto.RoleName.Trim();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                throw new KeyNotFoundException($"Role '{roleName}' does not exist.");
            }
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Count>0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to remove user from current roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                }
            }
            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to add user to role: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
            }
        }
    }
}
