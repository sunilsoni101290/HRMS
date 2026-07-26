using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Domain.Entities
{
    public class LeaveType : BaseEntity
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public int MaxDaysPerYear { get; set; }

        public bool IsPaid { get; set; } = true;

        public bool AllowCarryForward { get; set; }
        public int? MaxCarryForwardDays { get; set; }

        public bool AllowHalfDay { get; set; }

        // =====================================================
        // LEAVE POLICY ENGINE (foundational phase) - additive only,
        // all default to the pre-existing manual/backward-compatible
        // behavior so no already-shipped LeaveType row changes behavior
        // unless HR explicitly opts in via the LeaveType CRUD screen.
        // =====================================================

        // Employee must have (today - JoiningDate).Days >= this to be
        // eligible for this leave type at all. 0 = no minimum service
        // requirement (default, matches existing behavior).
        public int MinServiceDaysRequired { get; set; } = 0;

        // None (default) = balance is NOT auto-accrued - fully manual via
        // LeaveBalanceService, exactly like every LeaveType today. Only
        // LeaveTypes explicitly configured with a non-None frequency are
        // picked up by LeaveAccrualService's background job.
        public LeaveAccrualFrequency AccrualFrequency { get; set; } = LeaveAccrualFrequency.None;

        // Days credited each time the accrual cycle fires (e.g. 1.5 days/
        // month for a type with MaxDaysPerYear=18 and AccrualFrequency=Monthly).
        public decimal AccrualDaysPerCycle { get; set; } = 0;

        public bool IsEncashable { get; set; } = false;

        public int? MaxEncashableDays { get; set; }

        public LeaveApplicableGender ApplicableGender { get; set; } = LeaveApplicableGender.All;

        // Flags this LeaveType as the special "pick from the optional/
        // restricted holiday calendar" type. Validation logic keyed off
        // this flag lives in LeaveApplicationService (later phase) - this
        // flag only needs to exist and be persisted here.
        public bool IsRestrictedHolidayType { get; set; } = false;

        public override string GetSequencePrefix() => "LT";
    }
}
