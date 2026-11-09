
using Microsoft.Extensions.Logging;

namespace RaceDay.API.Models
{
    public class Organizer
    {
        public int OrganizerID { get; set; }
        public int UserID { get; set; }

        public User User { get; set; } = null!;
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}

