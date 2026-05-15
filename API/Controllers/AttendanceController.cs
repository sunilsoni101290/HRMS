using Application.DTOs.Attendance;
using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/attendance")]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;

        public AttendanceController(IAttendanceService service)
        {
            _service = service;
        }

        [HttpGet("get-all-attendance-logs")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }



        [HttpPost("punch-in")]
        public async Task<IActionResult> PunchIn(PunchRequestDto dto)
        {
            var result = await _service.PunchInAsync(dto);
            return Ok(new { success = result, message = "Punch In successful" });
        }

        [HttpPost("punch-out")]
        public async Task<IActionResult> PunchOut(PunchRequestDto dto)
        {
            var result = await _service.PunchOutAsync(dto);
            return Ok(new { success = result, message = "Punch Out successful" });
        }

        [HttpGet("status/{employeeId}")]
        public async Task<IActionResult> GetStatus(string employeeId)
        {
            var result = await _service.GetLiveStatus(employeeId);
            return Ok(result);
        }

        [HttpGet("monthly")]
        public async Task<IActionResult> Monthly(string employeeId, int month, int year)
        {
            return Ok(await _service.GetMonthlyAsync(employeeId, month, year));
        }
    }
}
