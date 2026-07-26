using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Leaves
{
    public class LeaveTypeService : ILeaveTypeService
    {
        private readonly ApplicationDbContext _context;

        public LeaveTypeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LeaveTypeDto>> GetAllAsync()
        {
            try
            {
            return await _context.LeaveTypes
                .Select(x => new LeaveTypeDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    MaxDaysPerYear = x.MaxDaysPerYear,
                    IsPaid = x.IsPaid,
                    AllowCarryForward = x.AllowCarryForward,
                    MaxCarryForwardDays = x.MaxCarryForwardDays,
                    AllowHalfDay = x.AllowHalfDay,

                    MinServiceDaysRequired = x.MinServiceDaysRequired,
                    AccrualFrequency = x.AccrualFrequency,
                    AccrualFrequencyName = x.AccrualFrequency.ToString(),
                    AccrualDaysPerCycle = x.AccrualDaysPerCycle,
                    IsEncashable = x.IsEncashable,
                    MaxEncashableDays = x.MaxEncashableDays,
                    ApplicableGender = x.ApplicableGender,
                    ApplicableGenderName = x.ApplicableGender.ToString(),
                    IsRestrictedHolidayType = x.IsRestrictedHolidayType,

                    TenantId = x.TenantId,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<LeaveTypeDto>();
            }
        }

        public async Task<LeaveTypeDto?> GetByIdAsync(string id)
        {
            try
            {
            return await _context.LeaveTypes
                .Where(x => x.Id == id)
                .Select(x => new LeaveTypeDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    MaxDaysPerYear = x.MaxDaysPerYear,
                    IsPaid = x.IsPaid,
                    AllowCarryForward = x.AllowCarryForward,
                    MaxCarryForwardDays = x.MaxCarryForwardDays,
                    AllowHalfDay = x.AllowHalfDay,

                    MinServiceDaysRequired = x.MinServiceDaysRequired,
                    AccrualFrequency = x.AccrualFrequency,
                    AccrualFrequencyName = x.AccrualFrequency.ToString(),
                    AccrualDaysPerCycle = x.AccrualDaysPerCycle,
                    IsEncashable = x.IsEncashable,
                    MaxEncashableDays = x.MaxEncashableDays,
                    ApplicableGender = x.ApplicableGender,
                    ApplicableGenderName = x.ApplicableGender.ToString(),
                    IsRestrictedHolidayType = x.IsRestrictedHolidayType,

                    TenantId = x.TenantId,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LeaveTypeDto> CreateAsync(LeaveTypeDto dto)
        {
            try
            {
            var exists = await _context.LeaveTypes
                .AnyAsync(x => x.Name == dto.Name);

            if (exists)
                throw new Exception("Leave Type already exists.");

            ValidatePolicyFields(dto);

            var entity = new LeaveType
            {
                Id = IDManager.GetNewId(new LeaveType()),
                Name = dto.Name,
                MaxDaysPerYear = dto.MaxDaysPerYear,
                IsPaid = dto.IsPaid,
                AllowCarryForward = dto.AllowCarryForward,
                MaxCarryForwardDays = dto.MaxCarryForwardDays,
                AllowHalfDay = dto.AllowHalfDay,

                MinServiceDaysRequired = dto.MinServiceDaysRequired,
                AccrualFrequency = dto.AccrualFrequency,
                AccrualDaysPerCycle = dto.AccrualDaysPerCycle,
                IsEncashable = dto.IsEncashable,
                MaxEncashableDays = dto.MaxEncashableDays,
                ApplicableGender = dto.ApplicableGender,
                IsRestrictedHolidayType = dto.IsRestrictedHolidayType,

                TenantId=dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.LeaveTypes.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<LeaveTypeDto?> UpdateAsync(string id, LeaveTypeDto dto)
        {
            try
            {
            var entity = await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            ValidatePolicyFields(dto);

            entity.Name = dto.Name;
            entity.MaxDaysPerYear = dto.MaxDaysPerYear;
            entity.IsPaid = dto.IsPaid;
            entity.AllowCarryForward = dto.AllowCarryForward;
            entity.MaxCarryForwardDays = dto.MaxCarryForwardDays;
            entity.AllowHalfDay = dto.AllowHalfDay;

            entity.MinServiceDaysRequired = dto.MinServiceDaysRequired;
            entity.AccrualFrequency = dto.AccrualFrequency;
            entity.AccrualDaysPerCycle = dto.AccrualDaysPerCycle;
            entity.IsEncashable = dto.IsEncashable;
            entity.MaxEncashableDays = dto.MaxEncashableDays;
            entity.ApplicableGender = dto.ApplicableGender;
            entity.IsRestrictedHolidayType = dto.IsRestrictedHolidayType;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.LeaveTypes
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.LeaveTypes.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================
        // Leave Policy Engine field validation - basic guardrails only,
        // deliberately not enforcing anything the pre-existing LeaveType
        // fields didn't already enforce.
        // =====================================================
        private static void ValidatePolicyFields(LeaveTypeDto dto)
        {
            if (dto.MinServiceDaysRequired < 0)
                throw new Exception("Minimum Service Days Required cannot be negative.");

            if (dto.AccrualDaysPerCycle < 0)
                throw new Exception("Accrual Days Per Cycle cannot be negative.");

            if (dto.IsEncashable && (!dto.MaxEncashableDays.HasValue || dto.MaxEncashableDays.Value <= 0))
                throw new Exception("Maximum Encashable Days must be set and greater than zero when Is Encashable is enabled.");
        }
    }
}
