
namespace RaceDay.API.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public Event Event { get; set; } = null!;
        public ICollection<Enrollment> Enrollments { get; set; }
            = new List<Enrollment>();
    }
}
