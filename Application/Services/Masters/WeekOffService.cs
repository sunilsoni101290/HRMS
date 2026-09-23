using Application.DTOs.Masters;
using Application.Interfaces.Masters;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Domain.Enums.EnumExtensions;

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
        // MAPPING (shared by every read path below)
        // ======================================================

        private static WeekOffDto ToDto(WeekOff x) => new WeekOffDto
        {
            Id = x.Id,
            Day = x.Day,
            PatternType = x.PatternType,
            Occurrences = x.PatternType == WeekOffPatternType.NthWeekdayOfMonth
                ? (x.Occurrences ?? new List<WeekOffOccurrence>())
                    .Select(o => o.OccurrenceNumber)
                    .OrderBy(n => n)
                    .ToList()
                : new List<int>(),

            TenantId = x.TenantId,
            CreatedBy = x.CreatedBy,
            ModifiedOn = x.ModifiedOn,
            ModifiedBy = x.ModifiedBy
        };

        // Distinct, valid (1-5) occurrence numbers from a submitted DTO.
        private static List<int> NormalizeOccurrences(List<int>? occurrences)
        {
            return (occurrences ?? new List<int>())
                .Where(n => n >= 1 && n <= 5)
                .Distinct()
                .OrderBy(n => n)
                .ToList();
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<WeekOffDto>> GetAllAsync()
        {
            try
            {
                var entities = await _context.WeekOffs
                    .Include(x => x.Occurrences)
                    .ToListAsync();

                return entities.Select(ToDto).ToList();
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
                var entity = await _context.WeekOffs
                    .Include(x => x.Occurrences)
                    .FirstOrDefaultAsync(x => x.Id == id);

                return entity == null ? null : ToDto(entity);
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
                // Duplicate Check - scoped by PatternType too (additive):
                // "every Saturday" and "2nd/4th Saturday" are different
                // configurations and must be allowed to coexist for the
                // same weekday.
                var exists = await _context.WeekOffs
                    .AnyAsync(x => x.Day == dto.Day && x.PatternType == dto.PatternType);

                if (exists)
                    throw new Exception("Week Off already exists");

                var entity = new WeekOff
                {
                    Id = IDManager.GetNewId(new WeekOff()),
                    Day = dto.Day,
                    PatternType = dto.PatternType,

                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };

                if (dto.PatternType == WeekOffPatternType.NthWeekdayOfMonth)
                {
                    entity.Occurrences = NormalizeOccurrences(dto.Occurrences)
                        .Select(n => new WeekOffOccurrence
                        {
                            Id = IDManager.GetNewId(new WeekOffOccurrence()),
                            OccurrenceNumber = n,
                            TenantId = dto.TenantId,
                            CreatedBy = dto.CreatedBy,
                            CreatedOn = DateTime.UtcNow
                        })
                        .ToList();
                }

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
                    .Include(x => x.Occurrences)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return null;

                // Duplicate Check
                var exists = await _context.WeekOffs
                    .AnyAsync(x => x.Day == dto.Day && x.PatternType == dto.PatternType && x.Id != id);

                if (exists)
                    throw new Exception("Week Off already exists");

                entity.Day = dto.Day;
                entity.PatternType = dto.PatternType;

                entity.TenantId = dto.TenantId;
                entity.ModifiedOn = DateTime.UtcNow;
                entity.ModifiedBy = dto.ModifiedBy;

                // Replace the occurrence child rows with the submitted set.
                if (entity.Occurrences != null && entity.Occurrences.Count > 0)
                    _context.RemoveRange(entity.Occurrences);

                entity.Occurrences = dto.PatternType == WeekOffPatternType.NthWeekdayOfMonth
                    ? NormalizeOccurrences(dto.Occurrences)
                        .Select(n => new WeekOffOccurrence
                        {
                            Id = IDManager.GetNewId(new WeekOffOccurrence()),
                            WeekOffId = entity.Id,
                            OccurrenceNumber = n,
                            TenantId = dto.TenantId,
                            CreatedBy = dto.ModifiedBy ?? dto.CreatedBy,
                            CreatedOn = DateTime.UtcNow
                        })
                        .ToList()
                    : new List<WeekOffOccurrence>();

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
                    .Include(x => x.Occurrences)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return false;

                if (entity.Occurrences != null && entity.Occurrences.Count > 0)
                    _context.RemoveRange(entity.Occurrences);

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
                var entities = await _context.WeekOffs
                    .Include(x => x.Occurrences)
                    .Where(x => x.TenantId == tenantId)
                    .OrderBy(x => x.Day)
                    .ToListAsync();

                return entities.Select(ToDto).ToList();
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
                        x.PatternType == dto.PatternType &&
                        x.TenantId == dto.TenantId);

                if (exists)
                    throw new Exception("Week Off already exists");

                var entity = new WeekOff
                {
                    Id = IDManager.GetNewId(new WeekOff()),
                    Day = dto.Day,
                    PatternType = dto.PatternType,

                    TenantId = dto.TenantId,
                    CreatedBy = dto.CreatedBy,
                    CreatedOn = DateTime.UtcNow
                };

                if (dto.PatternType == WeekOffPatternType.NthWeekdayOfMonth)
                {
                    entity.Occurrences = NormalizeOccurrences(dto.Occurrences)
                        .Select(n => new WeekOffOccurrence
                        {
                            Id = IDManager.GetNewId(new WeekOffOccurrence()),
                            OccurrenceNumber = n,
                            TenantId = dto.TenantId,
                            CreatedBy = dto.CreatedBy,
                            CreatedOn = DateTime.UtcNow
                        })
                        .ToList();
                }

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
                    .Include(x => x.Occurrences)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity == null)
                    return false;

                if (entity.Occurrences != null && entity.Occurrences.Count > 0)
                    _context.RemoveRange(entity.Occurrences);

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
        // CHECK WEEK OFF (now pattern-aware - both EveryWeek and
        // NthWeekdayOfMonth configs for the tenant are consulted)
        // ======================================================

        public async Task<bool> IsWeekOff(DateTime date, string tenantId)
        {
            try
            {
                var configs = await _context.WeekOffs
                    .Include(x => x.Occurrences)
                    .Where(x => x.TenantId == tenantId)
                    .ToListAsync();

                return WeekOffCalculator.IsWeekOffDate(date, configs);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ======================================================
        // GET WEEK OFF DATES FOR MONTH (new - the computational core)
        // ======================================================

        public async Task<List<DateTime>> GetWeekOffDatesForMonth(int year, int month, string tenantId)
        {
            try
            {
                var configs = await _context.WeekOffs
                    .Include(x => x.Occurrences)
                    .Where(x => x.TenantId == tenantId)
                    .ToListAsync();

                return WeekOffCalculator.GetWeekOffDatesForMonth(year, month, configs)
                    .OrderBy(d => d)
                    .ToList();
            }
            catch (Exception)
            {
                return new List<DateTime>();
            }
        }
    }
}
