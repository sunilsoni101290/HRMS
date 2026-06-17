using Application.DTOs.Attendance;
using Application.DTOs.Attendances;
using Application.DTOs.Auth;
using Application.Interfaces.Attendances;
using Application.Services.Attendances;
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

        // ==========================================
        // GET: api/AttendanceApi
        // ==========================================
        [HttpGet("get-all-attendance-list")]
        public async Task<IActionResult> GetAllAttendanceListAsync()
        {
            var data = await _service.GetAllAttendanceListAsync();

            return Ok(data);
        }

        // ==========================================
        // POST: api/AttendanceApi
        // ==========================================
        [HttpPost("save-attendance")]
        public async Task<IActionResult> Create([FromBody] AttendanceDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Validation failed.",
                    Errors = ModelState
                });
            }

            var result = await _service.CreateAsync(dto);

            if (!result)
            {
                return BadRequest(new ApiResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "Unable to create attendance."
                });
            }

            return Ok(new ApiResponse<AttendanceDto>
            {
                Success = true,
                Message = "Attendance created successfully."
            });
        }

        // ==========================================
        // PUT: api/AttendanceApi/{id}
        // ==========================================
        [HttpPut("update-attendance/{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AttendanceDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid attendance id."
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "Validation failed."
                });
            }

            var result = await _service.UpdateAsync(dto);

            if (!result)
            {
                return NotFound(new ApiResponse<AttendanceDto>
                {
                    Success = false,
                    Message = "Attendance not found."
                });
            }

            return Ok(new ApiResponse<AttendanceDto>
            {
                Success = true,
                Message = "Attendance updated successfully."
            });
        }

        // ==========================================
        // GET DETAILS WITH LOGS
        // ==========================================
        // api/AttendanceApi/details/{id}
        // ==========================================
        [HttpGet("get-attendance-detail/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _service.GetAttendanceByIdAsync(id);

            if (data == null)
            {
                return NotFound(new
                {
                    Success = false,
                    Message = "Attendance details not found."
                });
            }

            return Ok(data);
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

        [HttpPost("break-in")]
        public async Task<IActionResult> BreakIn(PunchRequestDto dto)
        {
            var result = await _service.BreakInAsync(dto);
            return Ok(new { success = result, message = "Break In successful" });
        }

        [HttpPost("break-out")]
        public async Task<IActionResult> BreakOut(PunchRequestDto dto)
        {
            var result = await _service.BreakOutAsync(dto);
            return Ok(new { success = result, message = "Break Out successful" });
        }

        [HttpGet("current-status/{employeeId}")]
        public async Task<IActionResult> GetCurrentStatus(string employeeId)
        {
            var result =await _service.GetCurrentStatusAsync(employeeId);

            return Ok(result);
        }

        [HttpGet("attendanceLog-detail/{id}")]
        public async Task<IActionResult> GetAttendanceLogDetails(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
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
