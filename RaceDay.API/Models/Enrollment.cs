
namespace RaceDay.API.Models
{
    public class Enrollment
    {
        public int EnrollmentID { get; set; }
        public int ParticipantID { get; set; }
        public int EventID { get; set; }
        public int CategoryID { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Enrolled";

        public Participant Participant { get; set; } = null!;
        public Event Event { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public Result? Result { get; set; }
    }
}

