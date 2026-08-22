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
        Task<List<DropdownDto>> GetBranchDropdownAsync(string? companyId);
        /// <summary>All branches across every company for the tenant - used by screens with no Company selection to cascade from (e.g. the Biometric Agent master).</summary>
        Task<List<DropdownDto>> GetAllBranchesDropdownAsync(string? tenantId);
        Task<List<DropdownDto>> GetDepartmentDropdownAsync();
        Task<List<DropdownDto>> GetDesignationDropdownAsync();
        Task<List<DropdownDto>> GetDesignationByDeptIdDropdownAsync(string?deptId);
        Task<List<DropdownDto>>GetEmployeeDropdownAsync();
        Task<List<DropdownDto>>GetRoleNameDropdownAsync();
        Task<List<DropdownDto>>GetReportingManagerDropdownAsync();
        Task<List<DropdownDto>> GetParentDepartmentDropdownAsync(string tenantId, string? departmentId = null);
        Task<List<DropdownDto>> GetParentDesignationDropdownAsync(string tenantId, string? designationId = null);
        Task<List<DropdownDto>> GetShiftDropdownAsync();
        Task<List<DropdownDto>> GetDefaultShiftDropdownAsync();
        Task<List<DropdownDto>> GetAppFeatureDropdownAsync();
        Task<List<DropdownDto>> GetParentFeatureDropdownAsync();
        Task<List<DropdownDto>> GetHolidayGroupDropdownAsync();
        Task<List<DropdownDto>> GetLeaveTypeDropdownAsync();
        Task<List<DropdownDto>> GetAssetCategoryDropdownAsync();
        Task<List<DropdownDto>> GetAvailableAssetDropdownAsync(string? assetId = null);
        Task<List<DropdownDto>> GetSalaryComponentDropdownAsync();
        Task<List<DropdownDto>> GetJobOpeningDropdownAsync();
        Task<List<DropdownDto>> GetCandidateDropdownAsync();
        Task<List<DropdownDto>> GetApplicationDropdownAsync();
    }
}
