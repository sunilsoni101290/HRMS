using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Attendances
{
    public class EmployeeBiometricMappingDto
    {
        public string? Id { get; set; }

        public string EmployeeId { get; set; }

        /// <summary>Convenience field for list screens - not persisted.</summary>
        public string? EmployeeName { get; set; }

        public string BiometricEmployeeCode { get; set; }

        public string? CardNumber { get; set; }

        public bool IsActive { get; set; }
    }
}
