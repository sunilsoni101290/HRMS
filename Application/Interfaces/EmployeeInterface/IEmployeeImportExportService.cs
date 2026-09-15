using Application.DTOs.Employee;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.EmployeeInterface
{
    public interface IEmployeeImportExportService
    {
        // Validate-only pass - resolves every master-data name, checks every
        // format/length/duplicate rule, but writes nothing. Safe to call as
        // often as needed (e.g. every time the admin re-uploads a corrected
        // file) with zero side effects.
        Task<EmployeeImportPreviewResultDto> ValidateImportAsync(
            List<EmployeeImportRowInputDto> rows,
            string tenantId,
            string userId,
            string userName);

        // Re-runs the exact same validation as ValidateImportAsync (defense
        // in depth against a stale preview / a race condition since the
        // preview was shown), and ONLY if every single row is still valid,
        // creates all of them inside one database transaction. If any row
        // fails - at validation time, or unexpectedly during the transaction
        // itself - nothing is committed and Success is false; there is no
        // partially-imported outcome.
        Task<EmployeeImportCommitResultDto> CommitImportAsync(
            List<EmployeeImportRowInputDto> rows,
            string tenantId,
            string userId,
            string userName);
    }
}
