using System;
using System.Collections.Generic;
using System.Text;

namespace QuizMasterDomain.DTOs.UserDtos
{
    public class UserProfileDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public ICollection<string> Roles { get; set; } = new List<string>();
    }
}
