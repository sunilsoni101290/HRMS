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
        Task<List<DropdownDto>>GetReportingManagerDropdownAsync();
    }
}
