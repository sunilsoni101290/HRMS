using Application.Common.Responses;
using Application.DTOs.Leaves;
using Application.Interfaces.Leaves;
using Application.Services.Leaves;
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

        #region Leave Balance CRUD

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(
                await _leaveBalanceService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data =
                await _leaveBalanceService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<IActionResult> GetByEmployee(
            string employeeId)
        {
            return Ok(
                await _leaveBalanceService
                    .GetByEmployeeAsync(employeeId));
        }

        [HttpGet("employee-balance")]
        public async Task<IActionResult> GetBalance(string employeeId,string leaveTypeId,int year)
        {
            var data =
                await _leaveBalanceService.GetEmployeeLeaveBalanceAsync(
                        employeeId,
                        leaveTypeId,
                        year);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            LeaveBalanceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(
                await _leaveBalanceService.CreateAsync(dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            string id,
            LeaveBalanceDto dto)
        {
            var data =
                await _leaveBalanceService
                    .UpdateAsync(id, dto);

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
                Message = "Leave balance deleted successfully."
            });
        }

        #endregion

        #region Leave Operations

        [HttpPost("allocate")]
        public async Task<IActionResult> AllocateLeave(AllocateLeaveRequestDto model)
        {
            var result =
                await _leaveBalanceService.AllocateLeaveAsync(model);

            return Ok(new
            {
                Success = result,
                Message = "Leave allocated successfully."
            });
        }

        [HttpPost("credit")]
        public async Task<IActionResult> CreditLeave(LeaveAdjustmentRequestDto request)
        {
            var result = await _leaveBalanceService.CreditLeaveAsync(request);

            return Ok(new
            {
                Success = result,
                Message = "Leave credited successfully."
            });
        }

        [HttpPost("deduct")]
        public async Task<IActionResult> DeductLeave(LeaveAdjustmentRequestDto request)
        {
            var result = await _leaveBalanceService.DeductLeaveAsync(request);

            return Ok(new
            {
                Success = result,
                Message = "Leave deducted successfully."
            });
        }

        [HttpPost("carry-forward")]
        public async Task<IActionResult> CarryForward(CarryForwardLeaveRequestDto request)
        {
            var result = await _leaveBalanceService.CarryForwardLeaveAsync(request);

            return Ok(new
            {
                Success = result,
                Message = "Leave carry forward completed."
            });
        }

        #endregion

        #region Transactions

        [HttpPost("transactions")]
        public async Task<IActionResult> GetTransactions(LeaveTransactionFilterRequestDto request)
        {
            return Ok(await _leaveBalanceService.GetTransactionsAsync(request));
        }

        [HttpPost("transactions/employee")]
        public async Task<IActionResult>GetEmployeeTransactions(EmployeeTransactionRequestDto request)
        {
            return Ok(await _leaveBalanceService.GetEmployeeTransactionsAsync(request));
        }

        [HttpPost("transactions/date-range")]
        public async Task<IActionResult>GetTransactionsByDateRange(TransactionDateRangeRequestDto requestDto)
        {
            return Ok(await _leaveBalanceService.GetTransactionsByDateRangeAsync(requestDto));
        }

        #endregion
    }
}
