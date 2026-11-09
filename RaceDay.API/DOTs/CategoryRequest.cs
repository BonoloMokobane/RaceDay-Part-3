
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class CategoryRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
