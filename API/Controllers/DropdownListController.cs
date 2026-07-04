using Application.Interfaces;
using Application.Interfaces.Company;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/dropdown")]
    [Authorize]
    public class DropdownListController : ControllerBase
    {
        private readonly IDropdownService _dropdownService;

        public DropdownListController(IDropdownService dropdownService)
        {
            _dropdownService = dropdownService;
        }

        #region Role Dropdown
        [HttpGet("role")]
        public async Task<IActionResult>GetRoleNameDropdown()
        {
            var data =
                await _dropdownService
                .GetRoleNameDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Country Dropdown
        [HttpGet("country")]
        public async Task<IActionResult>GetCountryDropdown()
        {
            var data =
                await _dropdownService
                .GetCountryDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region State Dropdown by CountryId
        [HttpGet("state/{countryId}")]
        public async Task<IActionResult>GetStateDropdown(string countryId)
        {
            var data =
                await _dropdownService
                .GetStateDropdownAsync(countryId);

            return Ok(data);
        }
        #endregion

        #region City Dropdown by StateId
        [HttpGet("city/{stateId}")]
        public async Task<IActionResult>GetCityDropdown(string stateId)
        {
            var data =
                await _dropdownService
                .GetCityDropdownAsync(stateId);

            return Ok(data);
        }
        #endregion

        #region Company Dropdown
        [HttpGet("company")]
        public async Task<IActionResult>GetCompanyDropdown()
        {
            var data =
                await _dropdownService
                .GetCompanyDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Branch Dropdown BY Company Id
        [HttpGet("branch/{companyId}")]
        public async Task<IActionResult>GetBranchDropdown(string? companyId)
        {
            var data =
                await _dropdownService
                .GetBranchDropdownAsync(companyId);

            return Ok(data);
        }
        #endregion

        #region Department Dropdown
        [HttpGet("department")]
        public async Task<IActionResult>GetDepartmentDropdown()
        {
            var data = await _dropdownService
                .GetDepartmentDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Parent Department Dropdown
        [HttpGet("parent-department")]
        public async Task<IActionResult>GetParentDepartmentDropdown(string tenantId, string? departmentId = null)
        {
            var data = await _dropdownService
                .GetParentDepartmentDropdownAsync(tenantId,departmentId);

            return Ok(data);
        }

        #endregion

        #region Designation Dropdown

        [HttpGet("designation")]
        public async Task<IActionResult>GetDesignationDropdown()
        {
            var data =
                await _dropdownService
                .GetDesignationDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Parent Designation Dropdown
        [HttpGet("parent-designation")]
        public async Task<IActionResult> GetParentDesignationDropdown(string tenantId, string? designationId = null)
        {
            var data = await _dropdownService
                .GetParentDesignationDropdownAsync(tenantId, designationId);

            return Ok(data);
        }

        #endregion

        #region Employee Dropdown
        [HttpGet("employee")]
        public async Task<IActionResult>GetEmployeeDropdown()
        {
            var data =
                await _dropdownService
                .GetEmployeeDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Reporting Manager Dropdown
        [HttpGet("reporting-manager")]
        public async Task<IActionResult>GetReportingManagerDropdown()
        {
            var data =
                await _dropdownService
                .GetReportingManagerDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Shift Dropdown
        [HttpGet("shift")]
        public async Task<IActionResult> GetShiftDropdown()
        {
            var data =
                await _dropdownService
                .GetShiftDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Default Shift Dropdown
        [HttpGet("default-shift")]
        public async Task<IActionResult> GetDefualtShiftDropdown()
        {
            var data =
                await _dropdownService
                .GetDefaultShiftDropdownAsync();

            return Ok(data);
        }
        #endregion

        #region Parent App Features Dropdown
        [HttpGet("parent-appfeature")]
        public async Task<IActionResult> GetparentDropdown()
        {
            var result = await _dropdownService.GetParentFeatureDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region App Features Dropdown
        [HttpGet("appfeature")]
        public async Task<IActionResult> GetDropdown()
        {
            var result = await _dropdownService.GetAppFeatureDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region Holiday Group Dropdown
        [HttpGet("holidaygroup")]
        public async Task<IActionResult> GetHolidayGroupDropdown()
        {
            var result = await _dropdownService.GetHolidayGroupDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region Designantion Dropdown by Department
        [HttpGet("designation/{deptId}")]
        public async Task<IActionResult> GetDesignantionByDepartmentDropdown(string deptId)
        {
            var data =
                await _dropdownService
                .GetDesignationByDeptIdDropdownAsync(deptId);

            return Ok(data);
        }
        #endregion

        #region Holiday Group Dropdown
        [HttpGet("leave-type")]
        public async Task<IActionResult> GetLeaveTypeDropdown()
        {
            var result = await _dropdownService.GetLeaveTypeDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region Asset Category Dropdown
        [HttpGet("asset-category")]
        public async Task<IActionResult> GetAssetCategoryDropdown()
        {
            var result = await _dropdownService.GetAssetCategoryDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region Available Asset Dropdown
        [HttpGet("available-asset")]
        public async Task<IActionResult> GetAvailableAssetDropdown(string? assetId = null)
        {
            var result = await _dropdownService.GetAvailableAssetDropdownAsync(assetId);

            return Ok(result);
        }
        #endregion

        #region Salary Component Dropdown
        [HttpGet("salary-component")]
        public async Task<IActionResult> GetSalaryComponentDropdown()
        {
            var result = await _dropdownService.GetSalaryComponentDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region Job Opening Dropdown
        [HttpGet("job-opening")]
        public async Task<IActionResult> GetJobOpeningDropdown()
        {
            var result = await _dropdownService.GetJobOpeningDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region Candidate Dropdown
        [HttpGet("candidate")]
        public async Task<IActionResult> GetCandidateDropdown()
        {
            var result = await _dropdownService.GetCandidateDropdownAsync();

            return Ok(result);
        }
        #endregion

        #region Candidate Application Dropdown
        [HttpGet("application")]
        public async Task<IActionResult> GetApplicationDropdown()
        {
            var result = await _dropdownService.GetApplicationDropdownAsync();

            return Ok(result);
        }
        #endregion
    }
}
