using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.WorkTracking
{
    // ======================================================================
    // Daily Work Entry module - master/lookup DTOs. Kept simple (plain
    // property bags, DropdownDto reused for dropdown-only lookups) matching
    // this codebase's existing LeaveType/Department-style master pattern -
    // no dedicated "/lookup" endpoint convention exists here, so these are
    // exposed via ordinary GetAll() actions, same as LeaveTypeController.
    // ======================================================================

    public class ClientDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Client name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(50)]
        public string? Code { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WorkJobDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Job number is required.")]
        [MaxLength(50)]
        public string JobNumber { get; set; } = "";

        [Required(ErrorMessage = "Job name is required.")]
        [MaxLength(200)]
        public string JobName { get; set; } = "";

        [Required(ErrorMessage = "Client is required.")]
        public string ClientId { get; set; } = "";

        public string? ClientName { get; set; }

        public int Status { get; set; } = 1; // WorkJobStatus
        public string? StatusName { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class JobTypeDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Job type name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [MaxLength(30)]
        public string? Code { get; set; }

        public int DisplayOrder { get; set; }

        public bool HasDisciplines { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class JobItemDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Job is required.")]
        public string WorkJobId { get; set; } = "";

        public string? JobNumber { get; set; }

        [Required(ErrorMessage = "Item code is required.")]
        [MaxLength(100)]
        public string Code { get; set; } = "";

        [MaxLength(300)]
        public string? Description { get; set; }

        public int ItemType { get; set; } = 3; // JobItemType
        public string? ItemTypeName { get; set; }

        public decimal? TotalWeightMT { get; set; }

        public string? DocumentStatusId { get; set; }
        public string? DocumentStatusName { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WorkActivityDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Job type is required.")]
        public string JobTypeId { get; set; } = "";

        public string? JobTypeName { get; set; }

        /// <summary>SkidsDiscipline enum value - only meaningful when the Job Type is Skids Packages.</summary>
        public int? SkidsDiscipline { get; set; }
        public string? SkidsDisciplineName { get; set; }

        [Required(ErrorMessage = "Activity name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        public int WorkCategory { get; set; } = 1; // WorkCategory enum
        public string? WorkCategoryName { get; set; }

        public int DisplayOrder { get; set; }

        public bool RequiresReason { get; set; }
        public bool AllowFreeTextOther { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WorkEntryReasonDto
    {
        public string? Id { get; set; }

        [Required]
        public int Category { get; set; } // WorkEntryReasonCategory
        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Reason name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = "";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class DocumentStatusDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Code is required.")]
        [MaxLength(30)]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Display name is required.")]
        [MaxLength(150)]
        public string DisplayName { get; set; } = "";

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
