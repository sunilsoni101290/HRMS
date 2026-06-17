using Application.DTOs.Masters;
using Application.Interfaces.Masters;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Application.Services.Masters
{
    public class CountryService : ICountryService
    {
        private readonly ApplicationDbContext _context;

        public CountryService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        public async Task<List<CountryDto>> GetAllAsync()
        {
            return await _context.Countries
                .Select(x => new CountryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    PhoneCode = x.PhoneCode,
                    TenantId = x.TenantId
                })
                .ToListAsync();
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        public async Task<CountryDto?> GetByIdAsync(string id)
        {
            return await _context.Countries
                .Where(x => x.Id == id)
                .Select(x => new CountryDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    PhoneCode = x.PhoneCode,
                    TenantId = x.TenantId
                })
                .FirstOrDefaultAsync();
        }

        // ======================================================
        // CREATE
        // ======================================================

        public async Task<CountryDto> CreateAsync(CountryDto dto)
        {
            var entity = new Country
            {
                Id=IDManager.GetNewId(new Country()),
                Name = dto.Name,
                Code = dto.Code,
                PhoneCode = dto.PhoneCode,
                TenantId = dto.TenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = "System"
            };

            _context.Countries.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        // ======================================================
        // UPDATE
        // ======================================================

        public async Task<CountryDto?> UpdateAsync(string id, CountryDto dto)
        {
            var entity = await _context.Countries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.PhoneCode = dto.PhoneCode;
            entity.TenantId = dto.TenantId;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = "System";

            await _context.SaveChangesAsync();

            return dto;
        }

        // ======================================================
        // DELETE
        // ======================================================

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Countries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Countries.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
