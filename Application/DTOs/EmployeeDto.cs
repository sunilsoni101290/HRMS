using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs
{
    public class EmployeeDto
    {
        public string? Id { get; set; }   // Used only for update

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string Mobile { get; set; }

        [Required]
        public string DepartmentId { get; set; }

        [Required]
        public string DesignationId { get; set; }

        [Required]
        public DateTime DateOfJoining { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? SalaryStructureId { get; set; }

        public string? ManagerId { get; set; }

        public bool HasLoginAccess { get; set; } = false;

        public bool? IsActive { get; set; }   // 🔥 nullable (important)
    }
}
