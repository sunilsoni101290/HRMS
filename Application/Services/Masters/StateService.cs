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
    public class StateService : IStateService
    {
        private readonly ApplicationDbContext _context;

        public StateService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<StateDto>> GetAllAsync()
        {
            try
            {
            return await _context.States
                .Include(x => x.Country)
                .Select(x => new StateDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    CountryId = x.CountryId,
                    CountryName = x.Country.Name,
                    GSTStateCode = x.GSTStateCode,
                    TenantId = x.TenantId,
                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<StateDto>();
            }
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<StateDto?> GetByIdAsync(string id)
        {
            try
            {
            return await _context.States
                .Include(x => x.Country)
                .Where(x => x.Id == id)
                .Select(x => new StateDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    CountryId = x.CountryId,
                    CountryName = x.Country.Name,
                    GSTStateCode = x.GSTStateCode,
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

        public async Task<StateDto> CreateAsync(StateDto dto)
        {
            try
            {
            // Duplicate Check
            var exists = await _context.States.AnyAsync(x =>
                x.Name == dto.Name &&
                x.CountryId == dto.CountryId);

            if (exists)
                throw new Exception("State already exists");

            var entity = new State
            {
                Id = IDManager.GetNewId(new State()),
                Name = dto.Name,
                Code = dto.Code,
                CountryId = dto.CountryId,
                GSTStateCode = dto.GSTStateCode,
                TenantId = dto.TenantId,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.States.Add(entity);

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

        public async Task<StateDto?> UpdateAsync(string id, StateDto dto)
        {
            try
            {
            var entity = await _context.States
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.CountryId = dto.CountryId;
            entity.GSTStateCode = dto.GSTStateCode;
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
            var entity = await _context.States
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.States.Remove(entity);

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
