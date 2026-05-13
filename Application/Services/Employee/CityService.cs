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
    public class CityService : ICityService
    {
        private readonly ApplicationDbContext _context;

        public CityService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<CityListDto>> GetAllAsync()
        {
            return await _context.Cities
                .Select(x => new CityListDto
                {
                    Id = x.Id,
                    Name = x.Name,

                    StateId = x.StateId,
                    StateName = x.State != null
                        ? x.State.Name
                        : ""
                })
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<CityDto> GetByIdAsync(string id)
        {
            var entity = await _context.Cities
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            return new CityDto
            {
                Id = entity.Id,
                Name = entity.Name,
                StateId = entity.StateId
            };
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(CityDto dto)
        {
            var entity = new City
            {
                Id = Guid.NewGuid().ToString(),

                Name = dto.Name,
                StateId = dto.StateId
            };

            await _context.Cities.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, CityDto dto)
        {
            var entity = await _context.Cities
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return "City Not Found";

            entity.Name = dto.Name;
            entity.StateId = dto.StateId;

            _context.Cities.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.Cities
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.Cities.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
