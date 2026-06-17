using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class HolidayGroupController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public HolidayGroupController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }


        // =====================================================
        // HOLIDAY GROUP INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<HolidayGroupDto>>("holidaygroup/groups");

            return View(data);
        }

        // =====================================================
        // CREATE HOLIDAY GROUP - GET
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new HolidayGroupDto());
        }

        // =====================================================
        // CREATE HOLIDAY GROUP - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(HolidayGroupDto dto)
        {
            try
            {
               if(dto!=null)
                {
                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<HolidayGroupDto>("holidaygroup/groups", dto);

                    AlertHelper.Success(TempData, "Holiday Group created successfully.");
                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View(dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT HOLIDAY GROUP - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<HolidayGroupDto>(
                $"holidaygroup/groups/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            return View("Create", data);
        }

        // =====================================================
        // EDIT HOLIDAY GROUP - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HolidayGroupDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.Id) && dto != null)
                {

                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PutAsync<dynamic>($"holidaygroup/groups/{dto.Id}", dto);

                    AlertHelper.Success(TempData, "Holiday Group updated successfully.");

                    return View("Create",dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return View("Create", dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<HolidayGroupDto>(
                $"holidaygroup/groups/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            return View(data);
        }

        // =====================================================
        // DELETE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync(
                    $"holidaygroup/groups/{id}"
                );

                TempData["Success"] = "Holiday Group deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // HOLIDAY DETAILS LIST
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> HolidayList(string holidayGroupId)
        {
            ViewBag.HolidayGroupId = holidayGroupId;

            var data = await _apiService.GetAsync<List<HolidayGroupDetailDto>>(
                $"holidaygroup/details/group/{holidayGroupId}"
            );

            return View(data);
        }

        // =====================================================
        // CREATE HOLIDAY DETAIL - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> CreateHoliday(string holidayGroupId)
        {
            await LoadHolidayGroupDropdown();

            return View(new HolidayGroupDetailDto
            {
                HolidayGroupId = holidayGroupId,
                HolidayDate = DateTime.Today
            });
        }

        // =====================================================
        // CREATE HOLIDAY DETAIL - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateHoliday(HolidayGroupDetailDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.HolidayGroupId) && dto != null)
                {
                    await LoadHolidayGroupDropdown();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<HolidayGroupDetailDto>("holidaygroup/details/",dto);

                    AlertHelper.Success(TempData, "Holiday added successfully.");

                    return View(dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadHolidayGroupDropdown();

                return View(dto);
            }
            return RedirectToAction(nameof(HolidayList), new { holidayGroupId = dto.HolidayGroupId });
        }

        // =====================================================
        // EDIT HOLIDAY DETAIL - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> EditHoliday(string id)
        {
            var data = await _apiService.GetAsync<HolidayGroupDetailDto>(
                $"holidaygroup/details/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadHolidayGroupDropdown();

            return View("CreateHoliday", data);
        }

        // =====================================================
        // EDIT HOLIDAY DETAIL - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditHoliday(HolidayGroupDetailDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadHolidayGroupDropdown();


                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PutAsync<dynamic>($"holidaygroup/details/{dto.Id}", dto);

                    AlertHelper.Success(TempData, "Holiday updated successfully.");

                    return View("CreateHoliday",dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadHolidayGroupDropdown();

                return View("CreateHoliday", dto);
            }
            return RedirectToAction(nameof(HolidayList), new { holidayGroupId = dto.HolidayGroupId });
        }

        // =====================================================
        // DELETE HOLIDAY DETAIL
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteHoliday(
            string id,
            string holidayGroupId
        )
        {
            try
            {
                await _apiService.DeleteAsync(
                    $"holidaygroup/details/{id}"
                );

                TempData["Success"] = "Holiday deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(
                nameof(HolidayList),
                new { holidayGroupId }
            );
        }

        // =====================================================
        // LOAD HOLIDAY GROUP DROPDOWN
        // =====================================================
        private async Task LoadHolidayGroupDropdown()
        {
            var groups = await _apiService.GetAsync<List<DropdownDto>>(
                "dropdown/holidaygroup"
            );

            ViewBag.HolidayGroupList = groups.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();
        }
    }
}
