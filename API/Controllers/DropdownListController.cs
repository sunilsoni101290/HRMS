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

        #region State Dropdown
        [HttpGet("state/{countryId}")]
        public async Task<IActionResult>GetStateDropdown(string countryId)
        {
            var data =
                await _dropdownService
                .GetStateDropdownAsync(countryId);

            return Ok(data);
        }
        #endregion

        #region City Dropdown
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
        #region Company Dropdown
        [HttpGet("branch")]
        public async Task<IActionResult>GetBranchDropdown()
        {
            var data =
                await _dropdownService
                .GetBranchDropdownAsync();

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
    }
}
