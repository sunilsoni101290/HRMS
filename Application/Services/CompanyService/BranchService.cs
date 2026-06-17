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
    public class BranchService : IBranchService
    {
        private readonly ApplicationDbContext _context;

        public BranchService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BranchDto>> GetAllAsync()
        {
            return await _context.Branches
                .Include(x => x.Company)
                .Include(x => x.Country)
                .Include(x => x.State)
                .Include(x => x.City)
                .Select(x => new BranchDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company.Name,

                    TenantId = x.TenantId,

                    Email = x.Email,
                    Phone = x.Phone,
                    AlternatePhone = x.AlternatePhone,

                    Address = x.Address,
                    Pincode = x.Pincode,

                    CountryId = x.CountryId,
                    CountryName = x.Country.Name,

                    StateId = x.StateId,
                    StateName = x.State.Name,

                    CityId = x.CityId,
                    CityName = x.City.Name,

                    GSTNumber = x.GSTNumber,
                    CINNo = x.CINNo,

                    IsHeadOffice = x.IsHeadOffice,

                    CreatedBy = x.CreatedBy,
                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy
                })
                .ToListAsync();
        }

        public async Task<List<BranchDto>> GetByCompanyAsync(string companyId)
        {
            return await _context.Branches
                .Where(x => x.CompanyId == companyId)
                .Select(x => new BranchDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    CompanyId = x.CompanyId,
                    Phone = x.Phone,
                    Address = x.Address,
                    IsHeadOffice = x.IsHeadOffice
                })
                .ToListAsync();
        }

        public async Task<BranchDto?> GetByIdAsync(string id)
        {
            var branch = await _context.Branches
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new BranchDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,

                    CompanyId = x.CompanyId,
                    CompanyName = x.Company.Name,

                    TenantId = x.TenantId,
                    TenantName = x.Tenant.Name,

                    Email = x.Email,
                    Phone = x.Phone,
                    AlternatePhone = x.AlternatePhone,

                    Address = x.Address,
                    Pincode = x.Pincode,

                    CountryId = x.CountryId,
                    CountryName = x.Country.Name,

                    StateId = x.StateId,
                    StateName = x.State.Name,

                    CityId = x.CityId,
                    CityName = x.City.Name,

                    GSTNumber = x.GSTNumber,
                    CINNo = x.CINNo,

                    IsHeadOffice = x.IsHeadOffice
                })
                .FirstOrDefaultAsync();

            if (branch != null)
            {
                branch.Locations = await _context.Locations
                    .AsNoTracking()
                    .Where(l => l.BranchId == branch.Id)
                    .Select(l => new LocationDto
                    {
                        Id = l.Id,
                        LocationName = l.LocationName,
                        LocationCode = l.LocationCode,

                        BranchId = l.BranchId,

                        Address = l.Address,

                        IsDefault = l.IsDefault,

                        CreatedBy = l.CreatedBy,
                        ModifiedBy = l.ModifiedBy,
                        ModifiedOn = l.ModifiedOn
                    })
                    .ToListAsync();
            }

            return branch;
        }

        public async Task<BranchDto> CreateAsync(BranchDto dto)
        {
            if (dto.IsHeadOffice)
            {
                var headOffice = await _context.Branches
                    .Where(x => x.CompanyId == dto.CompanyId && x.IsHeadOffice)
                    .ToListAsync();

                foreach (var item in headOffice)
                {
                    item.IsHeadOffice = false;
                }
            }

            var entity = new Branch
            {
                Id=IDManager.GetNewId(new Branch()),
                Name = dto.Name,
                Code = dto.Code,

                CompanyId = dto.CompanyId,
                TenantId = dto.TenantId,

                Email = dto.Email,
                Phone = dto.Phone,
                AlternatePhone = dto.AlternatePhone,

                Address = dto.Address,
                Pincode = dto.Pincode,

                CountryId = dto.CountryId,
                StateId = dto.StateId,
                CityId = dto.CityId,

                GSTNumber = dto.GSTNumber,
                CINNo = dto.CINNo,

                IsHeadOffice = dto.IsHeadOffice,

                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow
            };

            _context.Branches.Add(entity);

            await _context.SaveChangesAsync();

            dto.Id = entity.Id;

            return dto;
        }

        public async Task<BranchDto?> UpdateAsync(string id, BranchDto dto)
        {
            var entity = await _context.Branches
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            entity.Name = dto.Name;
            entity.Code = dto.Code;

            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.AlternatePhone = dto.AlternatePhone;

            entity.Address = dto.Address;
            entity.Pincode = dto.Pincode;

            entity.CountryId = dto.CountryId;
            entity.StateId = dto.StateId;
            entity.CityId = dto.CityId;

            entity.GSTNumber = dto.GSTNumber;
            entity.CINNo = dto.CINNo;

            entity.IsHeadOffice = dto.IsHeadOffice;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return dto;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Branches
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Branches.Remove(entity);

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
