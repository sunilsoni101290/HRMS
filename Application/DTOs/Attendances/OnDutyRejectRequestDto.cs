using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    // Body for PUT api/ondutyrequest/{id}/reject.
    public class OnDutyRejectRequestDto
    {
        [Required]
        public string Reason { get; set; }
    }
}
