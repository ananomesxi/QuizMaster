using QuizMasterDomain.DTOs.AuthDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IUserService
    {
        Task RegisterUserAsync(RegisterDto dto, CancellationToken cancellationToken = default);
        Task VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken = default);

    }
}
