using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/leave")]
    [Authorize]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveService _service;
        private readonly ILeaveTypeService _leaveTypeService;
        public LeaveController(ILeaveService service, ILeaveTypeService leaveTypeService)
        {
            _service = service;
            _leaveTypeService = leaveTypeService;
        }

        
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] LeaveApplyDto dto)
        {
            var result = await _service.ApplyLeaveAsync(dto);
            return Ok(result);
        }

        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] LeaveApproveDto dto)
        {
            var result = await _service.ApproveLeaveAsync(dto);
            return Ok(result);
        }

        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromBody] LeaveRejectDto dto)
        {
            var result = await _service.RejectLeaveAsync(dto);
            return Ok(result);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetEmployeeLeaves(string employeeId)
        {
            var result = await _service.GetEmployeeLeaves(employeeId);
            return Ok(result);
        }
    }
}
