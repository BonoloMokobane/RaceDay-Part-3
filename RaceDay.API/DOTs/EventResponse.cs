
namespace RaceDay.API.DTOs
{
    public class EventResponse
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
    }
}
