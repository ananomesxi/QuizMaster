using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace QuizMasterDomain.DTOs.AdminDtos
{
    public class ChangeUserRoleDto
    {
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;
    }
}
