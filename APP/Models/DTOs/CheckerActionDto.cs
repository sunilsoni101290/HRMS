using System.ComponentModel.DataAnnotations;

namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.EmployeeLifecycle.CheckerActionDto exactly -
    // property names/types must match the API's JSON 1:1 or model binding
    // silently breaks. Shared shape for both Approve and Reject (the
    // Checker's action) across every Maker-Checker feature in the
    // "Probation & Confirmation" (Employee Lifecycle) module - Probation
    // Confirmation, PIP, and Employee Transfer all POST/PUT this same
    // shape to their respective approve/reject endpoints. CheckerRemarks is
    // optional for Approve, required for Reject - enforced server-side,
    // not via attributes here.
    public class CheckerActionDto
    {
        [MaxLength(1000)]
        public string? CheckerRemarks { get; set; }
        public string? TenantId { get; set; }
        public string? ActingUserId { get; set; }
    }
}
