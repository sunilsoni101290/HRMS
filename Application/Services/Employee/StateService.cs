using Application.DTOs.Employee;
using Application.Interfaces.Employee;
using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Application.Services.Employee
{
    public class StateService : IStateService
    {
        private readonly ApplicationDbContext _context;

        public StateService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Get All

        public async Task<List<StateListDto>> GetAllAsync()
        {
            return await _context.States
                .Select(x => new StateListDto
                {
                    Id = x.Id,
                    Name = x.Name,

                    CountryId = x.CountryId,
                    CountryName = x.Country != null
                        ? x.Country.Name
                        : ""
                })
                .ToListAsync();
        }

        #endregion

        #region Get By Id

        public async Task<StateDto> GetByIdAsync(string id)
        {
            var entity = await _context.States
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return null;

            return new StateDto
            {
                Id = entity.Id,
                Name = entity.Name,
                CountryId = entity.CountryId
            };
        }

        #endregion

        #region Create

        public async Task<string> CreateAsync(StateDto dto)
        {
            var entity = new State
            {
                Id = IDManager.GetNewId(new State()),
                GSTStateCode=dto.GSTStateCode,
                Name = dto.Name,
                CountryId = dto.CountryId
            };

            await _context.States.AddAsync(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Update

        public async Task<string> UpdateAsync(string id, StateDto dto)
        {
            var entity = await _context.States
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return "State Not Found";

            entity.Name = dto.Name;
            entity.CountryId = dto.CountryId;

            _context.States.Update(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id)
        {
            var entity = await _context.States
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            _context.States.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion
    }
}
