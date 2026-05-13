using Application.DTOs.Company;
using Application.Interfaces.Company;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Services.CompanyService
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;

        public CompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All
        public async Task<List<CompanyListDto>> GetAllAsync()
        {
            return await _context.Companies
                .AsNoTracking()
                .Select(x => new CompanyListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    GSTNumber = x.GSTNumber,
                    Phone = x.Phone,
                    Email = x.Email,

                    CountryName = x.Country.Name,
                    StateName = x.State.Name,
                    CityName = x.City.Name,

                    Logo = x.Logo
                })
                .ToListAsync();
        }
        #endregion

        #region Get By Id
        public async Task<CompanyDto> GetByIdAsync(string id)
        {
            var entity = await _context.Companies
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            return new CompanyDto
            {
                Id = entity.Id,

                // Basic Info
                Name = entity.Name,
                Code = entity.Code,

                // Legal Details
                GSTNumber = entity.GSTNumber,
                PANNumber = entity.PANNumber,
                CINNumber = entity.CINNumber,

                // Contact Info
                Email = entity.Email,
                Phone = entity.Phone,
                AlternatePhone = entity.AlternatePhone,

                // Address
                Address = entity.Address,
                Pincode = entity.Pincode,

                // Business
                OwnershipType = entity.OwnershipType,
                BusinessCategory = entity.BusinessCategory,

                // Multi Tenant
                TenantId = entity.TenantId,

                // Location
                CountryId = entity.CountryId,
                StateId = entity.StateId,
                CityId = entity.CityId,

                // Financial
                IncorporationDate = entity.IncorporationDate,

                // Branding
                Logo = entity.Logo,
                WebsiteUrl = entity.WebsiteUrl
            };
        }
        #endregion

        #region Create
        public async Task<string> CreateAsync(CompanyDto dto)
        {
            var entity = new Company
            {
                Id = Guid.NewGuid().ToString(),

                // Basic Info
                Name = dto.Name,
                Code = dto.Code,

                // Legal Details
                GSTNumber = dto.GSTNumber,
                PANNumber = dto.PANNumber,
                CINNumber = dto.CINNumber,

                // Contact Info
                Email = dto.Email,
                Phone = dto.Phone,
                AlternatePhone = dto.AlternatePhone,

                // Address
                Address = dto.Address,
                Pincode = dto.Pincode,

                // Business
                OwnershipType = dto.OwnershipType,
                BusinessCategory = dto.BusinessCategory,

                // Multi Tenant
                TenantId = dto.TenantId,

                // Location
                CountryId = dto.CountryId,
                StateId = dto.StateId,
                CityId = dto.CityId,

                // Financial
                IncorporationDate = dto.IncorporationDate,

                // Branding
                Logo = dto.Logo,
                WebsiteUrl = dto.WebsiteUrl
            };

            await _context.Companies.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }
        #endregion

        #region Update
        public async Task<string> UpdateAsync(string id, CompanyDto dto)
        {
            var entity = await _context.Companies
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return "Company Not Found";

            // Basic Info
            entity.Name = dto.Name;
            entity.Code = dto.Code;

            // Legal Details
            entity.GSTNumber = dto.GSTNumber;
            entity.PANNumber = dto.PANNumber;
            entity.CINNumber = dto.CINNumber;

            // Contact Info
            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.AlternatePhone = dto.AlternatePhone;

            // Address
            entity.Address = dto.Address;
            entity.Pincode = dto.Pincode;

            // Business
            entity.OwnershipType = dto.OwnershipType;
            entity.BusinessCategory = dto.BusinessCategory;

            // Multi Tenant
            entity.TenantId = dto.TenantId;

            // Location
            entity.CountryId = dto.CountryId;
            entity.StateId = dto.StateId;
            entity.CityId = dto.CityId;

            // Financial
            entity.IncorporationDate = dto.IncorporationDate;

            // Branding
            entity.Logo = dto.Logo;
            entity.WebsiteUrl = dto.WebsiteUrl;

            _context.Companies.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }
        #endregion

        #region Delete
        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Companies
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Companies.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }
        #endregion
    }
}
