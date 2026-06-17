using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Leaves
{
    public class LeaveApplicationDto
    {
        public string? Id { get; set; }

        public string CompanyId { get; set; }
        public string? CompanyName { get; set; }

        public string BranchId { get; set; }
        public string? BranchName { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }

        public string LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }

        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }

        public decimal TotalDays { get; set; }

        public bool IsHalfDay { get; set; }

        public HalfDayType HalfDayType { get; set; }

        public string? Reason { get; set; }

        public ApprovalStatus Status { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public string? RejectedReason { get; set; }

        public string? DocumentUrl { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }
}
