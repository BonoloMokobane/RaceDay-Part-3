
namespace RaceDay.API.DTOs
{
    public class ResultResponse
    {
        public int ResultID { get; set; }
        public int EnrollmentID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = string.Empty;
        public TimeSpan? FinishTime { get; set; }
        public int? FinishingPosition { get; set; }
    }
}
