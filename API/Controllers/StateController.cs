using Application.DTOs.Employee;
using Application.DTOs.Masters;
using Application.Interfaces.EmployeeInterface;
using Application.Interfaces.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region State API

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StateController : ControllerBase
    {
        private readonly IStateService _stateService;

        public StateController(IStateService stateService)
        {
            _stateService = stateService;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _stateService.GetAllAsync();

            return Ok(data);
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _stateService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // ======================================================
        // CREATE
        // ======================================================

        [HttpPost]
        public async Task<IActionResult> Create(StateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _stateService.CreateAsync(dto);

            return Ok(result);
        }

        // ======================================================
        // UPDATE
        // ======================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, StateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _stateService.UpdateAsync(id, dto);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // ======================================================
        // DELETE
        // ======================================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _stateService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "State deleted successfully"
            });
        }
    }

    #endregion
}
