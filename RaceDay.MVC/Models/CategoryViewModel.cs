
using System.ComponentModel.DataAnnotations;

namespace RaceDay.MVC.Models;

public class CategoryViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";
}
