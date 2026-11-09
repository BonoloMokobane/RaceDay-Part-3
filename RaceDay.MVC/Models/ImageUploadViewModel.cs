using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models
{
    public class ImageUploadViewModel
    {
        [Required]
        public IFormFile? Image { get; set; }
    }
}
