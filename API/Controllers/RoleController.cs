using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoleController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-roles")]
        public async Task<IActionResult> Create(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            return Ok(role);
        }

        [HttpPost("assign-permissions")]
        public async Task<IActionResult> AssignPermissions(string roleId, List<string> permissionIds)
        {
            var existing = _context.RolePermissions.Where(x => x.RoleId == roleId);
            _context.RolePermissions.RemoveRange(existing);

            var newPermissions = permissionIds.Select(pid => new RolePermission
            {
                RoleId = roleId,
                PermissionId = pid
            });

            _context.RolePermissions.AddRange(newPermissions);
            await _context.SaveChangesAsync();

            return Ok("Permissions Assigned");
        }
    }
}
