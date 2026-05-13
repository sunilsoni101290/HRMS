using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class EmployeeController : Controller
    {
        private readonly IApiService _apiService;
        private  string _tenantId;
        private  string _userId;

        public EmployeeController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<EmployeeListDto>>($"Employee/employee-list");

            return View(data);
        }

        #endregion

        #region Create GET

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new EmployeeDto());
        }

        #endregion

        #region Create POST

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
               
                dto.TenantId = _tenantId;

                await _apiService
                    .PostAsync<dynamic>($"Employee/add-employee", dto);

                TempData["Success"] = "Employee created successfully.";

                /*
                    TempData["Warning"] = "Please verify details.";
                    TempData["Info"] = "New update available.";
                    TempData["Error"] = "Something went wrong.";
                 */

                return View(dto);
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit GET

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<EmployeeDto>($"Employee/get-employee-detail/{id}");

            if (data == null)
                return NotFound();

            await LoadDropdowns();

            return View("Create", data);
        }

        #endregion

        #region Edit POST

        [HttpPost]
        public async Task<IActionResult> Edit(EmployeeDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.TenantId = _tenantId;

                await LoadDropdowns();
                await _apiService
                .PutAsync<dynamic>($"Employee/update-employee", dto);

                TempData["Success"] = "Employee updated successfully.";

                return View("Create", dto);
            }

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<EmployeeDto>($"Employee/get-employee-detail/{id}");

            if (data == null)
                return NotFound();

            return View(data);
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService
                .DeleteAsync($"Employee/employee/{id}");

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            // Company
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/department");

            ViewBag.DepartmentList = new SelectList(
                departments,
                "Value",
                "Text");

            // Designation
            var designations = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/designation");

            ViewBag.DesignationList = new SelectList(
                designations,
                "Value",
                "Text");

            // Employees (Reporting Manager)
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/employee");

            ViewBag.ReportingManagerList = new SelectList(
                employees,
                "Value",
                "Text");
        }

        #endregion
    }
}
