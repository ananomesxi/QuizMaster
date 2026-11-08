using QuizMasterDomain.DTOs.UserDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IAccountService
    {
        Task<UserProfileDto> GetUserProfileAsync(string userId, CancellationToken cancellationToken);
        Task<UserProfileDto> UpdateUserProfileAsync(string userId, UpdateProfileDto updateProfileDto, CancellationToken cancellationToken);
        Task ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto, CancellationToken cancellationToken);
        Task<UserProfileDto> UpdateProfilePhotoAsync( string userId, Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default);

        Task<UserProfileDto> DeleteProfilePhotoAsync( string userId, CancellationToken cancellationToken = default);
    }
}
