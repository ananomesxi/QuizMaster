using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace QuizMasterDomain.DTOs.UserDtos
{
    public class UpdateProfileDto
    {
        [Required]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;
    }
}
