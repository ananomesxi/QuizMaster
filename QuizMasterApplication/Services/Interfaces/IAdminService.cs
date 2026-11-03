using QuizMasterDomain.DTOs.AdminDtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterApplication.Services.Interfaces
{
    public interface IAdminService
    {
        Task ChangeUserRoleAsync(string userId, ChangeUserRoleDto dto, CancellationToken cancellationToken = default);
    }
}
