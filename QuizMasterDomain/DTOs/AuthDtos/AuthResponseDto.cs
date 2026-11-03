using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.AuthDtos
{
    public class AuthResponseDto
    {
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
