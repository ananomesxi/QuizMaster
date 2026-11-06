using Microsoft.AspNetCore.Identity;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.UserDtos;
using QuizMasterDomain.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }
            var result = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Failed to change password");
            }
            
        }

        public async Task<UserProfileDto> GetUserProfileAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }
            return await MapToProfileDtoAsync(user);
        }

        public async Task<UserProfileDto> UpdateUserProfileAsync(string userId, UpdateProfileDto updateProfileDto, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }
           
            var result = await _userManager.SetUserNameAsync(user, updateProfileDto.UserName);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Failed to update user profile");
            }
            return await MapToProfileDtoAsync(user);
        }

        private async Task<UserProfileDto> MapToProfileDtoAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            };
        }
    }
}
