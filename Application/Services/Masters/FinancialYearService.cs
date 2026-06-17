using Application.Common.Exceptions;
using Application.DTOs.Masters;
using Application.Interfaces.Masters;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.Masters
{
    public class FinancialYearService : IFinancialYearService
    {
        private readonly ApplicationDbContext _context;

        public FinancialYearService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<FinancialYearDto>> GetAllAsync()
        {
            return await _context.FinancialYears
                .Include(x => x.Company)
                .Select(x => new FinancialYearDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status,

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company.Name,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy,

                    IsCurrent = x.IsCurrent
                })
                .OrderByDescending(x => x.StartDate)
                .ToListAsync();
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<FinancialYearDto?> GetByIdAsync(string id)
        {
            return await _context.FinancialYears
                .Include(x => x.Company)
                .Where(x => x.Id == id)
                .Select(x => new FinancialYearDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status,

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company.Name,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy,

                    IsCurrent = x.IsCurrent
                })
                .FirstOrDefaultAsync();
        }

        // ======================================================
        // CREATE
        // ======================================================

        public async Task<FinancialYearDto> CreateAsync(FinancialYearDto dto)
        {
            // Duplicate Check
            var exists = await _context.FinancialYears.AnyAsync(x =>
                x.Name == dto.Name &&
                x.CompanyId == dto.CompanyId);

            if (exists)
                throw new Exception("Financial Year already exists");

            // Date Validation
            if (dto.EndDate <= dto.StartDate)
                throw new Exception("End Date must be greater than Start Date");

            // Only One Current FY
            if (dto.IsCurrent)
            {
                var currentYears = await _context.FinancialYears
                    .Where(x => x.CompanyId == dto.CompanyId && x.IsCurrent)
                    .ToListAsync();

                foreach (var fy in currentYears)
                {
                    fy.IsCurrent = false;
                }
            }

            var entity = new FinancialYear
            {
                Id = IDManager.GetNewId(new FinancialYear()),
                Name = dto.Name,
                Code = dto.Code,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = dto.Status,

                CompanyId = dto.CompanyId,

                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow,

                IsCurrent = dto.IsCurrent
            };

            _context.FinancialYears.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        // ======================================================
        // UPDATE
        // ======================================================

        public async Task<FinancialYearDto?> UpdateAsync(string id, FinancialYearDto dto)
        {
            var entity = await _context.FinancialYears
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            // Date Validation
            if (dto.EndDate <= dto.StartDate)
                throw new Exception("End Date must be greater than Start Date");

            // Only One Current FY
            if (dto.IsCurrent)
            {
                var currentYears = await _context.FinancialYears
                    .Where(x => x.CompanyId == dto.CompanyId &&
                                x.IsCurrent &&
                                x.Id != id)
                    .ToListAsync();

                foreach (var fy in currentYears)
                {
                    fy.IsCurrent = false;
                }
            }

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.StartDate = dto.StartDate;
            entity.EndDate = dto.EndDate;
            entity.Status = dto.Status;

            entity.CompanyId = dto.CompanyId;

            entity.IsCurrent = dto.IsCurrent;

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
            var entity = await _context.FinancialYears
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.FinancialYears.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }

        // ======================================================
        // GET CURRENT FINANCIAL YEAR
        // ======================================================

        public async Task<FinancialYearDto?> GetCurrentFinancialYearAsync()
        {
            return await _context.FinancialYears
                .Include(x => x.Company)
                .Where(x => x.IsCurrent)
                .Select(x => new FinancialYearDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    Status = x.Status,

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company.Name,

                    TenantId = x.TenantId,

                    IsCurrent = x.IsCurrent
                })
                .FirstOrDefaultAsync();
        }
    }
}
