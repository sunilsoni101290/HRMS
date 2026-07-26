using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    // Body for PUT api/compoff/{id}/reject.
    public class CompOffRejectRequestDto
    {
        [Required]
        public string Reason { get; set; }
    }
}
