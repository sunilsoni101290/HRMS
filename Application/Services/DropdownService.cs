using Application.DTOs;
using Application.Interfaces;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services
{
    public class DropdownService : IDropdownService
    {
        private readonly ApplicationDbContext _context;

        public DropdownService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Country
        public async Task<List<DropdownDto>>GetCountryDropdownAsync()
        {
            return await _context.Countries
                .AsNoTracking()

                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })

                .OrderBy(x => x.Text)

                .ToListAsync();
        }
        #endregion

        #region State
        public async Task<List<DropdownDto>>GetStateDropdownAsync(string countryId)
        {
            return await _context.States
                .AsNoTracking()

                .Where(x => x.CountryId == countryId)

                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })

                .OrderBy(x => x.Text)

                .ToListAsync();
        }
        #endregion

        #region City
        public async Task<List<DropdownDto>>GetCityDropdownAsync(string stateId)
        {
            return await _context.Cities
                .AsNoTracking()

                .Where(x => x.StateId == stateId)

                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })

                .OrderBy(x => x.Text)

                .ToListAsync();
        }
        #endregion

        #region Company
        public async Task<List<DropdownDto>>GetCompanyDropdownAsync()
        {
            return await _context.Companies
                .AsNoTracking()

                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })

                .OrderBy(x => x.Text)

                .ToListAsync();
        }
        #endregion

        #region Department
        public async Task<List<DropdownDto>>GetDepartmentDropdownAsync()
        {
            return await _context.Departments
                .AsNoTracking()

                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })

                .OrderBy(x => x.Text)

                .ToListAsync();
        }
        #endregion

        #region Designation
        public async Task<List<DropdownDto>>GetDesignationDropdownAsync()
        {
            return await _context.Designations
                .AsNoTracking()

                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })

                .OrderBy(x => x.Text)

                .ToListAsync();
        }
        #endregion

        #region Employee

        public async Task<List<DropdownDto>>
            GetEmployeeDropdownAsync()
        {
            return await _context.Employees
                .AsNoTracking()

                .OrderBy(x => x.FirstName)

                .Select(x => new DropdownDto
                {
                    Value = x.Id,

                    Text =
                        x.EmployeeCode
                        + " - "
                        + x.FirstName
                        + " "
                        + x.LastName
                })

                .ToListAsync();
        }

        #endregion

        #region Reporting Manager

        public async Task<List<DropdownDto>>GetReportingManagerDropdownAsync()
        {
            return await _context.Employees
                .Include(x => x.ReportingManager)
                .AsNoTracking()
                .OrderBy(x => x.ReportingManager.FirstName)

                .Select(x => new DropdownDto
                {
                    Value = x.ReportingManager.Id,

                    Text = x.ReportingManager.EmployeeCode
                        + " - "
                        + x.ReportingManager.FirstName
                        + " "
                        + x.ReportingManager.LastName
                })
                .ToListAsync();
        }

        public async Task<List<DropdownDto>> GetBranchDropdownAsync()
        {
            return await _context.Branches
                .Include(x => x.Company)
                .AsNoTracking()

                .OrderBy(x => x.Name)

                .Select(x => new DropdownDto
                {
                    Value = x.Id,

                    Text =
                        x.Company.Name
                        + " - "
                        + x.Name
                })

                .ToListAsync();
        }

        #endregion
    }
}
