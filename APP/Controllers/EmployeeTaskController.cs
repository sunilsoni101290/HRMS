using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Employee Task Controller

    [JwtAuthorize]
    public class EmployeeTaskController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public EmployeeTaskController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<EmployeeTaskListDto>>("employee-task");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new EmployeeTaskDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeTaskDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;
                dto.AssignedBy = _userId;

                await _apiService.PostAsync<dynamic>("employee-task", dto);

                TempData["Success"] = "Task created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<EmployeeTaskDto>($"employee-task/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<EmployeeTaskDto>($"employee-task/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, EmployeeTaskDto dto)
        {
            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"employee-task/{id}", dto);

                TempData["Success"] = "Task updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeStatus(string id, string status)
        {
            await _apiService.PutAsync<dynamic>(
                $"employee-task/status/{id}?status={status}&userId={_userId}", new { });
            TempData["Success"] = $"Task marked {status}.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"employee-task/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee");
            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            ViewBag.StatusList = new List<SelectListItem>
            {
                new() { Value = "Pending", Text = "Pending" },
                new() { Value = "InProgress", Text = "In Progress" },
                new() { Value = "Completed", Text = "Completed" }
            };

            ViewBag.PriorityList = new List<SelectListItem>
            {
                new() { Value = "High", Text = "High" },
                new() { Value = "Medium", Text = "Medium" },
                new() { Value = "Low", Text = "Low" }
            };
        }

        #endregion
    }

    #endregion
}
