using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
   
    public class TenantBusinessService : ITenantBusinessService
    {
        private readonly ApplicationDbContext _context;

        public TenantBusinessService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<TenantDto>> GetAllAsync()
        {
            try
            {
            return await _context.Tenants
                .Include(x => x.Country)
                .Include(x => x.State)
                .Include(x => x.City)
                .Where(x=>x.IsActive)
                .Select(x => new TenantDto
                {
                    Id = x.Id,

                    Name = x.Name,
                    Code = x.Code,

                    Domain = x.Domain,
                    SubDomain = x.SubDomain,

                    Email = x.Email,
                    Phone = x.Phone,

                    Address = x.Address,
                    Pincode = x.Pincode,

                    CountryId = x.CountryId,
                    CountryName = x.Country.Name,

                    StateId = x.StateId,
                    StateName = x.State.Name,

                    CityId = x.CityId,
                    CityName = x.City.Name,

                    SubscriptionStartDate = x.SubscriptionStartDate,
                    SubscriptionEndDate = x.SubscriptionEndDate,

                    Logo = x.Logo,
                    WebsiteUrl = x.WebsiteUrl,

                    ConnectionString = x.ConnectionString,

                    CreatedOn = x.CreatedOn,
                    CreatedBy = x.CreatedBy,

                    ModifiedOn = x.ModifiedOn,
                    ModifiedBy = x.ModifiedBy,

                    IsActive = x.IsActive
                })
                .OrderBy(x => x.Name)
                .ToListAsync();
            }
            catch (Exception)
            {
                return new List<TenantDto>();
            }
        }

        #endregion

        #region Get By Id

        public async Task<TenantDto> GetByIdAsync(string id)
        {
            try
            {
            var tenant = await _context.Tenants
                .Include(x => x.Country)
                .Include(x => x.State)
                .Include(x => x.City)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (tenant == null)
                throw new Exception("Tenant not found.");

            return MapToDto(tenant);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Create

        public async Task<TenantDto> CreateAsync(TenantDto dto)
        {
            try
            {
            bool exists = await _context.Tenants
                .AnyAsync(x =>
                    x.Code == dto.Code);

            if (exists)
                throw new Exception("Tenant code already exists.");

            var entity = new Tenant
            {
                Id = IDManager.GetNewId(new Tenant()),
                Name = dto.Name,
                Code = dto.Code,

                Domain = dto.Domain,
                SubDomain = dto.SubDomain,

                Email = dto.Email,
                Phone = dto.Phone,

                Address = dto.Address,
                Pincode = dto.Pincode,

                CountryId = dto.CountryId,
                StateId = dto.StateId,
                CityId = dto.CityId,

                SubscriptionStartDate = dto.SubscriptionStartDate,
                SubscriptionEndDate = dto.SubscriptionEndDate,

                Logo = dto.Logo,
                WebsiteUrl = dto.WebsiteUrl,

                ConnectionString = dto.ConnectionString,

                CreatedOn = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy,

                IsActive = true
            };

            _context.Tenants.Add(entity);

            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Update

        public async Task<TenantDto> UpdateAsync(
            string id,
            TenantDto dto)
        {
            try
            {
            var entity = await _context.Tenants
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                throw new Exception("Tenant not found.");

            entity.Name = dto.Name;
            entity.Code = dto.Code;

            entity.Domain = dto.Domain;
            entity.SubDomain = dto.SubDomain;

            entity.Email = dto.Email;
            entity.Phone = dto.Phone;

            entity.Address = dto.Address;
            entity.Pincode = dto.Pincode;

            entity.CountryId = dto.CountryId;
            entity.StateId = dto.StateId;
            entity.CityId = dto.CityId;

            entity.SubscriptionStartDate =
                dto.SubscriptionStartDate;

            entity.SubscriptionEndDate =
                dto.SubscriptionEndDate;

            entity.Logo = dto.Logo;
            entity.WebsiteUrl = dto.WebsiteUrl;

            entity.ConnectionString =
                dto.ConnectionString;

            entity.ModifiedOn = DateTime.UtcNow;
            entity.ModifiedBy = dto.ModifiedBy;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(entity.Id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
            var entity = await _context.Tenants
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            entity.IsActive = false;

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Activate

        public async Task<bool> ActivateAsync(string id)
        {
            try
            {
            var entity = await _context.Tenants
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            entity.IsActive = true;
            entity.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Deactivate

        public async Task<bool> DeactivateAsync(string id)
        {
            try
            {
            var entity = await _context.Tenants
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            entity.IsActive = false;
            entity.ModifiedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Get By Code

        public async Task<TenantDto> GetByCodeAsync(string code)
        {
            try
            {
            var tenant = await _context.Tenants
                .Include(x => x.Country)
                .Include(x => x.State)
                .Include(x => x.City)
                .FirstOrDefaultAsync(x =>
                    x.Code == code);

            if (tenant == null)
                throw new Exception("Tenant not found.");

            return MapToDto(tenant);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Get By Domain

        public async Task<TenantDto> GetByDomainAsync(string domain)
        {
            try
            {
            var tenant = await _context.Tenants
                .Include(x => x.Country)
                .Include(x => x.State)
                .Include(x => x.City)
                .FirstOrDefaultAsync(x =>
                    x.Domain == domain);

            if (tenant == null)
                throw new Exception("Tenant not found.");

            return MapToDto(tenant);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Helper

        private TenantDto MapToDto(Tenant x)
        {
            return new TenantDto
            {
                Id = x.Id,

                Name = x.Name,
                Code = x.Code,

                Domain = x.Domain,
                SubDomain = x.SubDomain,

                Email = x.Email,
                Phone = x.Phone,

                Address = x.Address,
                Pincode = x.Pincode,

                CountryId = x.CountryId,
                CountryName = x.Country?.Name,

                StateId = x.StateId,
                StateName = x.State?.Name,

                CityId = x.CityId,
                CityName = x.City?.Name,

                SubscriptionStartDate = x.SubscriptionStartDate,
                SubscriptionEndDate = x.SubscriptionEndDate,

                Logo = x.Logo,
                WebsiteUrl = x.WebsiteUrl,

                ConnectionString = x.ConnectionString,

                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy,

                ModifiedOn = x.ModifiedOn,
                ModifiedBy = x.ModifiedBy,

                IsActive = x.IsActive
            };
        }

        #endregion
    }
}
