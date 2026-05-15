using Application.DTOs.Attendances;
using Application.Interfaces.Attendances;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Application.Services
{
    public class EmployeeShiftMappingService : IEmployeeShiftMappingService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeShiftMappingService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<EmployeeShiftMappingDto>> GetAllAsync()
        {
            return await _context.EmployeeShiftMappings
                .Include(x => x.Employee)
                .Include(x => x.Shift)
                .Select(x => new EmployeeShiftMappingDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    ShiftId = x.ShiftId,
                    ShiftName = x.Shift.Name,

                    EffectiveFrom = x.EffectiveFrom,
                    EffectiveTo = x.EffectiveTo,

                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.ModifiedOn,
                    IsActive = x.IsActive
                })
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<EmployeeShiftMappingDto?> GetByIdAsync(string id)
        {
            return await _context.EmployeeShiftMappings
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.Shift)
                .Where(x => x.Id == id)
                .Select(x => new EmployeeShiftMappingDto
                {
                    Id = x.Id,
                    EmployeeId = x.EmployeeId,
                    EmployeeName = x.Employee.FirstName + " " + x.Employee.LastName,

                    ShiftId = x.ShiftId,
                    ShiftName = x.Shift.Name,

                    EffectiveFrom = x.EffectiveFrom,
                    EffectiveTo = x.EffectiveTo,

                    CreatedOn = x.CreatedOn,
                    UpdatedOn = x.ModifiedOn,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync();
        }

        #endregion

        #region Create

        public async Task<EmployeeShiftMappingDto> CreateAsync(EmployeeShiftMappingDto dto)
        {
            var entity = new EmployeeShiftMapping
            {
                Id = IDManager.GetNewId(new EmployeeShiftMapping()),

                EmployeeId = dto.EmployeeId,
                ShiftId = dto.ShiftId,

                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,

                CreatedBy = dto.CreatedBy
            };

            await _context.EmployeeShiftMappings.AddAsync(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        #endregion

        #region Update

        public async Task<EmployeeShiftMappingDto> UpdateAsync(EmployeeShiftMappingDto dto)
        {
            var entity = await _context.EmployeeShiftMappings
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                throw new Exception("Employee shift mapping not found");

            entity.EmployeeId = dto.EmployeeId;
            entity.ShiftId = dto.ShiftId;

            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;

            entity.ModifiedBy = dto.ModifiedBy;

            entity.ModifiedOn = dto.UpdatedOn;

            await _context.SaveChangesAsync();

            return dto;
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.EmployeeShiftMappings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.EmployeeShiftMappings.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
