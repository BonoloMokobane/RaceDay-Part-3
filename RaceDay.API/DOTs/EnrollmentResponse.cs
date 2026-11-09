
namespace RaceDay.API.DTOs
{
    public class EnrollmentResponse
    {
        public int EnrollmentID { get; set; }
        public int ParticipantID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime EnrollmentDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
