using Application.DTOs.Employee;
using Application.Interfaces.Employee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region State API

    [ApiController]
    [Route("api/state")]
    [Authorize]
    public class StateController : ControllerBase
    {
        private readonly IStateService _service;

        public StateController(IStateService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _service.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StateDto dto)
        {
            var id = await _service.CreateAsync(dto);

            return Ok(new
            {
                Message = "State Created Successfully",
                Id = id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] StateDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            return Ok(new
            {
                Message = "State Updated Successfully",
                Id = result
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "State Deleted Successfully"
            });
        }
    }

    #endregion
}
