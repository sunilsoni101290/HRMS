using Application.DTOs.LoanAdvance;

namespace Application.Interfaces.LoanAdvance
{
    /// <summary>
    /// Read/write access to the shared, append-only LoanAdvanceAuditLog.
    /// PHASE 6 STATUS: LogAsync is implemented and callable directly by
    /// any service; Phase 16 ("Audit Trail & Activity Logs") adds an
    /// EF SaveChanges interceptor that calls it automatically for every
    /// tracked Loan/Advance entity change, so individual service methods
    /// stop needing to call it by hand.
    /// </summary>
    public interface ILoanAdvanceAuditLogService
    {
        Task LogAsync(string entityType, string entityId, string action, string? oldValuesJson, string? newValuesJson, string tenantId, string performedBy, string? ipAddress = null);

        Task<List<LoanAdvanceAuditLogDto>> GetForEntityAsync(string entityType, string entityId, string tenantId, string actingUserId);

        /// <summary>Phase 16 - tenant-wide recent activity feed (most recent first, capped at `take`) backing the standalone "Loan &amp; Advance Audit Log" page, as opposed to GetForEntityAsync's single-record drill-down.</summary>
        Task<List<LoanAdvanceAuditLogDto>> GetRecentAsync(string tenantId, string actingUserId, string? entityType = null, int take = 200);
    }
}
