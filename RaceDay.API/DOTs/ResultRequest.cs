
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class ResultRequest
    {
        [Required]
        public string FinishTime { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int? FinishingPosition { get; set; }
    }
}

