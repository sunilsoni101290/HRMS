using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.DTOs.Leaves
{
    public class LeaveTypeDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Leave Type Name is required")]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Maximum Days Per Year is required")]
        [Display(Name = "Max Days Per Year")]
        public int MaxDaysPerYear { get; set; }

        [Display(Name = "Paid Leave")]
        public bool IsPaid { get; set; } = true;

        [Display(Name = "Allow Carry Forward")]
        public bool AllowCarryForward { get; set; }

        [Display(Name = "Maximum Carry Forward Days")]
        public int? MaxCarryForwardDays { get; set; }

        [Display(Name = "Allow Half Day")]
        public bool AllowHalfDay { get; set; }

        // =====================================================
        // Leave Policy Engine (foundational phase)
        // =====================================================

        [Range(0, int.MaxValue, ErrorMessage = "Minimum Service Days Required cannot be negative.")]
        [Display(Name = "Minimum Service Days Required")]
        public int MinServiceDaysRequired { get; set; } = 0;

        [Display(Name = "Accrual Frequency")]
        public LeaveAccrualFrequency AccrualFrequency { get; set; } = LeaveAccrualFrequency.None;

        public string? AccrualFrequencyName { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Accrual Days Per Cycle cannot be negative.")]
        [Display(Name = "Accrual Days Per Cycle")]
        public decimal AccrualDaysPerCycle { get; set; } = 0;

        [Display(Name = "Is Encashable")]
        public bool IsEncashable { get; set; } = false;

        [Display(Name = "Maximum Encashable Days")]
        public int? MaxEncashableDays { get; set; }

        [Display(Name = "Applicable Gender")]
        public LeaveApplicableGender ApplicableGender { get; set; } = LeaveApplicableGender.All;

        public string? ApplicableGenderName { get; set; }

        [Display(Name = "Is Restricted Holiday Type")]
        public bool IsRestrictedHolidayType { get; set; } = false;

        public string? TenantId { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }
}
