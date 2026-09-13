using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.Services
{
    /// <summary>
    /// Resolves the current request's TenantId directly from
    /// IHttpContextAccessor - no middleware required.
    ///
    /// Previously TenantId only ever came from an explicit SetTenantId(...)
    /// call, which only TenantMiddleware ever made (reading the
    /// "X-Tenant-ID" header, then the JWT's "TenantId" claim) - and that
    /// middleware HARD-REJECTED the request (HTTP 400 "Tenant not
    /// provided") with no way through if neither was present. That blocked
    /// EVERY endpoint under the API, including ones that never call
    /// GetTenantId() at all, the moment a user's JWT carried an empty
    /// TenantId claim (e.g. a user row whose TenantId was never seeded).
    ///
    /// GetTenantId() now does that same header-then-claim resolution
    /// itself, on demand, per call - so it still returns the right value
    /// for a real multi-tenant request, but a request missing both simply
    /// gets "" back instead of never reaching the controller. Callers that
    /// genuinely require a tenant (e.g. EsslAttendanceController) are
    /// expected to check for an empty result themselves rather than relying
    /// on a middleware to have already guaranteed one.
    ///
    /// SetTenantId(...) is kept for any explicit/background-job caller
    /// (there is no HttpContext for those - e.g. the eSSL background sync
    /// service - where an explicit value must still be honored) and always
    /// takes priority over the request-derived value once called.
    /// </summary>
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string? _explicitTenantId;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetTenantId()
        {
            if (!string.IsNullOrEmpty(_explicitTenantId))
                return _explicitTenantId;

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return "";

            var headerTenant = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerTenant))
                return headerTenant;

            var claimTenant = context.User?.Claims
                .FirstOrDefault(c => c.Type == "TenantId")?.Value;

            return claimTenant ?? "";
        }

        public void SetTenantId(string tenantId)
        {
            _explicitTenantId = tenantId;
        }
    }
}
