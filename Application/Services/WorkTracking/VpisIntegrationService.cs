using System.Threading.Tasks;
using Application.Interfaces.WorkTracking;

namespace Application.Services.WorkTracking
{
    /// <summary>
    /// No-op stub for IVpisIntegrationService - see the interface's remarks.
    /// Registered in DI today so the Daily Work Entry module works fully
    /// standalone; swap for a real implementation once VPIS endpoints are
    /// documented.
    /// </summary>
    public class VpisIntegrationService : IVpisIntegrationService
    {
        public bool IsAvailable() => false;

        public Task<string?> TryResolveClientNameAsync(string externalClientReference)
            => Task.FromResult<string?>(null);
    }
}
