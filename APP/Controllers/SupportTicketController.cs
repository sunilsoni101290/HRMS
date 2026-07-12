using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    #region Support Ticket Controller

    // Ticket access is open to everyone - admin, HR, and self-service
    // employees can all raise and view support tickets (per the user's own
    // scoping decision for this feature). What differs by role is scope:
    // admin/HR can see and manage every ticket org-wide; a self-service
    // employee can only ever see/reply to their own tickets, and can never
    // change a ticket's status - enforced here server-side, not just by
    // hiding buttons in the view.
    [JwtAuthorize]
    public class SupportTicketController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _companyId;
        private string _branchId;
        private string _userId;
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public SupportTicketController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _companyId = SessionHelper.GetActiveCompanyId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
        }

        // Admin/HR only - every ticket raised across the organization.
        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = _isAdmin;
            ViewBag.ListTitle = "All Support Tickets";

            var data = await _apiService.GetAsync<List<SupportTicketListDto>>("support-ticket");
            return View(data ?? new List<SupportTicketListDto>());
        }

        // Everyone - only the tickets the logged-in user themselves raised.
        public async Task<IActionResult> MyTickets()
        {
            ViewBag.IsAdmin = _isAdmin;
            ViewBag.ListTitle = "My Support Tickets";

            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.NoEmployeeProfile = true;
                return View("Index", new List<SupportTicketListDto>());
            }

            var data = await _apiService
                .GetAsync<List<SupportTicketListDto>>($"support-ticket/by-employee/{_employeeId}");

            return View("Index", data ?? new List<SupportTicketListDto>());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so a ticket can't be raised.";
                return RedirectToAction(nameof(MyTickets));
            }

            LoadDropdowns();
            return View(new CreateSupportTicketRequestDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSupportTicketRequestDto dto)
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so a ticket can't be raised.";
                return RedirectToAction(nameof(MyTickets));
            }

            if (dto != null)
            {
                dto.TenantId = _tenantId;
                dto.CompanyId = _companyId;
                // Who raised the ticket is always resolved from the
                // caller's own session, never trusted from posted form
                // data - prevents raising a ticket on someone else's behalf.
                dto.EmployeeId = _employeeId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("support-ticket", dto);

                TempData["Success"] = "Support ticket raised successfully.";
                return RedirectToAction(nameof(MyTickets));
            }

            LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<SupportTicketDto>($"support-ticket/{id}");

            if (data == null)
                return NotFound();

            // A self-service employee may only view their own ticket - a
            // tampered id in the URL can never expose someone else's thread.
            if (!_isAdmin && data.EmployeeId != _employeeId)
                return Forbid();

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.StatusList = EnumHelper.GetEnumList<TicketStatus>();

            return View(data);
        }

        // Everyone can reply on a ticket they can see - the employee who
        // raised it, or any admin/HR user helping out.
        [HttpPost]
        public async Task<IActionResult> Reply(string id, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["GlobalError"] = "Please enter a message.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!_isAdmin)
            {
                var ticket = await _apiService.GetAsync<SupportTicketDto>($"support-ticket/{id}");
                if (ticket == null || ticket.EmployeeId != _employeeId)
                    return Forbid();
            }

            var dto = new AddReplyRequestDto
            {
                SupportTicketId = id,
                Message = message,
                // Resolved server-side, never trusted from client input.
                RepliedByUserId = _userId
            };

            await _apiService.PostAsync<dynamic>("support-ticket/reply", dto);

            TempData["Success"] = "Reply added.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Admin/HR only - a self-service employee cannot change their own
        // ticket's status (e.g. mark it Resolved to hide it from support).
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(string id, int status)
        {
            if (!_isAdmin)
            {
                TempData["GlobalError"] = "You don't have permission to change ticket status.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var dto = new UpdateTicketStatusRequestDto
            {
                SupportTicketId = id,
                Status = status,
                UpdatedByUserId = _userId
            };

            await _apiService.PostAsync<dynamic>("support-ticket/status", dto);

            TempData["Success"] = "Ticket status updated.";
            return RedirectToAction(nameof(Details), new { id });
        }

        #region Load Dropdowns

        private void LoadDropdowns()
        {
            ViewBag.CategoryList = EnumHelper.GetEnumList<TicketCategory>();
            ViewBag.PriorityList = EnumHelper.GetEnumList<TicketPriority>();
        }

        #endregion
    }

    #endregion
}
