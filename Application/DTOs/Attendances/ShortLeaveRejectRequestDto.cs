using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    // Body for PUT api/shortleaverequest/{id}/reject.
    public class ShortLeaveRejectRequestDto
    {
        [Required]
        public string Reason { get; set; }
    }
}
