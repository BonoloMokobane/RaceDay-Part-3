
namespace RaceDay.API.Models
{
    public class Event
    {
        public int EventID { get; set; }
        public int OrganizerID { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string? BannerImageUrl { get; set; }

        public Organizer Organizer { get; set; } = null!;
        public ICollection<Category> Categories { get; set; }
            = new List<Category>();
        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}
