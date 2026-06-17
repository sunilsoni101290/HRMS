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
    public class HolidayGroupService : IHolidayGroupService
    {
        private readonly ApplicationDbContext _context;

        public HolidayGroupService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // HOLIDAY GROUP
        // ======================================================

        public async Task<List<HolidayGroupDto>> GetAllGroupsAsync()
        {
            return await _context.HolidayGroups
                .Select(x => new HolidayGroupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        public async Task<HolidayGroupDto?> GetGroupByIdAsync(string id)
        {
            return await _context.HolidayGroups
                .Where(x => x.Id == id)
                .Select(x => new HolidayGroupDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .FirstOrDefaultAsync();
        }

        public async Task<HolidayGroupDto> CreateGroupAsync(HolidayGroupDto dto)
        {
            var exists = await _context.HolidayGroups
                .AnyAsync(x => x.Name == dto.Name);

            if (exists)
                throw new Exception("Holiday Group already exists");

            var entity = new HolidayGroup
            {
                Id = IDManager.GetNewId(new HolidayGroup()),
                Name = dto.Name,
                Description = dto.Description,

                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.HolidayGroups.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<HolidayGroupDto?> UpdateGroupAsync(string id, HolidayGroupDto dto)
        {
            var entity = await _context.HolidayGroups
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.Description = dto.Description;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task<bool> DeleteGroupAsync(string id)
        {
            var entity = await _context.HolidayGroups
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.HolidayGroups.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        // ======================================================
        // HOLIDAY GROUP DETAILS
        // ======================================================

        public async Task<List<HolidayGroupDetailDto>> GetAllDetailsAsync()
        {
            return await _context.HolidayGroupDetails
                .Include(x => x.HolidayGroup)
                .Select(x => new HolidayGroupDetailDto
                {
                    Id = x.Id,

                    HolidayGroupId = x.HolidayGroupId,
                    HolidayGroupName = x.HolidayGroup.Name,

                    HolidayDate = x.HolidayDate,
                    HolidayName = x.HolidayName,
                    Remarks = x.Remarks,
                    IsOptional = x.IsOptional,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();
        }

        public async Task<List<HolidayGroupDetailDto>> GetDetailsByGroupAsync(string holidayGroupId)
        {
            return await _context.HolidayGroupDetails
                .Include(x => x.HolidayGroup)
                .Where(x => x.HolidayGroupId == holidayGroupId)
                .Select(x => new HolidayGroupDetailDto
                {
                    Id = x.Id,

                    HolidayGroupId = x.HolidayGroupId,
                    HolidayGroupName = x.HolidayGroup.Name,

                    HolidayDate = x.HolidayDate,
                    HolidayName = x.HolidayName,
                    Remarks = x.Remarks,
                    IsOptional = x.IsOptional,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();
        }

        public async Task<HolidayGroupDetailDto?> GetDetailByIdAsync(string id)
        {
            return await _context.HolidayGroupDetails
                .Include(x => x.HolidayGroup)
                .Where(x => x.Id == id)
                .Select(x => new HolidayGroupDetailDto
                {
                    Id = x.Id,

                    HolidayGroupId = x.HolidayGroupId,
                    HolidayGroupName = x.HolidayGroup.Name,

                    HolidayDate = x.HolidayDate,
                    HolidayName = x.HolidayName,
                    Remarks = x.Remarks,
                    IsOptional = x.IsOptional,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .FirstOrDefaultAsync();
        }

        public async Task<HolidayGroupDetailDto> CreateDetailAsync(HolidayGroupDetailDto dto)
        {
            var exists = await _context.HolidayGroupDetails
                .AnyAsync(x =>
                    x.HolidayGroupId == dto.HolidayGroupId &&
                    x.HolidayDate.Date == dto.HolidayDate.Date);

            if (exists)
                throw new Exception("Holiday already exists for this date");

            var entity = new HolidayGroupDetail
            {
                Id = IDManager.GetNewId(new HolidayGroupDetail()),
                HolidayGroupId = dto.HolidayGroupId,

                HolidayDate = dto.HolidayDate,
                HolidayName = dto.HolidayName,
                Remarks = dto.Remarks,
                IsOptional = dto.IsOptional,

                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.HolidayGroupDetails.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<HolidayGroupDetailDto?> UpdateDetailAsync(string id, HolidayGroupDetailDto dto)
        {
            var entity = await _context.HolidayGroupDetails
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.HolidayGroupId = dto.HolidayGroupId;

            entity.HolidayDate = dto.HolidayDate;
            entity.HolidayName = dto.HolidayName;
            entity.Remarks = dto.Remarks;
            entity.IsOptional = dto.IsOptional;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task<bool> DeleteDetailAsync(string id)
        {
            var entity = await _context.HolidayGroupDetails
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.HolidayGroupDetails.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
