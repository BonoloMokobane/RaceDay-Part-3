using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class EventViewModel
{
    public int EventId { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    [Required]
    public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

    [Required]
    public string Location { get; set; } = "";

    [Range(1, 1000000)]
    public double Distance { get; set; }

    [Required]
    public string EventType { get; set; } = "Run";

    public string? BannerImageUrl { get; set; }
}