using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace QuizMasterDomain.Identity
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(256)]
        public string? ProfileImageUrl { get; set; }
    }
}
