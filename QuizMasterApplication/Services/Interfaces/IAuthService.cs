using QuizMasterDomain.DTOs.AuthDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginUserAsync(LoginDto dto, CancellationToken cancellationToken = default);
    }
}
