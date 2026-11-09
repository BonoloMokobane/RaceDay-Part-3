
namespace RaceDay.API.Models
{
    public class Result
    {
        public int ResultID { get; set; }
        public int EnrollmentID { get; set; }

        public TimeSpan? FinishTime { get; set; }
        public int? FinishingPosition { get; set; }

        public Enrollment Enrollment { get; set; } = null!;
    }
}
