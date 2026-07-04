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
    public class CityService : ICityService
    {
        private readonly ApplicationDbContext _context;

        public CityService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<CityDto>> GetAllAsync()
        {
            try
            {
            return await _context.Cities
                .Include(x => x.State)
                .ThenInclude(x => x.Country)
                .Select(x => new CityDto
                {
                    Id = x.Id,
                    Name = x.Name,

                    StateId = x.StateId,
                    StateName = x.State.Name,

                    CountryId = x.State.CountryId,
                    CountryName = x.State.Country.Name,

                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<CityDto>();
            }
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<CityDto?> GetByIdAsync(string id)
        {
            try
            {
            return await _context.Cities
                .Include(x => x.State)
                .ThenInclude(x => x.Country)
                .Where(x => x.Id == id)
                .Select(x => new CityDto
                {
                    Id = x.Id,
                    Name = x.Name,

                    StateId = x.StateId,
                    StateName = x.State.Name,

                    CountryId = x.State.CountryId,
                    CountryName = x.State.Country.Name,

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

        public async Task<CityDto> CreateAsync(CityDto dto)
        {
            try
            {
            // Duplicate Check
            var exists = await _context.Cities.AnyAsync(x =>
                x.Name == dto.Name &&
                x.StateId == dto.StateId);

            if (exists)
                throw new Exception("City already exists");

            var entity = new City
            {
                Id = IDManager.GetNewId(new City()),
                Name = dto.Name,
                StateId = dto.StateId,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.Cities.Add(entity);

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

        public async Task<CityDto?> UpdateAsync(string id, CityDto dto)
        {
            try
            {
            var entity = await _context.Cities
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.StateId = dto.StateId;
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
            var entity = await _context.Cities
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Cities.Remove(entity);

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
