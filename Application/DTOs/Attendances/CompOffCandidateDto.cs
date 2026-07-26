using System;

namespace Application.DTOs.Attendances
{
    public class CompOffCandidateDto
    {
        public string? Id { get; set; }

        public string EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? DepartmentName { get; set; }

        public string AttendanceId { get; set; }

        public DateTime WorkedDate { get; set; }
        public decimal HoursWorked { get; set; }

        public string? TriggerReason { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public string? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedOn { get; set; }

        public string? RejectionReason { get; set; }

        public decimal CreditedDays { get; set; }

        public string? TenantId { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
