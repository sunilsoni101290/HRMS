using Application.DTOs.Payroll;
using Application.Interfaces;
using Application.Interfaces.Communication;
using Application.Interfaces.Payroll;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.PayrollService
{
    // Employee -> Reporting Manager -> Finance payslip request workflow.
    // See Domain/Entities/PayslipRequest.cs for the full state machine and
    // Domain/Entities/PayslipRequestAudit.cs for the audit trail shape.
    // Structured the same way as Application.Services.Attendances.
    // WfhRequestService (single-level Reporting-Manager approval, hand-rolled
    // per-service permission check rather than a shared authorization
    // service) with an extra Finance stage layered on top.
    public class PayslipRequestService : IPayslipRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PayslipRequestService> _logger;

        public PayslipRequestService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailSender emailSender,
            ILogger<PayslipRequestService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logger = logger;
        }

        // Non-terminal statuses - an employee may have at most one of these
        // open per Payroll period at a time (duplicate-request prevention).
        private static readonly PayslipRequestStatus[] NonTerminalStatuses =
        {
            PayslipRequestStatus.PendingManagerApproval,
            PayslipRequestStatus.ApprovedByManager,
            PayslipRequestStatus.PendingFinanceAction,
            PayslipRequestStatus.PayslipGenerated
        };

        #region Create

        public async Task<PayslipRequestDto> CreateAsync(CreatePayslipRequestDto dto, string tenantId, string actingUserId)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            // Self-only - never on behalf of another employee, even if a
            // different EmployeeId is posted from the client.
            if (string.IsNullOrEmpty(actingEmployeeId) || actingEmployeeId != dto.EmployeeId)
                throw new UnauthorizedAccessException("You can only submit a payslip request for yourself.");

            var payroll = await _context.Payrolls
                .FirstOrDefaultAsync(x => x.Id == dto.PayrollId && x.TenantId == tenantId && !x.IsDeleted);

            if (payroll == null)
                throw new Exception("Payroll period not found.");

            if (payroll.EmployeeId != dto.EmployeeId)
                throw new UnauthorizedAccessException("You can only request a payslip for your own payroll period.");

            // Duplicate/overlap prevention - no more than one non-terminal
            // request per employee+period at a time.
            var duplicate = await _context.PayslipRequests
                .Where(x =>
                    x.EmployeeId == dto.EmployeeId &&
                    x.PayrollId == dto.PayrollId &&
                    x.TenantId == tenantId &&
                    NonTerminalStatuses.Contains(x.Status))
                .FirstOrDefaultAsync();

            if (duplicate != null)
                throw new Exception(
                    $"You already have a payslip request for this payroll period with status '{duplicate.Status}'. " +
                    "Please wait for it to be resolved before submitting another.");

            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new Exception("Employee not found.");

            var entity = new PayslipRequest
            {
                Id = IDManager.GetNewId(new PayslipRequest()),

                EmployeeId = dto.EmployeeId,
                PayrollId = dto.PayrollId,
                PayrollYear = payroll.SalaryYear,
                PayrollMonth = payroll.SalaryMonth,

                Status = PayslipRequestStatus.PendingManagerApproval,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = actingUserId
            };

            _context.PayslipRequests.Add(entity);

            await AddAuditAsync(entity.Id, "Requested", actingUserId, null, tenantId);

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(employee.ReportingManagerId))
            {
                var managerUserId = await ResolveUserIdForEmployeeAsync(employee.ReportingManagerId);

                if (!string.IsNullOrEmpty(managerUserId))
                {
                    await NotifyAsync(
                        managerUserId,
                        "Payslip Request Submitted",
                        $"{FullName(employee)} has requested their payslip for {payroll.SalaryMonth}/{payroll.SalaryYear}. Please review.",
                        "Info", entity.Id, tenantId, actingUserId);
                }
                else
                {
                    _logger.LogInformation(
                        "Reporting manager {ManagerEmployeeId} of employee {EmployeeId} has no linked User account - skipping manager notification for payslip request {RequestId}.",
                        employee.ReportingManagerId, employee.Id, entity.Id);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Employee {EmployeeId} has no Reporting Manager configured - payslip request {RequestId} has no one to notify at the manager stage.",
                    employee.Id, entity.Id);
            }

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region Reads

        private async Task<PayslipRequest> GetEntityByIdAsync(string id, string tenantId)
        {
            var entity = await _context.PayslipRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.Payroll)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (entity == null)
                throw new Exception("Payslip request not found.");

            return entity;
        }

        // Internal, trusted re-fetch used immediately after this service
        // itself already performed and authorized a mutation - no
        // additional authorization check here.
        private async Task<PayslipRequestDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        public async Task<PayslipRequestDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityByIdAsync(id, tenantId);

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isOwnRequest = !string.IsNullOrEmpty(actingEmployeeId) && entity.EmployeeId == actingEmployeeId;

            bool isReportingManager =
                !string.IsNullOrEmpty(entity.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                entity.Employee.ReportingManagerId == actingEmployeeId;

            if (!isOwnRequest && !isReportingManager && !await IsFinanceOrAdminForPayrollAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to view this payslip request.");

            return await AssembleDtoAsync(entity);
        }

        public async Task<List<PayslipRequestDto>> GetMyRequestsAsync(string actingUserId, string tenantId)
        {
            var employeeId = await GetActingEmployeeIdAsync(actingUserId);

            if (string.IsNullOrEmpty(employeeId))
                return new List<PayslipRequestDto>();

            var entities = await _context.PayslipRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.Payroll)
                .Where(x => x.EmployeeId == employeeId && x.TenantId == tenantId)
                .OrderByDescending(x => x.CreatedOn)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<PayslipRequestDto>> GetPendingForManagerAsync(string actingUserId, string tenantId)
        {
            bool isOverride = await IsFinanceOrAdminForPayrollAsync(actingUserId);
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            var query = _context.PayslipRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.Payroll)
                .Where(x => x.TenantId == tenantId && x.Status == PayslipRequestStatus.PendingManagerApproval)
                .AsQueryable();

            if (!isOverride)
            {
                if (string.IsNullOrEmpty(actingEmployeeId))
                    return new List<PayslipRequestDto>();

                query = query.Where(x => x.Employee.ReportingManagerId == actingEmployeeId);
            }

            var entities = await query.OrderBy(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<PayslipRequestDto>> GetPendingForFinanceAsync(string actingUserId, string tenantId)
        {
            if (!await IsFinanceOrAdminForPayrollAsync(actingUserId))
                return new List<PayslipRequestDto>();

            var entities = await _context.PayslipRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.Payroll)
                .Where(x => x.TenantId == tenantId &&
                    (x.Status == PayslipRequestStatus.PendingFinanceAction ||
                     x.Status == PayslipRequestStatus.PayslipGenerated))
                .OrderBy(x => x.CreatedOn)
                .ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        public async Task<List<PayslipRequestDto>> GetAllAsync(string tenantId, string? status)
        {
            var query = _context.PayslipRequests
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.ReportingManager)
                .Include(x => x.Payroll)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PayslipRequestStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            return await AssembleDtoListAsync(entities);
        }

        #endregion

        #region Manager Stage

        public async Task<PayslipRequestDto> ManagerApproveAsync(string id, string? remarks, string actingUserId, string tenantId)
        {
            var request = await _context.PayslipRequests
                .Include(x => x.Employee)
                .Include(x => x.Payroll)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Payslip request not found.");

            if (request.Status != PayslipRequestStatus.PendingManagerApproval)
                throw new Exception("Only requests pending manager approval can be approved.");

            await EnsureManagerAuthorizedAsync(request, actingUserId);

            var now = DateTime.UtcNow;

            // Manager-approve immediately auto-forwards to Finance in the
            // same call - there is no separate manual "forward" step in
            // this workflow.
            request.Status = PayslipRequestStatus.ApprovedByManager;
            request.ManagerActionBy = actingUserId;
            request.ManagerActionOn = now;
            request.ManagerRemarks = remarks;
            request.ModifiedOn = now;
            request.ModifiedBy = actingUserId;

            await AddAuditAsync(request.Id, "ApprovedByManager", actingUserId, remarks, tenantId);

            request.Status = PayslipRequestStatus.PendingFinanceAction;
            await AddAuditAsync(request.Id, "ForwardedToFinance", actingUserId, null, tenantId);

            await _context.SaveChangesAsync();

            try
            {
                var financeUserIds = await ResolveFinanceUserIdsAsync(tenantId);

                foreach (var userId in financeUserIds)
                {
                    await NotifyAsync(
                        userId,
                        "Payslip Ready for Finance Action",
                        $"A payslip request for {FullName(request.Employee)} ({request.PayrollMonth}/{request.PayrollYear}) has been approved by the Reporting Manager and needs to be generated/uploaded.",
                        "Info", request.Id, tenantId, actingUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Finance notification failed for payslip request {RequestId} - the approval itself is unaffected.", request.Id);
            }

            await NotifyApplicantAsync(
                request.EmployeeId,
                "Payslip Request Approved by Manager",
                "Your payslip request was approved by your Reporting Manager and has been forwarded to Finance.",
                "Success", request.Id, tenantId, actingUserId);

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        public async Task<PayslipRequestDto> ManagerRejectAsync(string id, string reason, string actingUserId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Rejection remarks are required.");

            var request = await _context.PayslipRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Payslip request not found.");

            if (request.Status != PayslipRequestStatus.PendingManagerApproval)
                throw new Exception("Only requests pending manager approval can be rejected.");

            await EnsureManagerAuthorizedAsync(request, actingUserId);

            request.Status = PayslipRequestStatus.RejectedByManager;
            request.ManagerActionBy = actingUserId;
            request.ManagerActionOn = DateTime.UtcNow;
            request.ManagerRemarks = reason.Trim();
            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await AddAuditAsync(request.Id, "RejectedByManager", actingUserId, reason, tenantId);

            await _context.SaveChangesAsync();

            await NotifyApplicantAsync(
                request.EmployeeId,
                "Payslip Request Rejected",
                $"Your payslip request was rejected by your Reporting Manager. Reason: {reason.Trim()}",
                "Error", request.Id, tenantId, actingUserId);

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        #endregion

        #region Finance Stage

        public async Task<PayslipRequestDto> FinanceUploadAsync(string id, string documentUrl, string documentFileName, string? remarks, string actingUserId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(documentUrl))
                throw new Exception("Document URL is required.");

            var request = await _context.PayslipRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Payslip request not found.");

            if (request.Status != PayslipRequestStatus.PendingFinanceAction)
                throw new Exception("Only requests pending finance action can have a payslip uploaded.");

            if (!await IsFinanceOrAdminForPayrollAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to upload a payslip for this request.");

            request.DocumentUrl = documentUrl;
            request.DocumentFileName = documentFileName;
            request.GeneratedBy = actingUserId;
            request.GeneratedOn = DateTime.UtcNow;
            request.FinanceRemarks = remarks;
            request.Status = PayslipRequestStatus.PayslipGenerated;
            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await AddAuditAsync(request.Id, "PayslipGenerated", actingUserId, remarks, tenantId);

            await _context.SaveChangesAsync();

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        public async Task<PayslipRequestDto> FinanceCompleteAsync(string id, string? remarks, string actingUserId, string tenantId)
        {
            var request = await _context.PayslipRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Payslip request not found.");

            if (request.Status != PayslipRequestStatus.PayslipGenerated)
                throw new Exception("Upload the payslip document before marking this request Completed.");

            if (!await IsFinanceOrAdminForPayrollAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to complete this request.");

            request.CompletedBy = actingUserId;
            request.CompletedOn = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(remarks))
                request.FinanceRemarks = remarks;

            request.Status = PayslipRequestStatus.Completed;
            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await AddAuditAsync(request.Id, "Completed", actingUserId, remarks, tenantId);

            await _context.SaveChangesAsync();

            await NotifyApplicantAsync(
                request.EmployeeId,
                "Payslip Ready",
                "Your payslip is ready. You can now view/download it.",
                "Success", request.Id, tenantId, actingUserId);

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        public async Task<PayslipRequestDto> FinanceRejectAsync(string id, string reason, string actingUserId, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new Exception("Rejection reason is required.");

            var request = await _context.PayslipRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Payslip request not found.");

            if (request.Status != PayslipRequestStatus.PendingFinanceAction &&
                request.Status != PayslipRequestStatus.PayslipGenerated)
                throw new Exception("Only requests pending finance action or already generated can be rejected by Finance.");

            if (!await IsFinanceOrAdminForPayrollAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to reject this request.");

            request.Status = PayslipRequestStatus.RejectedByFinance;
            request.FinanceActionBy = actingUserId;
            request.FinanceActionOn = DateTime.UtcNow;
            request.FinanceRemarks = reason.Trim();
            request.ModifiedOn = DateTime.UtcNow;
            request.ModifiedBy = actingUserId;

            await AddAuditAsync(request.Id, "RejectedByFinance", actingUserId, reason, tenantId);

            await _context.SaveChangesAsync();

            await NotifyApplicantAsync(
                request.EmployeeId,
                "Payslip Request Rejected by Finance",
                $"Your payslip request was rejected by Finance. Reason: {reason.Trim()}",
                "Error", request.Id, tenantId, actingUserId);

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        #endregion

        #region Download

        public async Task<PayslipRequestDto> GetForDownloadAsync(string id, string actingUserId, string tenantId)
        {
            var request = await _context.PayslipRequests
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId);

            if (request == null)
                throw new Exception("Payslip request not found.");

            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isOwnRequest = !string.IsNullOrEmpty(actingEmployeeId) && request.EmployeeId == actingEmployeeId;

            if (!isOwnRequest && !await IsFinanceOrAdminForPayrollAsync(actingUserId))
                throw new UnauthorizedAccessException("You are not authorized to download this payslip.");

            if (request.Status != PayslipRequestStatus.Completed || string.IsNullOrEmpty(request.DocumentUrl))
            {
                var reason = request.Status == PayslipRequestStatus.RejectedByManager ||
                             request.Status == PayslipRequestStatus.RejectedByFinance
                    ? $" This request was rejected. Reason: {request.FinanceRemarks ?? request.ManagerRemarks}"
                    : "";

                throw new Exception($"Payslip is not yet available (current status: {request.Status}).{reason}");
            }

            try
            {
                await AddAuditAsync(request.Id, "Downloaded", actingUserId, null, tenantId);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Never let the audit write block an otherwise-authorized
                // download.
                _logger.LogWarning(ex, "Failed to write Downloaded audit row for payslip request {RequestId}.", request.Id);
            }

            return await GetByIdInternalAsync(request.Id, tenantId);
        }

        #endregion

        #region Authorization Helpers

        // Resolves the acting user's linked EmployeeId from their User.Id -
        // never trusted from client input. Same pattern as
        // WfhRequestService.GetActingEmployeeIdAsync.
        private async Task<string?> GetActingEmployeeIdAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
            return user?.EmployeeId;
        }

        private async Task<string?> ResolveUserIdForEmployeeAsync(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
                return null;

            return await _context.Users
                .Where(u => u.EmployeeId == employeeId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        // Same hand-rolled per-service permission check convention as every
        // other module in this codebase (e.g.
        // WfhRequestService.IsHrOrAdminForAttendanceAsync) - does the acting
        // user hold, through any Role assigned to them, an allowed
        // RolePermission for the Approve action on the
        // PAYROLL_PAYSLIP_REQUEST feature. Used both as the Finance-role
        // gate AND as the HR/Admin override at every stage (manager
        // approve/reject included) - a tenant creates a "Finance" Role via
        // the existing Role/Permission admin screens and grants it this
        // permission; Admin/HR roles are expected to already hold it (or be
        // granted it) for support/override purposes.
        private async Task<bool> IsFinanceOrAdminForPayrollAsync(string? actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.PAYROLL_PAYSLIP_REQUEST && x.Action == Actions.Approve)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        // All distinct User.Ids holding the Finance/Admin override
        // permission for this tenant - used to fan out the
        // "forwarded to Finance" notification to everyone who can act on
        // it, rather than a single fixed recipient.
        private async Task<List<string>> ResolveFinanceUserIdsAsync(string tenantId)
        {
            return await (
                from ur in _context.UserRoles
                join rp in _context.RolePermissions.Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _context.Permissions.Where(x =>
                        x.FeatureId == AppFeatureConstants.PAYROLL_PAYSLIP_REQUEST && x.Action == Actions.Approve)
                    on rp.PermissionId equals p.Id
                join u in _context.Users on ur.UserId equals u.Id
                where u.TenantId == tenantId
                select ur.UserId
            ).Distinct().ToListAsync();
        }

        // Either the acting user is the request's employee's direct
        // ReportingManagerId, or they hold the Finance/HR/Admin override.
        private async Task EnsureManagerAuthorizedAsync(PayslipRequest request, string actingUserId)
        {
            var actingEmployeeId = await GetActingEmployeeIdAsync(actingUserId);

            bool isReportingManager =
                !string.IsNullOrEmpty(request.Employee?.ReportingManagerId) &&
                !string.IsNullOrEmpty(actingEmployeeId) &&
                request.Employee.ReportingManagerId == actingEmployeeId;

            if (isReportingManager)
                return;

            if (await IsFinanceOrAdminForPayrollAsync(actingUserId))
                return;

            throw new UnauthorizedAccessException("You are not authorized to act on this payslip request.");
        }

        #endregion

        #region Audit Trail

        private async Task AddAuditAsync(string payslipRequestId, string action, string performedBy, string? remarks, string tenantId)
        {
            var audit = new PayslipRequestAudit
            {
                Id = IDManager.GetNewId(new PayslipRequestAudit()),
                PayslipRequestId = payslipRequestId,
                Action = action,
                PerformedBy = performedBy,
                Remarks = remarks,
                PerformedOn = DateTime.UtcNow,

                TenantId = tenantId,
                CreatedOn = DateTime.UtcNow,
                CreatedBy = performedBy
            };

            await _context.PayslipRequestAudits.AddAsync(audit);
        }

        #endregion

        #region Notifications

        // Best-effort fan-out: creates the in-app Notification (the channel
        // that must actually work) and then, independently, tries the
        // email (secondary channel that must never be able to break the
        // caller). Every failure is caught and logged here, never
        // rethrown - same pattern as
        // LeaveApplicationService.NotifyLeaveEventAsync.
        private async Task NotifyAsync(
            string userId, string title, string message, string severity,
            string requestId, string? tenantId, string? actorUserId)
        {
            try
            {
                await _notificationService.CreateDirectAsync(
                    userId, title, message, severity,
                    redirectUrl: $"/PayslipRequest/Details/{requestId}",
                    featureId: AppFeatureConstants.PAYROLL_PAYSLIP_REQUEST,
                    referenceId: requestId,
                    tenantId: tenantId,
                    createdBy: actorUserId ?? "System");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "In-app notification failed for payslip request {RequestId}, user {UserId}.", requestId, userId);
            }

            try
            {
                var email = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(email))
                    await _emailSender.SendAsync(email, title, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Email notification failed for payslip request {RequestId}, user {UserId}.", requestId, userId);
            }
        }

        // Resolution must never throw out of this method - every call site
        // sits inside an already-committed workflow transition, same
        // rationale as LeaveApplicationService.NotifyApplicantAsync.
        private async Task NotifyApplicantAsync(
            string employeeId, string title, string message, string severity,
            string requestId, string? tenantId, string? actorUserId)
        {
            try
            {
                var userId = await ResolveUserIdForEmployeeAsync(employeeId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation(
                        "Employee {EmployeeId} has no linked User account - skipping applicant notification for payslip request {RequestId}.",
                        employeeId, requestId);
                    return;
                }

                await NotifyAsync(userId, title, message, severity, requestId, tenantId, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Applicant notification resolution/dispatch failed for payslip request {RequestId} - the workflow transition itself is unaffected.",
                    requestId);
            }
        }

        #endregion

        #region DTO Assembly

        private static string FullName(Employee? e) => e != null ? $"{e.FirstName} {e.LastName}".Trim() : "";

        private static PayslipRequestDto AssembleDto(PayslipRequest x)
        {
            return new PayslipRequestDto
            {
                Id = x.Id,

                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? FullName(x.Employee) : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,
                ReportingManagerName = x.Employee?.ReportingManager != null
                    ? FullName(x.Employee.ReportingManager)
                    : null,

                PayrollId = x.PayrollId,
                PayrollYear = x.PayrollYear,
                PayrollMonth = x.PayrollMonth,

                Status = (int)x.Status,
                StatusName = x.Status.ToString(),

                ManagerRemarks = x.ManagerRemarks,
                ManagerActionOn = x.ManagerActionOn,

                FinanceRemarks = x.FinanceRemarks,
                FinanceActionOn = x.FinanceActionOn,

                DocumentUrl = x.Status == PayslipRequestStatus.Completed ? x.DocumentUrl : null,
                DocumentFileName = x.Status == PayslipRequestStatus.Completed ? x.DocumentFileName : null,

                GeneratedOn = x.GeneratedOn,
                CompletedOn = x.CompletedOn,

                TenantId = x.TenantId,
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            };
        }

        // Batches name-resolution for every User.Id referenced anywhere in
        // the given requests (ManagerActionBy/FinanceActionBy/GeneratedBy/
        // CompletedBy) plus every audit row's PerformedBy, in as few
        // queries as possible - mirrors WfhRequestService.
        // BuildApprovedByNameMapAsync.
        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();

            if (ids.Count == 0)
                return new Dictionary<string, string>();

            var users = await _context.Users
                .Include(u => u.Employee)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? FullName(u.Employee) : u.Username);
        }

        private async Task<PayslipRequestDto> AssembleDtoAsync(PayslipRequest x)
        {
            var dto = AssembleDto(x);

            var userIds = new List<string?> { x.ManagerActionBy, x.FinanceActionBy, x.GeneratedBy, x.CompletedBy }
                .Where(v => !string.IsNullOrEmpty(v)).Select(v => v!).ToList();

            var audits = await _context.PayslipRequestAudits
                .Where(a => a.PayslipRequestId == x.Id)
                .OrderBy(a => a.PerformedOn)
                .ToListAsync();

            userIds.AddRange(audits.Select(a => a.PerformedBy));

            var nameMap = await BuildUserNameMapAsync(userIds);

            dto.ManagerActionByName = !string.IsNullOrEmpty(x.ManagerActionBy) && nameMap.TryGetValue(x.ManagerActionBy, out var mn) ? mn : null;
            dto.FinanceActionByName = !string.IsNullOrEmpty(x.FinanceActionBy) && nameMap.TryGetValue(x.FinanceActionBy, out var fn) ? fn : null;
            dto.GeneratedByName = !string.IsNullOrEmpty(x.GeneratedBy) && nameMap.TryGetValue(x.GeneratedBy, out var gn) ? gn : null;
            dto.CompletedByName = !string.IsNullOrEmpty(x.CompletedBy) && nameMap.TryGetValue(x.CompletedBy, out var cn) ? cn : null;

            dto.Timeline = audits.Select(a => new PayslipRequestAuditDto
            {
                Action = a.Action,
                PerformedByName = nameMap.TryGetValue(a.PerformedBy, out var pn) ? pn : null,
                Remarks = a.Remarks,
                PerformedOn = a.PerformedOn
            }).ToList();

            return dto;
        }

        private async Task<List<PayslipRequestDto>> AssembleDtoListAsync(List<PayslipRequest> entities)
        {
            if (entities.Count == 0)
                return new List<PayslipRequestDto>();

            var actionUserIds = entities
                .SelectMany(x => new[] { x.ManagerActionBy, x.FinanceActionBy, x.GeneratedBy, x.CompletedBy })
                .Where(v => !string.IsNullOrEmpty(v)).Select(v => v!).ToList();

            var nameMap = await BuildUserNameMapAsync(actionUserIds);

            var list = new List<PayslipRequestDto>();

            foreach (var x in entities)
            {
                var dto = AssembleDto(x);

                dto.ManagerActionByName = !string.IsNullOrEmpty(x.ManagerActionBy) && nameMap.TryGetValue(x.ManagerActionBy, out var mn) ? mn : null;
                dto.FinanceActionByName = !string.IsNullOrEmpty(x.FinanceActionBy) && nameMap.TryGetValue(x.FinanceActionBy, out var fn) ? fn : null;
                dto.GeneratedByName = !string.IsNullOrEmpty(x.GeneratedBy) && nameMap.TryGetValue(x.GeneratedBy, out var gn) ? gn : null;
                dto.CompletedByName = !string.IsNullOrEmpty(x.CompletedBy) && nameMap.TryGetValue(x.CompletedBy, out var cn) ? cn : null;

                // List views intentionally skip the per-row Timeline query
                // (N+1) - Details is where the full Timeline is loaded.
                list.Add(dto);
            }

            return list;
        }

        #endregion
    }
}
