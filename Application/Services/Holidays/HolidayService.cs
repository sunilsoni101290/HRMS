using Application.DTOs.Holidays;
using Application.Interfaces.Holidays;
using Domain.Entities;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.Holidays
{
    public class HolidayService : IHolidayService
    {
        private readonly ApplicationDbContext _db;

        public HolidayService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ================= HOLIDAY =================

        public async Task<List<HolidayDto>> GetHolidays(string tenantId, int year)
        {
            return await _db.Holidays
                .Where(x => x.TenantId == tenantId && x.Date.Year == year)
                .Select(x => new HolidayDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Date = x.Date,
                    TenantId = x.TenantId
                })
                .ToListAsync();
        }

        public async Task<HolidayDto> CreateHoliday(CreateHolidayDto dto)
        {
            var exists = await _db.Holidays.AnyAsync(x =>
                x.TenantId == dto.TenantId &&
                x.Date.Date == dto.Date.Date);

            if (exists)
                throw new Exception("Holiday already exists");

            var entity = new Holiday
            {
                Name = dto.Name,
                Date = dto.Date.Date,
                TenantId = dto.TenantId
            };

            _db.Holidays.Add(entity);
            await _db.SaveChangesAsync();

            return new HolidayDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Date = entity.Date,
                TenantId = entity.TenantId
            };
        }

        public async Task<bool> DeleteHoliday(string id)
        {
            var entity = await _db.Holidays.FindAsync(id);
            if (entity == null) return false;

            _db.Holidays.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        // ================= WEEKOFF =================

        public async Task<List<WeekOffDto>> GetWeekOffs(string tenantId)
        {
            return await _db.WeekOffs
                .Where(x => x.TenantId == tenantId)
                .Select(x => new WeekOffDto
                {
                    Id = x.Id,
                    Day = x.Day,
                    TenantId = x.TenantId
                })
                .ToListAsync();
        }

        public async Task<WeekOffDto> AddWeekOff(CreateWeekOffDto dto)
        {
            var exists = await _db.WeekOffs.AnyAsync(x =>
                x.TenantId == dto.TenantId &&
                x.Day == dto.Day);

            if (exists)
                throw new Exception("WeekOff already exists");

            var entity = new WeekOff
            {
                Day = dto.Day,
                TenantId = dto.TenantId
            };

            _db.WeekOffs.Add(entity);
            await _db.SaveChangesAsync();

            return new WeekOffDto
            {
                Id = entity.Id,
                Day = entity.Day,
                TenantId = entity.TenantId
            };
        }

        public async Task<bool> RemoveWeekOff(string id)
        {
            var entity = await _db.WeekOffs.FindAsync(id);
            if (entity == null) return false;

            _db.WeekOffs.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        // ================= VALIDATION =================

        public async Task<bool> IsHoliday(DateTime date, string tenantId)
        {
            return await _db.Holidays.AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Date.Date == date.Date);
        }

        public async Task<bool> IsWeekOff(DateTime date, string tenantId)
        {
            return await _db.WeekOffs.AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Day == date.DayOfWeek);
        }
    }
}
