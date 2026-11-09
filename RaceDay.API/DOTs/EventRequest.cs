
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class EventRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "1000")]
        public decimal Distance { get; set; }

        [Required]
        [RegularExpression("^(Run|Walk|Cycle)$",
            ErrorMessage = "EventType must be Run, Walk, or Cycle.")]
        public string EventType { get; set; } = string.Empty;

        [Url]
        public string? BannerImageUrl { get; set; }
    }
}
