using Application.DTOs.Company;
using Application.Interfaces.Company;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Infrastructure.Data;

namespace Application.Services.CompanyService
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;

        public CompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CompanyDto>> GetAllAsync()
        {
            return await _context.Companies
                .Include(x => x.Country)
                .Include(x => x.State)
                .Include(x => x.City)
                .Select(x => new CompanyDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    GSTNumber = x.GSTNumber,
                    PANNumber = x.PANNumber,
                    CINNumber = x.CINNumber,
                    Email = x.Email,
                    Phone = x.Phone,
                    AlternatePhone = x.AlternatePhone,
                    Address = x.Address,
                    Pincode = x.Pincode,
                    OwnershipType = x.OwnershipType,
                    BusinessCategory = x.BusinessCategory,

                    TenantId = x.TenantId,

                    CountryId = x.CountryId,
                    CountryName = x.Country.Name,

                    StateId = x.StateId,
                    StateName = x.State.Name,

                    CityId = x.CityId,
                    CityName = x.City.Name,

                    IncorporationDate = x.IncorporationDate,
                    Logo = x.Logo,
                    WebsiteUrl = x.WebsiteUrl,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        public async Task<CompanyDto?> GetByIdAsync(string id)
        {
            return await _context.Companies
                .Where(x => x.Id == id)
                .Select(x => new CompanyDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    GSTNumber = x.GSTNumber,
                    PANNumber = x.PANNumber,
                    CINNumber = x.CINNumber,
                    Email = x.Email,
                    Phone = x.Phone,
                    AlternatePhone = x.AlternatePhone,
                    Address = x.Address,
                    Pincode = x.Pincode,
                    OwnershipType = x.OwnershipType,
                    BusinessCategory = x.BusinessCategory,
                    TenantId = x.TenantId,

                    CountryId = x.CountryId,
                    StateId = x.StateId,
                    CityId = x.CityId,

                    IncorporationDate = x.IncorporationDate,
                    Logo = x.Logo,
                    WebsiteUrl = x.WebsiteUrl,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CompanyDto> CreateAsync(CompanyDto dto)
        {
            var entity = new Company
            {
                Id=IDManager.GetNewId(new Company()),
                Name = dto.Name,
                Code = dto.Code,
                GSTNumber = dto.GSTNumber,
                PANNumber = dto.PANNumber,
                CINNumber = dto.CINNumber,
                Email = dto.Email,
                Phone = dto.Phone,
                AlternatePhone = dto.AlternatePhone,
                Address = dto.Address,
                Pincode = dto.Pincode,
                OwnershipType = dto.OwnershipType,
                BusinessCategory = dto.BusinessCategory,
                TenantId = dto.TenantId,

                CountryId = dto.CountryId,
                StateId = dto.StateId,
                CityId = dto.CityId,

                IncorporationDate = dto.IncorporationDate,
                Logo = dto.Logo,
                WebsiteUrl = dto.WebsiteUrl,

                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.Companies.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<CompanyDto?> UpdateAsync(string id, CompanyDto dto)
        {
            var entity = await _context.Companies
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.GSTNumber = dto.GSTNumber;
            entity.PANNumber = dto.PANNumber;
            entity.CINNumber = dto.CINNumber;
            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.AlternatePhone = dto.AlternatePhone;
            entity.Address = dto.Address;
            entity.Pincode = dto.Pincode;
            entity.OwnershipType = dto.OwnershipType;
            entity.BusinessCategory = dto.BusinessCategory;

            entity.CountryId = dto.CountryId;
            entity.StateId = dto.StateId;
            entity.CityId = dto.CityId;

            entity.IncorporationDate = dto.IncorporationDate;
            entity.Logo = dto.Logo;
            entity.WebsiteUrl = dto.WebsiteUrl;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
        }

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
    }
}
