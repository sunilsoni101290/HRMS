using Application.DTOs.Assets;
using Application.Interfaces.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    #region Asset Allocation API

    [ApiController]
    [Route("api/asset-allocation")]
    [Authorize]
    public class AssetAllocationController : ControllerBase
    {
        private readonly IAssetAllocationService _service;

        public AssetAllocationController(IAssetAllocationService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("by-asset/{assetId}")]
        public async Task<IActionResult> GetByAsset(string assetId)
        {
            var data = await _service.GetByAssetIdAsync(assetId);
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
        public async Task<IActionResult> Create([FromBody] AssetAllocationDto dto)
        {
            var id = await _service.CreateAsync(dto);

            return Ok(new
            {
                Message = "Asset Allocated Successfully",
                Id = id
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] AssetAllocationDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);

            return Ok(new
            {
                Message = "Allocation Updated Successfully",
                Id = result
            });
        }

        [HttpPut("return/{id}")]
        public async Task<IActionResult> Return(string id, [FromBody] AssetAllocationDto dto)
        {
            var result = await _service.ReturnAsync(id, dto);

            return Ok(new
            {
                Message = "Asset Returned Successfully",
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
                Message = "Allocation Deleted Successfully"
            });
        }
    }

    #endregion
}
