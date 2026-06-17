using Application.DTOs.Masters;
using Application.Interfaces.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HolidayGroupController : ControllerBase
    {
        private readonly IHolidayGroupService _holidayGroupService;

        public HolidayGroupController(IHolidayGroupService holidayGroupService)
        {
            _holidayGroupService = holidayGroupService;
        }

        // ======================================================
        // HOLIDAY GROUP
        // ======================================================

        [HttpGet("groups")]
        public async Task<IActionResult> GetGroups()
        {
            var data = await _holidayGroupService.GetAllGroupsAsync();

            return Ok(data);
        }

        [HttpGet("groups/{id}")]
        public async Task<IActionResult> GetGroupById(string id)
        {
            var data = await _holidayGroupService.GetGroupByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup(HolidayGroupDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _holidayGroupService.CreateGroupAsync(dto);

            return Ok(result);
        }

        [HttpPut("groups/{id}")]
        public async Task<IActionResult> UpdateGroup(string id, HolidayGroupDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _holidayGroupService.UpdateGroupAsync(id, dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("groups/{id}")]
        public async Task<IActionResult> DeleteGroup(string id)
        {
            var result = await _holidayGroupService.DeleteGroupAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Holiday Group deleted successfully"
            });
        }

        // ======================================================
        // HOLIDAY DETAILS
        // ======================================================

        [HttpGet("details")]
        public async Task<IActionResult> GetDetails()
        {
            var data = await _holidayGroupService.GetAllDetailsAsync();

            return Ok(data);
        }

        [HttpGet("details/group/{holidayGroupId}")]
        public async Task<IActionResult> GetDetailsByGroup(string holidayGroupId)
        {
            var data = await _holidayGroupService.GetDetailsByGroupAsync(holidayGroupId);

            return Ok(data);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetDetailById(string id)
        {
            var data = await _holidayGroupService.GetDetailByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost("details")]
        public async Task<IActionResult> CreateDetail(HolidayGroupDetailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _holidayGroupService.CreateDetailAsync(dto);

            return Ok(result);
        }

        [HttpPut("details/{id}")]
        public async Task<IActionResult> UpdateDetail(string id, HolidayGroupDetailDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _holidayGroupService.UpdateDetailAsync(id, dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("details/{id}")]
        public async Task<IActionResult> DeleteDetail(string id)
        {
            var result = await _holidayGroupService.DeleteDetailAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Holiday deleted successfully"
            });
        }
    }
}
