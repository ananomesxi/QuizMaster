using Microsoft.AspNetCore.Identity;
using QuizMasterApplication.Services.Interfaces;
using QuizMasterDomain.DTOs.AuthDtos;
using QuizMasterDomain.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public UserService (UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }
        public async Task RegisterUserAsync(RegisterDto dto, CancellationToken cancellationToken = default)
        {
            var userExisting = await _userManager.FindByEmailAsync(dto.Email);
            if (userExisting != null)
            {
                throw new InvalidOperationException("User with this email already exists.");
            }
            var user = new ApplicationUser
            {
                Email = dto.Email,
                UserName = dto.UserName
            };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            var roleResult = await _userManager.AddToRoleAsync(user, "Student");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", roleResult.Errors.Select(error => error.Description)));
            }
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            await _emailService.SendEmailAsync(dto.Email, "QuizMaster email verification", $"Your email verification code is: {code}",cancellationToken);

        }

        public async Task VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, dto.Code);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
