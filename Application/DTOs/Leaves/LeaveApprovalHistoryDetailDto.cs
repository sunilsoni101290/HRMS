using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Leaves
{
    
    public class LeaveApprovalHistoryDetailDto
    {
        public string? Id { get; set; }

        public string LeaveApplicationId { get; set; }

        public string? LeaveApplicationNo { get; set; }

        public string EmployeeId { get; set; }

        public string? EmployeeName { get; set; }

        public string LeaveTypeId { get; set; }

        public string? LeaveTypeName { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public decimal TotalDays { get; set; }

        public string ActionBy { get; set; }

        public string? ActionByName { get; set; }

        public ApprovalStatus Action { get; set; }

        public string? Remarks { get; set; }

        public DateTime ActionDate { get; set; }

        public DateTime CreatedOn { get; set; }

        public string? CreatedBy { get; set; }
    }
}
