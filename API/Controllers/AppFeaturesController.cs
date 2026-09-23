using Application.DTOs;
using Application.Interfaces.Masters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Feature Management CRUD (below) is System Configurator ONLY -
    // [Authorize] is the class-level baseline (any logged-in user with a
    // valid JWT reaches the menu/favorites actions further down); the CRUD
    // actions layer [Authorize(Roles="System Configurator")] on top as a
    // coarse gate, PLUS the REAL, data-driven check is
    // IAppFeatureService's EnsurePermissionAsync (RolePermission/
    // Permission against AppFeatureConstants.APP_FEATURE), same convention
    // as ErrorLogController/DatabaseManagementController - so the acting
    // user is always passed explicitly rather than trusted implicitly.
    // GetMenuList/GetMenuByUser/favorites endpoints are deliberately NOT
    // restricted - every logged-in user needs those for their own sidebar.
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

        [Authorize(Roles = "System Configurator")]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetAllAsync(actingUserId);

                return Ok(result);
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // ======================================================
        // GET BY ID
        // ======================================================

        [Authorize(Roles = "System Configurator")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.GetByIdAsync(id, actingUserId);

                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // ======================================================
        // CREATE
        // ======================================================

        [Authorize(Roles = "System Configurator")]
        [HttpPost]
        public async Task<IActionResult> Create(AppFeatureDto dto, [FromQuery] string actingUserId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.CreateAsync(dto, actingUserId);

                return Ok(result);
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // ======================================================
        // UPDATE
        // ======================================================

        [Authorize(Roles = "System Configurator")]
        [HttpPut]
        public async Task<IActionResult> Update(AppFeatureDto dto, [FromQuery] string actingUserId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.UpdateAsync(dto, actingUserId);

                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
        }

        // ======================================================
        // DELETE
        // ======================================================

        [Authorize(Roles = "System Configurator")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] string actingUserId)
        {
            try
            {
                var result = await _service.DeleteAsync(id, actingUserId);

                if (!result)
                    return NotFound();

                return Ok(new
                {
                    Message = "Deleted Successfully"
                });
            }
            catch (Application.Common.Exceptions.UnauthorizedException ex)
            {
                return Unauthorized(new { Message = ex.Message, ErrorCode = ex.ErrorCode });
            }
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
