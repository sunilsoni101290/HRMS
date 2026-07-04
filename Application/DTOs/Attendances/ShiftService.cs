using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Application.DTOs.Attendances
{
    // ShiftService.cs

    public class ShiftService : IShiftService
    {
        private readonly ApplicationDbContext _context;

        public ShiftService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All
        public async Task<List<ShiftDto>> GetAllAsync()
        {
            return await _context.Shifts
                .Include(x => x.Tenant)
                .AsNoTracking()
                .Select(x => new ShiftDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    GraceInMinutes = x.GraceInMinutes,
                    GraceOutMinutes = x.GraceOutMinutes,
                    HalfDayMinutes = x.HalfDayMinutes,
                    FullDayMinutes = x.FullDayMinutes,
                    IsNightShift = x.IsNightShift,
                    MinimumWorkingMinutes = x.MinimumWorkingMinutes,
                    MaximumWorkingMinutes = x.MaximumWorkingMinutes,
                    TenantId = x.TenantId,
                    TenantName = x.Tenant.Name,
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.ModifiedOn,
                    IsActive = x.IsActive
                })
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        #endregion

        #region Get By Id
        public async Task<ShiftDto?> GetByIdAsync(string id)
        {
            return await _context.Shifts
                .Include(x => x.Tenant)
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ShiftDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    GraceInMinutes = x.GraceInMinutes,
                    GraceOutMinutes = x.GraceOutMinutes,
                    HalfDayMinutes = x.HalfDayMinutes,
                    FullDayMinutes = x.FullDayMinutes,
                    IsNightShift = x.IsNightShift,
                    MinimumWorkingMinutes = x.MinimumWorkingMinutes,
                    MaximumWorkingMinutes = x.MaximumWorkingMinutes,
                    TenantId = x.TenantId,
                    TenantName = x.Tenant.Name,
                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.ModifiedOn,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        #endregion

        #region Create
        public async Task<ShiftDto> CreateAsync(ShiftDto dto)
        {
            // Duplicate Shift Name
            if (await _context.Shifts.AnyAsync(x =>
                x.TenantId == dto.TenantId &&
                x.Name.ToLower() == dto.Name.Trim().ToLower()))
            {
                throw new Exception("Shift Name already exists.");
            }

            // Validation
            if (dto.StartTime == dto.EndTime)
                throw new Exception("Start Time and End Time cannot be the same.");

            if (dto.MinimumWorkingMinutes > dto.MaximumWorkingMinutes)
                throw new Exception("Minimum Working Minutes cannot be greater than Maximum Working Minutes.");

            if (dto.HalfDayMinutes > dto.FullDayMinutes)
                throw new Exception("Half Day Minutes cannot be greater than Full Day Minutes.");

            if (dto.GraceInMinutes < 0 || dto.GraceOutMinutes < 0)
                throw new Exception("Grace Minutes cannot be negative.");

            var entity = new Shift
            {
                Id = IDManager.GetNewId(new Shift()),

                Name = dto.Name.Trim(),
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,

                GraceInMinutes = dto.GraceInMinutes,
                GraceOutMinutes = dto.GraceOutMinutes,

                HalfDayMinutes = dto.HalfDayMinutes,
                FullDayMinutes = dto.FullDayMinutes,

                IsNightShift = dto.IsNightShift,

                MinimumWorkingMinutes = dto.MinimumWorkingMinutes,
                MaximumWorkingMinutes = dto.MaximumWorkingMinutes,

                CreatedBy = dto.CreatedBy,

                TenantId = dto.TenantId!
            };

            await _context.Shifts.AddAsync(entity);
            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        #endregion

        #region Update
        public async Task<ShiftDto> UpdateAsync(ShiftDto dto)
        {
            var entity = await _context.Shifts
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                throw new Exception("Shift not found");

            entity.Name = dto.Name;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;

            entity.GraceInMinutes = dto.GraceInMinutes;
            entity.GraceOutMinutes = dto.GraceOutMinutes;

            entity.HalfDayMinutes = dto.HalfDayMinutes;
            entity.FullDayMinutes = dto.FullDayMinutes;

            entity.IsNightShift = dto.IsNightShift;

            entity.MinimumWorkingMinutes = dto.MinimumWorkingMinutes;
            entity.MaximumWorkingMinutes = dto.MaximumWorkingMinutes;

            await _context.SaveChangesAsync();

            return dto;
        }

        #endregion

        #region Delete
        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Shifts
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Shifts.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }
        #endregion
    }
}
