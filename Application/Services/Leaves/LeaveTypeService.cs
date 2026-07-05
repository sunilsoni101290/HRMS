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

            var entity = new LeaveType
            {
                Id = IDManager.GetNewId(new LeaveType()),
                Name = dto.Name,
                MaxDaysPerYear = dto.MaxDaysPerYear,
                IsPaid = dto.IsPaid,
                AllowCarryForward = dto.AllowCarryForward,
                MaxCarryForwardDays = dto.MaxCarryForwardDays,
                AllowHalfDay = dto.AllowHalfDay,
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

            entity.Name = dto.Name;
            entity.MaxDaysPerYear = dto.MaxDaysPerYear;
            entity.IsPaid = dto.IsPaid;
            entity.AllowCarryForward = dto.AllowCarryForward;
            entity.MaxCarryForwardDays = dto.MaxCarryForwardDays;
            entity.AllowHalfDay = dto.AllowHalfDay;

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
    }
}
