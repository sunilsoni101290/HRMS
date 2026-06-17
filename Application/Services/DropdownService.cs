using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
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

        public async Task<List<DropdownDto>> GetBranchDropdownAsync(string? companyId)
        {
            return await _context.Branches
                .Include(x => x.Company)
                .AsNoTracking()
                .Where(x=>x.CompanyId == companyId)
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

        public async Task<List<DropdownDto>> GetParentDepartmentDropdownAsync(string tenantId,string? departmentId = null)
        {
            var query = _context.Departments
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId);

            // Exclude current department in Edit mode
            if (!string.IsNullOrWhiteSpace(departmentId))
            {
                query = query.Where(x => x.Id != departmentId);
            }

            return await query
                .OrderBy(x => x.Name)
                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })
                .ToListAsync();
        }

        public async Task<List<DropdownDto>> GetParentDesignationDropdownAsync(string tenantId, string? designationId = null)
        {
            var query = _context.Designations
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId);

            // Exclude current department in Edit mode
            if (!string.IsNullOrWhiteSpace(designationId))
            {
                query = query.Where(x => x.Id != designationId);
            }

            return await query
                .OrderBy(x => x.Name)
                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })
                .ToListAsync();
        }

        public async Task<List<DropdownDto>> GetRoleNameDropdownAsync()
        {
            return await _context.Roles
                .AsNoTracking()

                .OrderBy(x => x.Name)

                .Select(x => new DropdownDto
                {
                    Value = x.Id,

                    Text = x.Name
                })

                .ToListAsync();
        }

        public async Task<List<DropdownDto>> GetShiftDropdownAsync()
        {
            return await _context.Shifts
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new DropdownDto
            {
                Value = x.Id,
                Text = x.Name
            })
            .ToListAsync();
        }

        public async Task<List<DropdownDto>> GetDefaultShiftDropdownAsync()
        {
            return await _context.Shifts
            .AsNoTracking()
            .Where(x => x.IsActive && x.IsDefaultShift)
            .OrderBy(x => x.Name)
            .Select(x => new DropdownDto
            {
                Value = x.Id,
                Text = x.Name
            })
            .ToListAsync();
        }
        #endregion

        #region App Features
        public async Task<List<DropdownDto>> GetAppFeatureDropdownAsync()
        {
            return await _context.AppFeatures
                .Where(x => x.IsActive)
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })
                .ToListAsync();
        }
        #endregion

        #region Parent App Features

        public async Task<List<DropdownDto>> GetParentFeatureDropdownAsync()
        {
            return await _context.AppFeatures
                .AsNoTracking()
                .Where(x => x.IsActive && x.ParentFeatureId != null)
                .Select(x => x.ParentFeature)
                .Where(x => x != null)
                .Distinct()
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })
                .ToListAsync();
        }

        public async Task<List<DropdownDto>> GetHolidayGroupDropdownAsync()
        {
            return await _context.HolidayGroups
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new DropdownDto
            {
                Value = x.Id,
                Text = x.Name
            })
            .ToListAsync();
        }
        #endregion

        #region Designation by Department Id
        public async Task<List<DropdownDto>> GetDesignationByDeptIdDropdownAsync(string? deptId)
        {
            return await _context.Designations
                .AsNoTracking()

                .Where(x => x.DepartmentId == deptId)

                .Select(x => new DropdownDto
                {
                    Value = x.Id,
                    Text = x.Name
                })

                .OrderBy(x => x.Text)

                .ToListAsync();
        }
        #endregion

        #region Leave Types
        public async Task<List<DropdownDto>> GetLeaveTypeDropdownAsync()
        {
            return await _context.LeaveTypes
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
    }
}
