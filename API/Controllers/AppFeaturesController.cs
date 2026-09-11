using Application.DTOs;
using Application.Interfaces.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AppFeaturesController : ControllerBase
    {
        private readonly IAppFeatureService _service;

        public AppFeaturesController(IAppFeatureService service)
        {
            _service = service;
        }

        // ======================================================
        // GET ALL
        // ======================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();

            return Ok(result);
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // ======================================================
        // CREATE
        // ======================================================

        [HttpPost]
        public async Task<IActionResult> Create(AppFeatureDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.CreateAsync(dto);

            return Ok(result);
        }

        // ======================================================
        // UPDATE
        // ======================================================

        [HttpPut]
        public async Task<IActionResult> Update(AppFeatureDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _service.UpdateAsync(dto);

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
            var result = await _service.DeleteAsync(id);

            if (!result)
                return NotFound();

            return Ok(new
            {
                Message = "Deleted Successfully"
            });
        }

        [AllowAnonymous]
        [HttpGet("menu")]
        public async Task<IActionResult> GetMenuList()
        {
            var result = await _service.GetMenuAsync();

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpGet("menu/user/{userId}")]
        public async Task<IActionResult> GetMenuByUser(string userId)
        {
            var result = await _service.GetMenuByUserAsync(userId);

            return Ok(result);
        }

        // ======================================================
        // MENU BAR REDESIGN - Favorites / Quick Access
        // ======================================================

        [HttpGet("favorites/user/{userId}")]
        public async Task<IActionResult> GetFavorites(string userId)
        {
            var result = await _service.GetFavoritesAsync(userId);

            return Ok(result);
        }

        [HttpPost("favorites")]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteMenuRequestDto dto)
        {
            var result = await _service.AddFavoriteAsync(dto.UserId, dto.AppFeatureId, dto.TenantId);

            if (!result)
                return BadRequest(new { Message = "UserId and AppFeatureId are required." });

            return Ok(new { Message = "Pinned to Quick Access." });
        }

        [HttpDelete("favorites/{userId}/{appFeatureId}")]
        public async Task<IActionResult> RemoveFavorite(string userId, string appFeatureId)
        {
            var result = await _service.RemoveFavoriteAsync(userId, appFeatureId);

            if (!result)
                return BadRequest(new { Message = "UserId and AppFeatureId are required." });

            return Ok(new { Message = "Removed from Quick Access." });
        }
    }
}
