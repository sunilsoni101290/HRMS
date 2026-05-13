using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Leaves
{
    #region Apply Leave DTO
    public class LeaveApplyDto
    {
        public string EmployeeId { get; set; }
        public string LeaveTypeId { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public bool IsHalfDay { get; set; }
        public HalfDayType? HalfDayType { get; set; }

        public string? Reason { get; set; }
    }
    #endregion

    #region Approve DTO
    public class LeaveApproveDto
    {
        public string LeaveId { get; set; }
        public string ApproverId { get; set; }
    }
    #endregion

    #region Reject DTO
    public class LeaveRejectDto
    {
        public string LeaveId { get; set; }
        public string ApproverId { get; set; }
        public string Reason { get; set; }
    }
    #endregion

    public class LeaveCancelDto
    {
        public string LeaveId { get; set; }
        public string EmployeeId { get; set; }
    }

    #region Response DTO
    public class LeaveResponseDto
    {
        public string Id { get; set; }
        public string EmployeeName { get; set; }
        public string LeaveType { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalDays { get; set; }
        public string Status { get; set; }
    }
    #endregion
}
