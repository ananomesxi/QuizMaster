using Microsoft.AspNetCore.Identity;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.UserDtos;
using QuizMasterDomain.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using QuizMasterDomain.Interfaces;
using Microsoft.Extensions.Logging;

namespace QuizMasterApplication.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<AccountService> _logger;

        public AccountService( UserManager<ApplicationUser> userManager, IFileStorageService fileStorageService, ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _fileStorageService = fileStorageService;
            _logger = logger;
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
        public async Task<UserProfileDto> UpdateProfilePhotoAsync( string userId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var oldPath = user.ProfileImageUrl;

            var newPath = await _fileStorageService.SaveFileAsync(
                stream, fileName, contentType, cancellationToken);

            user.ProfileImageUrl = newPath;

            try
            {
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to update profile photo.");
                }
            }
            catch
            {
                user.ProfileImageUrl = oldPath;

                await TryDeletePhotoAsync(newPath);

                throw;
            }

            await TryDeletePhotoAsync(oldPath);

            return await MapToProfileDtoAsync(user);
        }
        public async Task<UserProfileDto> DeleteProfilePhotoAsync( string userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var oldPath = user.ProfileImageUrl;

            if (oldPath == null)
            {
                return await MapToProfileDtoAsync(user);
            }

            user.ProfileImageUrl = null;

            try
            {
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to remove profile photo.");
                }
            }
            catch
            {
                user.ProfileImageUrl = oldPath;
                throw;
            }

            await TryDeletePhotoAsync(oldPath);

            return await MapToProfileDtoAsync(user);
        }
        private async Task TryDeletePhotoAsync(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                await _fileStorageService.DeleteFileAsync( path, CancellationToken.None);
            }
            catch (Exception exception)
            {
                _logger.LogWarning( exception, "Could not clean up a profile photo file.");
            }
        }
        private async Task<UserProfileDto> MapToProfileDtoAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserProfileDto
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                ProfileImageUrl = user.ProfileImageUrl
            };
        }

    }
}
