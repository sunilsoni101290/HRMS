using Application.DTOs.EmployeeLifecycle;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.EmployeeLifecycle
{
    // Phase 3 of the "Probation & Confirmation" (Employee Lifecycle)
    // module - Employee Transfer. See Domain/Entities/EmployeeTransfer.cs /
    // Application/Services/EmployeeLifecycle/EmployeeTransferService.cs for
    // the actingUserId != MakerId invariant enforced inside ApproveAsync/
    // RejectAsync (identical to ProbationConfirmationService's/
    // PipService's, no override, checked BEFORE any permission check).
    public interface IEmployeeTransferService
    {
        // Maker action - proposes new Company/Branch/Department/
        // Designation/ReportingManager values for an Employee.
        // actingUserId becomes MakerId; must hold Create permission on
        // EMPLOYEE_TRANSFER. Snapshots the Employee's CURRENT values as
        // From*; validates at least one To* differs from its From*
        // counterpart.
        Task<EmployeeTransferDto> CreateAsync(CreateEmployeeTransferDto dto, string tenantId, string actingUserId);

        // View-authorization: anyone holding View permission on
        // EMPLOYEE_TRANSFER may view any record tenant-wide (HR-internal
        // records, no per-record ownership/privacy restriction) - same
        // permissive model as ProbationConfirmationService/PipService.
        Task<EmployeeTransferDto> GetByIdAsync(string id, string tenantId, string actingUserId);

        Task<List<EmployeeTransferDto>> GetAllAsync(string tenantId, string? status, string? departmentId, string? search);

        // Convenience method for an employee-detail "transfer history" tab -
        // all EmployeeTransfer rows (any status) for one employee, newest
        // first.
        Task<List<EmployeeTransferDto>> GetTransferHistoryForEmployeeAsync(string employeeId, string tenantId);

        // Checker action - Approve. THE CORE INVARIANT: actingUserId must
        // differ from the record's MakerId (checked before the permission
        // check, no override). Applies only the non-null To* fields onto
        // the live Employee record - see EmployeeTransferService for the
        // exact "apply only non-null To fields" logic. This record IS the
        // audit trail; EmployeeService itself is never modified.
        Task<EmployeeTransferDto> ApproveAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId);

        // Checker action - Reject. Same actingUserId != MakerId invariant
        // as ApproveAsync; CheckerRemarks required. No Employee changes are
        // applied.
        Task<EmployeeTransferDto> RejectAsync(string id, CheckerActionDto dto, string actingUserId, string tenantId);
    }
}
