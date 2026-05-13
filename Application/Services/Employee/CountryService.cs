using Application.DTOs.Employee;
using Application.Interfaces.Employee;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Employee
{
    public class CountryService : ICountryService
    {
        private readonly ApplicationDbContext _context;

        public CountryService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<CountryListDto>> GetAllAsync()
        {
            return await _context.Countries
                .Select(x => new CountryListDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    PhoneCode = x.PhoneCode
                })
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<CountryDto> GetByIdAsync(string id)
        {
            var entity = await _context.Countries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            return new CountryDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                PhoneCode = entity.PhoneCode
            };
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(CountryDto dto)
        {
            var entity = new Country
            {
                Id = Guid.NewGuid().ToString(),

                Name = dto.Name,
                Code = dto.Code,
                PhoneCode = dto.PhoneCode
            };

            await _context.Countries.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, CountryDto dto)
        {
            var entity = await _context.Countries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return "Country Not Found";

            entity.Name = dto.Name;
            entity.Code = dto.Code;
            entity.PhoneCode = dto.PhoneCode;

            _context.Countries.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Delete

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

        #endregion
    }
}
