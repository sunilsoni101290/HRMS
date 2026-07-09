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
        private string _companyId;
        private readonly string? _employeeId;

        // Only Admin/HR can create, edit, delete, or reassign tasks. A plain
        // employee may only change the status/remarks of a task already
        // assigned to them - enforced here server-side, not just by hiding
        // buttons in the view.
        private readonly bool _isAdmin;

        public EmployeeTaskController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _companyId = SessionHelper.GetActiveCompanyId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = _isAdmin;
            var data = await _apiService.GetAsync<List<EmployeeTaskListDto>>("employee-task");
            return View(data);
        }

        /// <summary>
        /// Self-service "my tasks" list - reuses the Index view but filters
        /// server-side down to tasks assigned to the logged-in user's own
        /// employee record, so an employee can never browse tasks assigned
        /// to other employees from this page.
        /// </summary>
        public async Task<IActionResult> MyTasks()
        {
            ViewBag.IsAdmin = false;

            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.NoEmployeeProfile = true;
                return View("Index", new List<EmployeeTaskListDto>());
            }

            var data = await _apiService.GetAsync<List<EmployeeTaskListDto>>("employee-task");

            var mine = (data ?? new List<EmployeeTaskListDto>())
                .Where(t => t.EmployeeId == _employeeId)
                .ToList();

            ViewBag.ListTitle = "My Tasks";
            return View("Index", mine);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // Assigning tasks is an Admin/HR function only - a plain employee
            // can update the status/remarks of a task already assigned to
            // them, but cannot create new ones for themselves or anyone else.
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to create tasks.";
                return RedirectToAction(nameof(MyTasks));
            }

            await LoadDropdowns();
            return View(new EmployeeTaskDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeTaskDto dto)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to create tasks.";
                return RedirectToAction(nameof(MyTasks));
            }

            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CompanyId = _companyId;
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

            // An employee may only view details of their own task.
            if (!_isAdmin && (data == null || data.EmployeeId != _employeeId))
                return Forbid();

            ViewBag.IsAdmin = _isAdmin;
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit tasks.";
                return RedirectToAction(nameof(MyTasks));
            }

            var data = await _apiService.GetAsync<EmployeeTaskDto>($"employee-task/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, EmployeeTaskDto dto)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to edit tasks.";
                return RedirectToAction(nameof(MyTasks));
            }

            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CompanyId = _companyId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"employee-task/{id}", dto);

                TempData["Success"] = "Task updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        /// <summary>
        /// The one action a self-service employee is allowed on their own
        /// task: change its status and attach a remarks note. Admin/HR can
        /// also use this for any task. For non-admins the task is fetched
        /// first and its EmployeeId is checked against the caller's own
        /// session EmployeeId - a tampered id in the request can never
        /// update someone else's task.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangeStatus(string id, string status, string? remarks = null)
        {
            if (!_isAdmin)
            {
                if (string.IsNullOrEmpty(_employeeId))
                {
                    TempData["GlobalError"] = "Your login isn't linked to an employee profile.";
                    return RedirectToAction(nameof(MyTasks));
                }

                var task = await _apiService.GetAsync<EmployeeTaskDto>($"employee-task/{id}");
                if (task == null || task.EmployeeId != _employeeId)
                {
                    TempData["GlobalError"] = "You can only update your own tasks.";
                    return RedirectToAction(nameof(MyTasks));
                }
            }

            var query = $"employee-task/status/{id}?status={Uri.EscapeDataString(status)}&userId={_userId}";
            if (!string.IsNullOrWhiteSpace(remarks))
                query += $"&remarks={Uri.EscapeDataString(remarks)}";

            await _apiService.PutAsync<dynamic>(query, new { });
            TempData["Success"] = $"Task marked {status}.";

            return _isAdmin ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(MyTasks));
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to delete tasks.";
                return RedirectToAction(nameof(MyTasks));
            }

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
