using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Attendances
{
    // Body for PUT api/wfhrequest/{id}/reject.
    public class WfhRejectRequestDto
    {
        [Required]
        public string Reason { get; set; }
    }
}
