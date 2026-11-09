
namespace RaceDay.API.DTOs
{
    public class CategoryResponse
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

