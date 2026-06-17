using Application.DTOs.Company;
using Application.Interfaces.Company;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Application.Services.CompanyService
{
    public class LocationService : ILocationService
    {
        private readonly ApplicationDbContext _context;

        public LocationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LocationDto>> GetAllAsync()
        {
            var data = await _context.Locations
            .AsNoTracking()
            .Join(
                _context.Branches,
                l => l.BranchId,
                b => b.Id,
                (l, b) => new { l, b }
            )
            .Join(
                _context.Companies,
                lb => lb.b.CompanyId,
                c => c.Id,
                (lb, c) => new LocationDto
                {
                    Id = lb.l.Id,
                    LocationName = lb.l.LocationName,
                    LocationCode = lb.l.LocationCode,

                    CompanyId = lb.b.CompanyId,

                    Address = lb.l.Address,
                    IsDefault = lb.l.IsDefault,

                    BranchId = lb.l.BranchId,
                    BranchName =lb.l.Branch.Name,

                    CreatedBy = lb.l.CreatedBy,
                    ModifiedOn = lb.l.ModifiedOn,
                    ModifiedBy = lb.l.ModifiedBy
                }
            )
            .ToListAsync();

            return data;
        }

        public async Task<List<LocationDto>> GetByBranchAsync(string branchId)
        {
            return await _context.Locations
                .Include(x => x.Branch)
                .ThenInclude(c => c.Company)
                .Where(x => x.BranchId == branchId)
                .Select(x => new LocationDto
                {
                    Id = x.Id,
                    LocationName = x.LocationName,
                    LocationCode = x.LocationCode,
                    
                    CompanyName = x.Branch.Company.Name,

                    BranchId = x.BranchId,
                    Address = x.Address,
                    IsDefault = x.IsDefault
                })
                .ToListAsync();
        }

        public async Task<LocationDto?> GetByIdAsync(string id)
        {
            return await _context.Locations
                .Where(x => x.Id == id)
                .Select(x => new LocationDto
                {
                    Id = x.Id,

                    LocationName = x.LocationName,
                    LocationCode = x.LocationCode,

                    CompanyId = x.Branch.CompanyId,
                    CompanyName = x.Branch.Company.Name,

                    BranchId = x.BranchId,
                    BranchName = x.Branch.Name,

                    Address = x.Address,
                    IsDefault = x.IsDefault
                })
                .FirstOrDefaultAsync();
        }

        public async Task<LocationDto> CreateAsync(LocationDto dto)
        {
            if (dto.IsDefault)
            {
                var defaults = await _context.Locations
                    .Where(x => x.BranchId == dto.BranchId && x.IsDefault)
                    .ToListAsync();

                foreach (var item in defaults)
                {
                    item.IsDefault = false;
                }
            }

            var entity = new Location
            {
                Id=IDManager.GetNewId(new Location()),
                LocationName = dto.LocationName,
                LocationCode = dto.LocationCode,

                BranchId = dto.BranchId,

                Address = dto.Address,
                IsDefault = dto.IsDefault,

                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.Locations.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<LocationDto?> UpdateAsync(string id, LocationDto dto)
        {
            var entity = await _context.Locations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.LocationName = dto.LocationName;
            entity.LocationCode = dto.LocationCode;

            entity.Address = dto.Address;
            entity.IsDefault = dto.IsDefault;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Locations
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Locations.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
