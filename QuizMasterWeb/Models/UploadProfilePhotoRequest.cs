using System.ComponentModel.DataAnnotations;

namespace QuizMasterWeb.Models
{
    public class UploadProfilePhotoRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}