
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs
{
    public class EnrollmentRequest
    {
        [Range(1, int.MaxValue)]
        public int EventID { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryID { get; set; }
    }
}
