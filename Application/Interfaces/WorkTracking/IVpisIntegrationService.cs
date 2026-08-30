using System.Threading.Tasks;

namespace Application.Interfaces.WorkTracking
{
    /// <summary>
    /// Integration seam for VPIS, called out twice in the Excel reference
    /// ("Client shall be linkup with VPIS" on the Entry sheet, "Structure
    /// Design linkup with VPIS" on the Engineering report) - spec section
    /// 27. NO VPIS integration, API client, or connection exists anywhere
    /// in this codebase today (confirmed by repo-wide search), so this is a
    /// clean abstraction with a no-op implementation
    /// (VpisIntegrationService) rather than an invented API contract. The
    /// module is fully functional without VPIS: Client is entered/derived
    /// manually via the Client/WorkJob masters, and IsAvailable() lets
    /// callers (e.g. a future settings screen) show "Not configured"
    /// rather than silently failing.
    ///
    /// When the real VPIS endpoints/data contract become available,
    /// implement this interface against them and register the
    /// implementation in place of the stub - no other part of this module
    /// needs to change.
    /// </summary>
    public interface IVpisIntegrationService
    {
        /// <summary>Whether a real VPIS integration is configured for this tenant - always false for the stub implementation.</summary>
        bool IsAvailable();

        /// <summary>Intended to look up a client's canonical name/code from VPIS by an external reference (e.g. VPIS Client Code). Returns null when VPIS is not available/configured - callers must treat that as "fall back to the local Client master", never as an error.</summary>
        Task<string?> TryResolveClientNameAsync(string externalClientReference);
    }
}
