using Application.Interfaces;
using Infrastructure.Interfaces;

namespace API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ITenantService tenantService)
        {
            var tenantId = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();

            if (string.IsNullOrEmpty(tenantId))
            {
                // fallback from JWT
                var claimTenant = context.User?.Claims
                    .FirstOrDefault(c => c.Type == "TenantId")?.Value;

                tenantId = claimTenant;
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Tenant not provided");
                return;
            }

            tenantService.SetTenantId(tenantId);

            await _next(context);
        }
    }
}
