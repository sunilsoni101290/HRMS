using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class BiometricDevice : BaseEntity
    {
        [Required]
        public string TenantId { get; set; }

        public virtual Tenant Tenant { get; set; }

        [Required]
        public string CompanyId { get; set; }

        public virtual Company Company { get; set; }

        public string? BranchId { get; set; }

        public virtual Branch Branch { get; set; }

        [Required]
        [MaxLength(100)]
        public string DeviceName { get; set; }

        [Required]
        [MaxLength(50)]
        public string DeviceCode { get; set; }

        [Required]
        public string IPAddress { get; set; }

        public int Port { get; set; }

        public string? ApiUrl { get; set; }

        public string? Username { get; set; }

        public string? Password { get; set; }

        public string? SerialNumber { get; set; }
        public DateTime? LastSyncDate { get; set; }

        /// <summary>
        /// Shared secret used by the on-site BiometricAgent (running on a machine
        /// at the client's location) to authenticate punch pushes to
        /// POST /api/BiometricSync/ingest. Not the same as Username/Password,
        /// which are for the device's own admin login.
        /// </summary>
        [MaxLength(100)]
        public string? DeviceKey { get; set; }

        public override string GetSequencePrefix()
            => "BDV";
    }
    public class BiometricAttendanceLog : BaseEntity
    {
        public string EmployeeCode { get; set; }

        public DateTime PunchTime { get; set; }

        public PunchType PunchType { get; set; }

        public string DeviceId { get; set; }

        public bool IsDuplicate { get; set; }

        public bool IsProcessed { get; set; }

        public DateTime? ProcessedOn { get; set; }

        public override string GetSequencePrefix()
        => "BAL";
    }

    public class EmployeeBiometricMapping : BaseEntity
    {
        [Required]
        public string EmployeeId { get; set; }

        public virtual Employee Employee { get; set; }

        [Required]
        public string BiometricEmployeeCode { get; set; }

        public string? CardNumber { get; set; }

        public string? FaceId { get; set; }

        public string? FingerTemplateId { get; set; }

        public override string GetSequencePrefix()
            => "EBM";
    }
}
