using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.EmployeeLifecycle
{
    // Maker's proposal input - EmployeeId is the subject employee (never
    // the maker themselves; the maker is resolved server-side from
    // actingUserId, see EmployeeTransferService.CreateAsync). All "To"
    // fields are optional/nullable - the caller only sets the dimensions
    // actually changing; the "From" snapshot is captured automatically
    // server-side from the Employee's current values, never supplied here.
    // At least one "To" field must be non-null and differ from the
    // corresponding current Employee value - validated in
    // EmployeeTransferService.CreateAsync (a no-op transfer proposal is
    // rejected with a clear message).
    public class CreateEmployeeTransferDto
    {
        [Required]
        [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public string Reason { get; set; }

        [Display(Name = "New Company")]
        public string? ToCompanyId { get; set; }

        [Display(Name = "New Branch")]
        public string? ToBranchId { get; set; }

        [Display(Name = "New Department")]
        public string? ToDepartmentId { get; set; }

        [Display(Name = "New Designation")]
        public string? ToDesignationId { get; set; }

        [Display(Name = "New Reporting Manager")]
        public string? ToReportingManagerId { get; set; }
    }
}
