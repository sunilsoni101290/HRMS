using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface IDropdownService
    {
        Task<List<DropdownDto>> GetCountryDropdownAsync();
        Task<List<DropdownDto>> GetStateDropdownAsync(string countryId);
        Task<List<DropdownDto>> GetCityDropdownAsync(string stateId);
        Task<List<DropdownDto>> GetCompanyDropdownAsync();
        Task<List<DropdownDto>> GetBranchDropdownAsync();
        Task<List<DropdownDto>> GetDepartmentDropdownAsync();
        Task<List<DropdownDto>> GetDesignationDropdownAsync();
        Task<List<DropdownDto>>GetEmployeeDropdownAsync();
        Task<List<DropdownDto>>GetRoleNameDropdownAsync();
        Task<List<DropdownDto>>GetReportingManagerDropdownAsync();
        Task<List<DropdownDto>> GetParentDepartmentDropdownAsync(string tenantId, string? departmentId = null);
        Task<List<DropdownDto>> GetParentDesignationDropdownAsync(string tenantId, string? designationId = null);
        Task<List<DropdownDto>> GetShiftDropdownAsync();
    }
}
