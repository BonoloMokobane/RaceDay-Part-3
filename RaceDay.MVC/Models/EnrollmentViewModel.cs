
namespace RaceDay.MVC.Models;

public class EnrollmentViewModel
{
    public int EnrollmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }

    public string EventName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string ParticipantName { get; set; } = "";

    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = "";
}
