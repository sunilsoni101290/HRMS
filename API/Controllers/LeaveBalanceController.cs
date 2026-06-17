using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveBalanceController : ControllerBase
    {
        private readonly ILeaveBalanceService _leaveBalanceService;

        public LeaveBalanceController(
            ILeaveBalanceService leaveBalanceService)
        {
            _leaveBalanceService = leaveBalanceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _leaveBalanceService.GetAllAsync());
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(string employeeId)
        {
            return Ok(
                await _leaveBalanceService
                    .GetByEmployeeAsync(employeeId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _leaveBalanceService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpGet("employee-balance")]
        public async Task<IActionResult> GetBalance(string employeeId,string leaveTypeId,int year)
        {
            var data =
                await _leaveBalanceService
                    .GetEmployeeLeaveBalanceAsync(
                        employeeId,
                        leaveTypeId,
                        year);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(LeaveBalanceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(
                await _leaveBalanceService.CreateAsync(dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id,LeaveBalanceDto dto)
        {
            var data =
                await _leaveBalanceService.UpdateAsync(id, dto);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result =
                await _leaveBalanceService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Leave Balance deleted successfully."
            });
        }

        // ======================================================
        // LEAVE ALLOCATION
        // ======================================================

        [HttpPost("allocate")]
        public async Task<IActionResult> AllocateLeave(
            string employeeId,
            int year)
        {
            var result = await _leaveBalanceService
                .AllocateLeaveAsync(employeeId, year);

            return Ok(new
            {
                Success = result,
                Message = "Leave allocated successfully."
            });
        }

        // ======================================================
        // LEAVE DEDUCTION
        // ======================================================

        [HttpPost("deduct")]
        public async Task<IActionResult> DeductLeave(
            string employeeId,
            string leaveTypeId,
            decimal days)
        {
            var result = await _leaveBalanceService
                .DeductLeaveAsync(
                    employeeId,
                    leaveTypeId,
                    days);

            return Ok(new
            {
                Success = result,
                Message = $"{days} leave days deducted successfully."
            });
        }

        // ======================================================
        // LEAVE CREDIT
        // ======================================================

        [HttpPost("credit")]
        public async Task<IActionResult> CreditLeave(
            string employeeId,
            string leaveTypeId,
            decimal days)
        {
            var result = await _leaveBalanceService
                .CreditLeaveAsync(
                    employeeId,
                    leaveTypeId,
                    days);

            return Ok(new
            {
                Success = result,
                Message = $"{days} leave days credited successfully."
            });
        }

        // ======================================================
        // CARRY FORWARD
        // ======================================================

        [HttpPost("carry-forward")]
        public async Task<IActionResult> CarryForwardLeave(
            string employeeId,
            int fromYear,
            int toYear)
        {
            var result = await _leaveBalanceService
                .CarryForwardLeaveAsync(
                    employeeId,
                    fromYear,
                    toYear);

            return Ok(new
            {
                Success = result,
                Message = "Leave carry forward completed successfully."
            });
        }
    }
}
