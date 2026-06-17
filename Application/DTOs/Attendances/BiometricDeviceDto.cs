using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Attendances
{
    public class BiometricDeviceDto
    {
        public string? Id { get; set; }

        public string TenantId { get; set; }

        public string CompanyId { get; set; }

        public string? BranchId { get; set; }

        public string DeviceName { get; set; }

        public string DeviceCode { get; set; }

        public string IPAddress { get; set; }

        public int Port { get; set; }

        public string? ApiUrl { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool IsActive { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
    }
}
