using Application.DTOs.Masters;
using Application.Interfaces.Masters;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Masters
{
    public class WeekOffService : IWeekOffService
    {
        private readonly ApplicationDbContext _context;

        public WeekOffService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<WeekOffDto>> GetAllAsync()
        {
            return await _context.WeekOffs
                .Select(x => new WeekOffDto
                {
                    Id = x.Id,
                    Day = x.Day,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<WeekOffDto?> GetByIdAsync(string id)
        {
            return await _context.WeekOffs
                .Where(x => x.Id == id)
                .Select(x => new WeekOffDto
                {
                    Id = x.Id,
                    Day = x.Day,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .FirstOrDefaultAsync();
        }

        // ======================================================
        // CREATE
        // ======================================================

        public async Task<WeekOffDto> CreateAsync(WeekOffDto dto)
        {
            // Duplicate Check
            var exists = await _context.WeekOffs
                .AnyAsync(x => x.Day == dto.Day);

            if (exists)
                throw new Exception("Week Off already exists");

            var entity = new WeekOff
            {
                Id = IDManager.GetNewId(new WeekOff()),
                Day = dto.Day,

                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.WeekOffs.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        // ======================================================
        // UPDATE
        // ======================================================

        public async Task<WeekOffDto?> UpdateAsync(string id, WeekOffDto dto)
        {
            var entity = await _context.WeekOffs
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            // Duplicate Check
            var exists = await _context.WeekOffs
                .AnyAsync(x => x.Day == dto.Day && x.Id != id);

            if (exists)
                throw new Exception("Week Off already exists");

            entity.Day = dto.Day;

            entity.TenantId = dto.TenantId;
            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
        }

        // ======================================================
        // DELETE
        // ======================================================

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.WeekOffs
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.WeekOffs.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        // ======================================================
        // GET WEEK OFFS
        // ======================================================

        public async Task<List<WeekOffDto>> GetWeekOffs(string tenantId)
        {
            return await _context.WeekOffs
                .Where(x => x.TenantId == tenantId)
                .Select(x => new WeekOffDto
                {
                    Id = x.Id,
                    Day = x.Day,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .OrderBy(x => x.Day)
                .ToListAsync();
        }

        // ======================================================
        // ADD WEEK OFF
        // ======================================================

        public async Task<WeekOffDto> AddWeekOff(WeekOffDto dto)
        {
            // Duplicate Check
            var exists = await _context.WeekOffs
                .AnyAsync(x =>
                    x.Day == dto.Day &&
                    x.TenantId == dto.TenantId);

            if (exists)
                throw new Exception("Week Off already exists");

            var entity = new WeekOff
            {
                Day = dto.Day,

                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.WeekOffs.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        // ======================================================
        // REMOVE WEEK OFF
        // ======================================================

        public async Task<bool> RemoveWeekOff(string id)
        {
            var entity = await _context.WeekOffs
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.WeekOffs.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        // ======================================================
        // CHECK HOLIDAY
        // ======================================================

        public async Task<bool> IsHoliday(DateTime date, string tenantId)
        {
            return await _context.HolidayGroupDetails
                .AnyAsync(x =>
                    x.HolidayDate.Date == date.Date &&
                    x.TenantId == tenantId);
        }

        // ======================================================
        // CHECK WEEK OFF
        // ======================================================

        public async Task<bool> IsWeekOff(DateTime date, string tenantId)
        {
            var day = date.DayOfWeek;

            return await _context.WeekOffs
                .AnyAsync(x =>
                    x.Day == day &&
                    x.TenantId == tenantId);
        }
    }
}
