using Application.DTOs.Company;
using Application.Interfaces.Company;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _locationService.GetAllAsync());
        }

        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetLocationByBranch(string branchId)
        {
            return Ok(await _locationService.GetByBranchAsync(branchId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _locationService.GetByIdAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create(LocationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return Ok(await _locationService.CreateAsync(dto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, LocationDto dto)
        {
            var data = await _locationService.UpdateAsync(id, dto);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _locationService.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok();
        }
    }
}
