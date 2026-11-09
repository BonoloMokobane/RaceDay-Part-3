
namespace RaceDay.API.Models
{
    public class Participant
    {
        public int ParticipantID { get; set; }
        public int UserID { get; set; }

        public User User { get; set; } = null!;
        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}
