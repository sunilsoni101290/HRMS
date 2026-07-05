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
            try
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
            catch (Exception)
            {
                return new List<WeekOffDto>();
            }
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<WeekOffDto?> GetByIdAsync(string id)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        // ======================================================
        // CREATE
        // ======================================================

        public async Task<WeekOffDto> CreateAsync(WeekOffDto dto)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        // ======================================================
        // UPDATE
        // ======================================================

        public async Task<WeekOffDto?> UpdateAsync(string id, WeekOffDto dto)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        // ======================================================
        // DELETE
        // ======================================================

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.WeekOffs
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.WeekOffs.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ======================================================
        // GET WEEK OFFS
        // ======================================================

        public async Task<List<WeekOffDto>> GetWeekOffs(string tenantId)
        {
            try
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
            catch (Exception)
            {
                return new List<WeekOffDto>();
            }
        }

        // ======================================================
        // ADD WEEK OFF
        // ======================================================

        public async Task<WeekOffDto> AddWeekOff(WeekOffDto dto)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }

        // ======================================================
        // REMOVE WEEK OFF
        // ======================================================

        public async Task<bool> RemoveWeekOff(string id)
        {
            try
            {
            var entity = await _context.WeekOffs
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.WeekOffs.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ======================================================
        // CHECK HOLIDAY
        // ======================================================

        public async Task<bool> IsHoliday(DateTime date, string tenantId)
        {
            try
            {
            return await _context.HolidayGroupDetails
                .AnyAsync(x =>
                    x.HolidayDate.Date == date.Date &&
                    x.TenantId == tenantId);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ======================================================
        // CHECK WEEK OFF
        // ======================================================

        public async Task<bool> IsWeekOff(DateTime date, string tenantId)
        {
            try
            {
            var day = date.DayOfWeek;

            return await _context.WeekOffs
                .AnyAsync(x =>
                    x.Day == day &&
                    x.TenantId == tenantId);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
