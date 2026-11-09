
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class ResultViewModel
{
    public int ResultId { get; set; }
    public int EnrollmentId { get; set; }
    public int EventId { get; set; }

    public string ParticipantName { get; set; } = "";
    public string EventName { get; set; } = "";

    [Required]
    public TimeSpan FinishTime { get; set; }

    [Range(1, 100000)]
    public int FinishingPosition { get; set; }
}
