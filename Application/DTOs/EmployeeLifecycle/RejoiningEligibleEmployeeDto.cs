using System;

namespace Application.DTOs.EmployeeLifecycle
{
    // Lightweight "picker" row for a FORMER employee eligible to rejoin
    // (Employee.RelievingDate != null, IsDeleted == false) - returned by
    // RejoiningService.GetEligibleForRejoinAsync, mirroring the shape of
    // ProbationDueForReviewDto (ProbationConfirmationService's own
    // "picker"/"due for review" listing DTO).
    public class RejoiningEligibleEmployeeDto
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }

        public DateTime JoiningDate { get; set; }
        public DateTime RelievingDate { get; set; }

        // Org placement as of when they exited - purely informational for
        // the picker UI; rejoining itself does not touch these fields (see
        // RejoiningService.RejoinAsync remarks - a subsequent Employee
        // Transfer is the correct feature if org placement needs to change).
        public string? DepartmentName { get; set; }
        public string? DesignationName { get; set; }
    }
}
