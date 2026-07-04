using Application.DTOs.Assets;
using Application.Interfaces.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Asset API

    [ApiController]
    [Route("api/asset")]
    [Authorize]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _service;

        public AssetController(IAssetService service)
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
        public async Task<IActionResult> Create([FromBody] AssetDto dto)
        {
            var id = await _service.CreateAsync(dto);

            return Ok(new
            {
                Message = "Asset Created Successfully",
                Id = id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AssetDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            return Ok(new
            {
                Message = "Asset Updated Successfully",
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
                Message = "Asset Deleted Successfully"
            });
        }
    }

    #endregion
}
