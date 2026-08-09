using Application.Common.Exceptions;
using Application.DTOs.LoanAdvance;
using Application.Interfaces;
using Application.Interfaces.Communication;
using Application.Interfaces.LoanAdvance;
using Domain.Entities;
using Domain.Helper;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Domain.Enums.EnumExtensions;

namespace Application.Services.LoanAdvance
{
    /// <summary>
    /// Orchestrates the EmployeeAdvance lifecycle. UNLIKE EmployeeLoanService's
    /// configurable N-level matrix (driven by LoanPolicyApprovalLevel),
    /// Advances use SINGLE-LEVEL approval - the requesting employee's
    /// direct ReportingManagerId, or anyone holding Approve permission on
    /// AppFeatureConstants.EMPLOYEE_ADVANCE as an HR/Finance override -
    /// the same shape as WfhRequestService/OnDutyRequestService, and what
    /// Phase 2's Advance Lifecycle diagram actually specified (Advances
    /// are the short-term/low-friction counterpart to Loans, so they don't
    /// carry a configurable multi-level matrix or a dedicated AdvancePolicy
    /// table - there was never one in the Phase 3/4 schema). Maker != Checker
    /// is still enforced with no override, same invariant as every other
    /// workflow in this codebase.
    /// </summary>
    public class EmployeeAdvanceService : IEmployeeAdvanceService
    {
        private readonly IUnitOfWork _uow;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<EmployeeAdvanceService> _logger;

        public EmployeeAdvanceService(
            IUnitOfWork uow,
            INotificationService notificationService,
            IEmailSender emailSender,
            ILogger<EmployeeAdvanceService> logger)
        {
            _uow = uow;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logger = logger;
        }

        #region Read

        public async Task<List<EmployeeAdvanceListDto>> GetAllAsync(string tenantId, string actingUserId, string? status = null, string? employeeId = null)
        {
            var isSelfOnly = !await HasPermissionAsync(actingUserId, Actions.View);
            var ownEmployeeId = await GetEmployeeIdForUserAsync(actingUserId);

            if (isSelfOnly && string.IsNullOrEmpty(ownEmployeeId))
                throw new UnauthorizedException("You are not authorized to view advance requests.");

            var query = _uow.Repository<EmployeeAdvance>().Query()
                .Include(x => x.Employee)
                .Include(x => x.AdvanceType)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (isSelfOnly)
                query = query.Where(x => x.EmployeeId == ownEmployeeId);
            else if (!string.IsNullOrWhiteSpace(employeeId))
                query = query.Where(x => x.EmployeeId == employeeId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<AdvanceStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();
            return entities.Select(MapList).ToList();
        }

        public async Task<EmployeeAdvanceDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityWithIncludesAsync(id, tenantId);
            await EnsureCanViewAsync(entity, actingUserId);
            return await AssembleDtoAsync(entity);
        }

        /// <summary>PHASE 18 - batch-resolves reporting managers for every candidate in one query instead of the original per-candidate N+1 (GetReportingManagerUserIdAsync did 2 round trips PER pending advance).</summary>
        public async Task<List<EmployeeAdvanceListDto>> GetPendingOnMeAsync(string tenantId, string actingUserId)
        {
            var candidates = await _uow.Repository<EmployeeAdvance>().Query()
                .Include(x => x.Employee)
                .Include(x => x.AdvanceType)
                .Where(x => x.TenantId == tenantId && x.Status == AdvanceStatus.PendingApproval && x.MakerId != actingUserId)
                .ToListAsync();

            if (candidates.Count == 0)
                return new List<EmployeeAdvanceListDto>();

            var reportingManagerEmployeeIds = candidates
                .Select(x => x.Employee?.ReportingManagerId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var managerUserIdMap = reportingManagerEmployeeIds.Count == 0
                ? new Dictionary<string, string>()
                : await _uow.Repository<User>().Query()
                    .Where(u => reportingManagerEmployeeIds.Contains(u.EmployeeId))
                    .ToDictionaryAsync(u => u.EmployeeId, u => u.Id);

            var result = candidates
                .Where(advance =>
                {
                    var mgrEmpId = advance.Employee?.ReportingManagerId;
                    return mgrEmpId != null && managerUserIdMap.TryGetValue(mgrEmpId, out var mgrUserId) && mgrUserId == actingUserId;
                })
                .Select(MapList)
                .OrderByDescending(x => x.CreatedOn)
                .ToList();

            return result;
        }

        /// <summary>See IEmployeeAdvanceService for the reminder-sweep rationale.</summary>
        public async Task<List<string>> ResolveCurrentApproverUserIdsAsync(string employeeAdvanceId)
        {
            var advance = await _uow.Repository<EmployeeAdvance>().Query()
                .FirstOrDefaultAsync(x => x.Id == employeeAdvanceId);

            if (advance == null || advance.Status != AdvanceStatus.PendingApproval)
                return new List<string>();

            var managerUserId = await GetReportingManagerUserIdAsync(advance.EmployeeId);
            return managerUserId == null ? new List<string>() : new List<string> { managerUserId };
        }

        #endregion

        #region Maker: Submit

        public async Task<EmployeeAdvanceDto> SubmitAsync(AdvanceSubmitDto dto, string tenantId, string actingUserId)
        {
            if (dto == null) throw new BadRequestException("Request body is required.");
            if (string.IsNullOrWhiteSpace(dto.EmployeeId)) throw new BadRequestException("EmployeeId is required.");
            if (string.IsNullOrWhiteSpace(dto.AdvanceTypeId)) throw new BadRequestException("AdvanceTypeId is required.");
            if (dto.RequestedAmount <= 0) throw new BadRequestException("Requested Amount must be greater than zero.");
            if (dto.InstallmentCount <= 0) throw new BadRequestException("Installment Count must be greater than zero.");

            await EnsureSelfOrHrAsync(dto.EmployeeId, actingUserId, Actions.Create);

            var employee = await _uow.Repository<Employee>().Query()
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new NotFoundException("Employee not found.");

            var advanceType = await _uow.Repository<AdvanceType>().Query()
                .FirstOrDefaultAsync(x => x.Id == dto.AdvanceTypeId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive);

            if (advanceType == null)
                throw new NotFoundException("Advance Type not found or inactive.");

            if (advanceType.MaxAmount.HasValue && dto.RequestedAmount > advanceType.MaxAmount.Value)
                throw new BadRequestException($"Requested amount exceeds the maximum of {advanceType.MaxAmount:N0} for this Advance Type.");

            if (dto.InstallmentCount > advanceType.MaxInstallments)
                throw new BadRequestException($"Installment Count cannot exceed {advanceType.MaxInstallments} for this Advance Type.");

            var hasOpenAdvanceOfType = await _uow.Repository<EmployeeAdvance>().ExistsAsync(x =>
                x.EmployeeId == dto.EmployeeId && x.AdvanceTypeId == dto.AdvanceTypeId &&
                x.Status != AdvanceStatus.Settled && x.Status != AdvanceStatus.Rejected && x.Status != AdvanceStatus.Cancelled);

            if (hasOpenAdvanceOfType)
                throw new BadRequestException("You already have an open request/advance of this type.");

            var managerUserId = await GetReportingManagerUserIdAsync(dto.EmployeeId);

            var entity = new EmployeeAdvance
            {
                TenantId = tenantId,
                CompanyId = employee.CompanyId,
                BranchId = employee.BranchId,
                EmployeeId = dto.EmployeeId,
                AdvanceTypeId = dto.AdvanceTypeId,
                RequestedAmount = dto.RequestedAmount,
                InstallmentCount = dto.InstallmentCount,
                Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
                Status = managerUserId != null ? AdvanceStatus.PendingApproval : AdvanceStatus.Approved,
                CurrentApprovalLevel = managerUserId != null ? 1 : 0,
                MakerId = actingUserId,
                MakerActionOn = DateTime.UtcNow,
                OutstandingAmount = 0,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            };

            // No Reporting Manager on file - auto-advance to Approved so
            // the request isn't stuck with no possible approver; Finance
            // still reviews at Disbursement time either way.
            if (managerUserId != null)
                entity.ApprovedAmount = null;
            else
                entity.ApprovedAmount = dto.RequestedAmount;

            await _uow.Repository<EmployeeAdvance>().AddAsync(entity);
            await _uow.SaveChangesAsync();

            if (managerUserId != null)
            {
                await NotifyAdvanceEventAsync(new[] { managerUserId },
                    "New Advance Request Awaiting Your Approval",
                    $"An advance request of {entity.RequestedAmount:N0} from {employee.FirstName} {employee.LastName} is awaiting your approval.",
                    "Info", entity.Id, tenantId, actingUserId);
            }
            else
            {
                // No Reporting Manager on file - auto-approved; let the
                // applicant AND Finance know.
                await NotifyApplicantAsync(entity, "Advance Request Approved",
                    $"Your advance request of {entity.RequestedAmount:N0} has been approved and is awaiting disbursement.",
                    "Success", tenantId, actingUserId);

                var financeUsers = await ResolveUsersWithPermissionAsync(Actions.Approve);
                await NotifyAdvanceEventAsync(financeUsers,
                    "Advance Ready for Disbursement",
                    $"{employee.FirstName} {employee.LastName}'s advance of {entity.RequestedAmount:N0} is approved and ready for disbursement.",
                    "Info", entity.Id, tenantId, actingUserId);
            }

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region Checker: Approve / Reject

        public async Task<EmployeeAdvanceDto> ApproveAsync(AdvanceApprovalActionDto dto, string tenantId, string actingUserId)
        {
            var advance = await GetEntityWithIncludesAsync(dto?.EmployeeAdvanceId, tenantId, forUpdate: true);

            EnsureCheckerIsNotMaker(advance, actingUserId);

            if (advance.Status != AdvanceStatus.PendingApproval)
                throw new BadRequestException("This advance is not currently awaiting approval.");

            await EnsureIsApproverAsync(advance, actingUserId);

            await _uow.Repository<AdvanceApprovalHistory>().AddAsync(new AdvanceApprovalHistory
            {
                EmployeeAdvanceId = advance.Id,
                LevelNumber = 1,
                CheckerId = actingUserId,
                Decision = ApprovalDecision.Approved,
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim(),
                ActionOn = DateTime.UtcNow,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            advance.Status = AdvanceStatus.Approved;
            advance.ApprovedAmount = dto.ApprovedAmount ?? advance.RequestedAmount;
            advance.ModifiedBy = actingUserId;
            advance.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<EmployeeAdvance>().Update(advance);
            await _uow.SaveChangesAsync();

            await NotifyApplicantAsync(advance, "Advance Request Approved",
                $"Your advance request of {advance.RequestedAmount:N0} has been approved and is awaiting disbursement.",
                "Success", tenantId, actingUserId);

            var financeUsers = await ResolveUsersWithPermissionAsync(Actions.Approve);
            await NotifyAdvanceEventAsync(financeUsers,
                "Advance Ready for Disbursement",
                $"{advance.Employee?.FirstName} {advance.Employee?.LastName}'s advance of {advance.RequestedAmount:N0} is approved and ready for disbursement.",
                "Info", advance.Id, tenantId, actingUserId);

            return await GetByIdInternalAsync(advance.Id, tenantId);
        }

        public async Task<EmployeeAdvanceDto> RejectAsync(AdvanceApprovalActionDto dto, string tenantId, string actingUserId)
        {
            var advance = await GetEntityWithIncludesAsync(dto?.EmployeeAdvanceId, tenantId, forUpdate: true);

            EnsureCheckerIsNotMaker(advance, actingUserId);

            if (advance.Status != AdvanceStatus.PendingApproval)
                throw new BadRequestException("This advance is not currently awaiting approval.");

            if (string.IsNullOrWhiteSpace(dto.Remarks))
                throw new BadRequestException("Remarks are required to reject an advance request.");

            await EnsureIsApproverAsync(advance, actingUserId);

            await _uow.Repository<AdvanceApprovalHistory>().AddAsync(new AdvanceApprovalHistory
            {
                EmployeeAdvanceId = advance.Id,
                LevelNumber = 1,
                CheckerId = actingUserId,
                Decision = ApprovalDecision.Rejected,
                Remarks = dto.Remarks.Trim(),
                ActionOn = DateTime.UtcNow,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            advance.Status = AdvanceStatus.Rejected;
            advance.ModifiedBy = actingUserId;
            advance.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<EmployeeAdvance>().Update(advance);
            await _uow.SaveChangesAsync();

            await NotifyApplicantAsync(advance, "Advance Request Rejected",
                $"Your advance request of {advance.RequestedAmount:N0} was rejected. Reason: {dto.Remarks.Trim()}",
                "Error", tenantId, actingUserId);

            return await GetByIdInternalAsync(advance.Id, tenantId);
        }

        #endregion

        #region Finance: Disburse / Settle

        public async Task<EmployeeAdvanceDto> DisburseAsync(AdvanceDisbursementDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Approve);

            var advance = await GetEntityWithIncludesAsync(dto?.EmployeeAdvanceId, tenantId, forUpdate: true);

            if (advance.Status != AdvanceStatus.Approved)
                throw new BadRequestException("Only an Approved advance can be disbursed.");

            if (dto.DisbursedAmount <= 0)
                throw new BadRequestException("Disbursed Amount must be greater than zero.");

            advance.DisbursedAmount = dto.DisbursedAmount;
            advance.ApprovedAmount ??= dto.DisbursedAmount;
            advance.DisbursedOn = dto.DisbursedOn == default ? DateTime.UtcNow : dto.DisbursedOn;
            advance.DisbursementMode = (DisbursementMode)dto.DisbursementMode;
            advance.DisbursementReference = string.IsNullOrWhiteSpace(dto.DisbursementReference) ? null : dto.DisbursementReference.Trim();
            advance.OutstandingAmount = dto.DisbursedAmount;
            advance.Status = AdvanceStatus.Disbursed;
            advance.ModifiedBy = actingUserId;
            advance.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<EmployeeAdvance>().Update(advance);

            // Flat installment split - equal amount per installment, last
            // one absorbs any rounding remainder (no interest, per
            // AdvanceType.IsInterestFree default).
            var firstDue = dto.FirstInstallmentDueDate == default ? advance.DisbursedOn.Value.AddMonths(1) : dto.FirstInstallmentDueDate;
            var perInstallment = Math.Round(dto.DisbursedAmount / advance.InstallmentCount, 2, MidpointRounding.AwayFromZero);
            var runningTotal = 0m;

            for (var i = 1; i <= advance.InstallmentCount; i++)
            {
                var amount = i == advance.InstallmentCount
                    ? dto.DisbursedAmount - runningTotal
                    : perInstallment;

                runningTotal += amount;

                await _uow.Repository<AdvanceInstallment>().AddAsync(new AdvanceInstallment
                {
                    EmployeeAdvanceId = advance.Id,
                    InstallmentNumber = i,
                    DueDate = firstDue.AddMonths(i - 1),
                    InstallmentAmount = amount,
                    Status = InstallmentStatus.Pending,
                    CreatedBy = actingUserId,
                    CreatedOn = DateTime.UtcNow
                });
            }

            await _uow.SaveChangesAsync();

            var firstInstallment = advance.InstallmentCount > 0 ? firstDue : (DateTime?)null;
            await NotifyApplicantAsync(advance, "Advance Disbursed",
                $"Your advance has been disbursed: {advance.DisbursedAmount:N0}. " +
                (firstInstallment.HasValue ? $"First installment of {perInstallment:N2} is due on {firstInstallment.Value:dd-MMM-yyyy}." : "The installment schedule has been generated."),
                "Success", tenantId, actingUserId);

            return await GetByIdInternalAsync(advance.Id, tenantId);
        }

        public async Task<EmployeeAdvanceDto> SettleAsync(AdvanceSettlementDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Approve);

            var advance = await GetEntityWithIncludesAsync(dto?.EmployeeAdvanceId, tenantId, forUpdate: true);

            if (advance.Status != AdvanceStatus.Disbursed)
                throw new BadRequestException("Only a Disbursed advance can be settled.");

            if (dto.AmountPaid <= 0)
                throw new BadRequestException("Amount Paid must be greater than zero.");

            await _uow.Repository<AdvancePaymentHistory>().AddAsync(new AdvancePaymentHistory
            {
                EmployeeAdvanceId = advance.Id,
                PaymentSource = PaymentSource.ManualReceipt,
                AmountPaid = dto.AmountPaid,
                PaymentDate = dto.PaymentDate == default ? DateTime.UtcNow : dto.PaymentDate,
                ReceiptReference = string.IsNullOrWhiteSpace(dto.ReceiptReference) ? null : dto.ReceiptReference.Trim(),
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim(),
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            var pendingInstallments = await _uow.Repository<AdvanceInstallment>().Query(asNoTracking: false)
                .Where(x => x.EmployeeAdvanceId == advance.Id && x.Status == InstallmentStatus.Pending)
                .ToListAsync();

            foreach (var installment in pendingInstallments)
            {
                installment.Status = InstallmentStatus.Waived;
                installment.ModifiedBy = actingUserId;
                installment.ModifiedOn = DateTime.UtcNow;
                _uow.Repository<AdvanceInstallment>().Update(installment);
            }

            advance.OutstandingAmount = Math.Max(0, advance.OutstandingAmount - dto.AmountPaid);

            if (advance.OutstandingAmount <= 0)
            {
                advance.Status = AdvanceStatus.Settled;
                advance.ClosedOn = DateTime.UtcNow;
            }

            advance.ModifiedBy = actingUserId;
            advance.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<EmployeeAdvance>().Update(advance);
            await _uow.SaveChangesAsync();

            await NotifyApplicantAsync(advance,
                advance.Status == AdvanceStatus.Settled ? "Advance Settled" : "Advance Payment Recorded",
                advance.Status == AdvanceStatus.Settled
                    ? $"Your advance has been fully settled and closed. Amount paid: {dto.AmountPaid:N2}."
                    : $"A payment of {dto.AmountPaid:N2} has been recorded against your advance. Remaining outstanding: {advance.OutstandingAmount:N2}.",
                "Success", tenantId, actingUserId);

            return await GetByIdInternalAsync(advance.Id, tenantId);
        }

        #endregion

        #region Notifications (Phase 15)

        // Mirrors EmployeeLoanService.NotifyLoanEventAsync exactly (see that
        // file's remarks) - best-effort in-app + email fan-out, never throws.
        private async Task NotifyAdvanceEventAsync(
            IEnumerable<string> userIds,
            string title,
            string message,
            string severity,
            string employeeAdvanceId,
            string? tenantId,
            string? actorUserId)
        {
            foreach (var userId in userIds.Where(x => !string.IsNullOrEmpty(x)).Distinct())
            {
                try
                {
                    await _notificationService.CreateDirectAsync(
                        userId,
                        title,
                        message,
                        severity,
                        redirectUrl: $"/EmployeeAdvance/Details/{employeeAdvanceId}",
                        featureId: AppFeatureConstants.EMPLOYEE_ADVANCE,
                        referenceId: employeeAdvanceId,
                        tenantId: tenantId,
                        createdBy: actorUserId ?? "System");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "In-app notification failed for advance {EmployeeAdvanceId}, user {UserId}.",
                        employeeAdvanceId, userId);
                }

                try
                {
                    var email = await _uow.Repository<User>().Query()
                        .Where(u => u.Id == userId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(email))
                        await _emailSender.SendAsync(email, title, message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Email notification failed for advance {EmployeeAdvanceId}, user {UserId}.",
                        employeeAdvanceId, userId);
                }
            }
        }

        private async Task NotifyApplicantAsync(EmployeeAdvance advance, string title, string message, string severity, string? tenantId, string? actorUserId)
        {
            try
            {
                var userId = await ResolveUserIdForEmployeeAsync(advance.EmployeeId);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation(
                        "Employee {EmployeeId} has no linked User account - skipping applicant notification for advance {EmployeeAdvanceId}.",
                        advance.EmployeeId, advance.Id);
                    return;
                }

                await NotifyAdvanceEventAsync(new[] { userId }, title, message, severity, advance.Id, tenantId, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Applicant notification resolution/dispatch failed for advance {EmployeeAdvanceId} - the advance transition itself is unaffected.",
                    advance.Id);
            }
        }

        private async Task<string?> ResolveUserIdForEmployeeAsync(string employeeId)
        {
            return await _uow.Repository<User>().Query()
                .Where(u => u.EmployeeId == employeeId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>Every User.Id holding the given action on AppFeatureConstants.EMPLOYEE_ADVANCE - used to notify "Finance" as a group.</summary>
        private async Task<List<string>> ResolveUsersWithPermissionAsync(string action)
        {
            return await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.EMPLOYEE_ADVANCE && x.Action == action)
                    on rp.PermissionId equals p.Id
                select ur.UserId
            ).Distinct().ToListAsync();
        }

        #endregion

        #region Authorization Helpers

        private static void EnsureCheckerIsNotMaker(EmployeeAdvance advance, string actingUserId)
        {
            if (!string.IsNullOrEmpty(actingUserId) && actingUserId == advance.MakerId)
                throw new UnauthorizedException(
                    "The approver must be a different person than whoever submitted this request - you cannot approve your own submission.");
        }

        private async Task EnsureIsApproverAsync(EmployeeAdvance advance, string actingUserId)
        {
            var managerUserId = await GetReportingManagerUserIdAsync(advance.EmployeeId);

            if (managerUserId == actingUserId)
                return;

            if (await HasPermissionAsync(actingUserId, Actions.Approve))
                return;

            throw new UnauthorizedException("You are not authorized to approve this advance request.");
        }

        private async Task<string?> GetReportingManagerUserIdAsync(string employeeId)
        {
            var employee = await _uow.Repository<Employee>().GetByIdAsync(employeeId);
            if (employee?.ReportingManagerId == null)
                return null;

            return await _uow.Repository<User>().Query()
                .Where(u => u.EmployeeId == employee.ReportingManagerId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        private async Task EnsureCanViewAsync(EmployeeAdvance advance, string actingUserId)
        {
            var ownEmployeeId = await GetEmployeeIdForUserAsync(actingUserId);
            if (!string.IsNullOrEmpty(ownEmployeeId) && ownEmployeeId == advance.EmployeeId)
                return;

            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedException("You are not authorized to view this advance request.");
        }

        private async Task EnsureSelfOrHrAsync(string employeeId, string actingUserId, string hrAction)
        {
            var ownEmployeeId = await GetEmployeeIdForUserAsync(actingUserId);
            if (!string.IsNullOrEmpty(ownEmployeeId) && ownEmployeeId == employeeId)
                return;

            if (!await HasPermissionAsync(actingUserId, hrAction))
                throw new UnauthorizedException("You are not authorized to act on behalf of this employee.");
        }

        private async Task<string?> GetEmployeeIdForUserAsync(string actingUserId)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return null;

            return await _uow.Repository<User>().Query()
                .Where(u => u.Id == actingUserId)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync();
        }

        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (!await HasPermissionAsync(actingUserId, action))
                throw new UnauthorizedException($"You are not authorized to {action} advance requests.");
        }

        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.EMPLOYEE_ADVANCE && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region Data Access / DTO Assembly

        private async Task<EmployeeAdvance> GetEntityWithIncludesAsync(string? id, string tenantId, bool forUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new BadRequestException("EmployeeAdvanceId is required.");

            var entity = await _uow.Repository<EmployeeAdvance>().Query(asNoTracking: !forUpdate)
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Include(x => x.AdvanceType)
                .Include(x => x.Company)
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new NotFoundException("Advance request not found.");

            return entity;
        }

        private async Task<EmployeeAdvanceDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityWithIncludesAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        private static EmployeeAdvanceListDto MapList(EmployeeAdvance x) => new()
        {
            Id = x.Id,
            EmployeeId = x.EmployeeId,
            EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
            EmployeeCode = x.Employee?.EmployeeCode,
            AdvanceTypeName = x.AdvanceType?.Name,
            RequestedAmount = x.RequestedAmount,
            ApprovedAmount = x.ApprovedAmount,
            OutstandingAmount = x.OutstandingAmount,
            Status = (int)x.Status,
            StatusName = x.Status.ToString(),
            CurrentApprovalLevel = x.CurrentApprovalLevel,
            DisbursedOn = x.DisbursedOn,
            CreatedOn = x.CreatedOn
        };

        private async Task<EmployeeAdvanceDto> AssembleDtoAsync(EmployeeAdvance x)
        {
            var approvalHistory = await _uow.Repository<AdvanceApprovalHistory>().Query()
                .Where(h => h.EmployeeAdvanceId == x.Id)
                .OrderBy(h => h.LevelNumber)
                .ToListAsync();

            var installments = await _uow.Repository<AdvanceInstallment>().Query()
                .Where(i => i.EmployeeAdvanceId == x.Id)
                .OrderBy(i => i.InstallmentNumber)
                .ToListAsync();

            var paymentHistory = await _uow.Repository<AdvancePaymentHistory>().Query()
                .Where(p => p.EmployeeAdvanceId == x.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var attachments = await _uow.Repository<LoanAdvanceAttachment>().Query()
                .Where(a => a.EntityType == LoanAttachmentEntityType.Advance && a.EntityId == x.Id)
                .ToListAsync();

            var userIds = approvalHistory
                .Select(h => h.CheckerId)
                .Append(x.MakerId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var nameMap = await BuildUserNameMapAsync(userIds);

            var nextPending = installments.FirstOrDefault(i => i.Status == InstallmentStatus.Pending);

            return new EmployeeAdvanceDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                CompanyId = x.CompanyId,
                CompanyName = x.Company?.Name,
                BranchId = x.BranchId,
                BranchName = x.Branch?.Name,
                EmployeeId = x.EmployeeId,
                EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
                EmployeeCode = x.Employee?.EmployeeCode,
                DepartmentName = x.Employee?.Department?.Name,
                DesignationName = x.Employee?.Designation?.Name,
                AdvanceTypeId = x.AdvanceTypeId,
                AdvanceTypeName = x.AdvanceType?.Name,
                RequestedAmount = x.RequestedAmount,
                ApprovedAmount = x.ApprovedAmount,
                InstallmentCount = x.InstallmentCount,
                Purpose = x.Purpose,
                Status = (int)x.Status,
                StatusName = x.Status.ToString(),
                CurrentApprovalLevel = x.CurrentApprovalLevel,
                TotalApprovalLevels = 1,
                MakerId = x.MakerId,
                MakerName = nameMap.GetValueOrDefault(x.MakerId),
                MakerActionOn = x.MakerActionOn,
                MakerRemarks = x.MakerRemarks,
                DisbursedAmount = x.DisbursedAmount,
                DisbursedOn = x.DisbursedOn,
                DisbursementMode = x.DisbursementMode.HasValue ? (int)x.DisbursementMode.Value : null,
                DisbursementModeName = x.DisbursementMode?.ToString(),
                DisbursementReference = x.DisbursementReference,
                OutstandingAmount = x.OutstandingAmount,
                TotalPaid = paymentHistory.Sum(p => p.AmountPaid),
                NextDueDate = nextPending?.DueDate,
                NextDueAmount = nextPending?.InstallmentAmount,
                ClosedOn = x.ClosedOn,
                ApprovalHistory = approvalHistory.Select(h => new AdvanceApprovalHistoryDto
                {
                    Id = h.Id,
                    EmployeeAdvanceId = h.EmployeeAdvanceId,
                    LevelNumber = h.LevelNumber,
                    CheckerId = h.CheckerId,
                    CheckerName = nameMap.GetValueOrDefault(h.CheckerId),
                    Decision = (int)h.Decision,
                    DecisionName = h.Decision.ToString(),
                    Remarks = h.Remarks,
                    ActionOn = h.ActionOn
                }).ToList(),
                Installments = installments.Select(i => new AdvanceInstallmentDto
                {
                    Id = i.Id,
                    EmployeeAdvanceId = i.EmployeeAdvanceId,
                    InstallmentNumber = i.InstallmentNumber,
                    DueDate = i.DueDate,
                    InstallmentAmount = i.InstallmentAmount,
                    Status = (int)i.Status,
                    StatusName = i.Status.ToString(),
                    RecoveredOn = i.RecoveredOn,
                    PayrollId = i.PayrollId
                }).ToList(),
                PaymentHistory = paymentHistory.Select(p => new AdvancePaymentHistoryDto
                {
                    Id = p.Id,
                    EmployeeAdvanceId = p.EmployeeAdvanceId,
                    AdvanceInstallmentId = p.AdvanceInstallmentId,
                    PaymentSource = (int)p.PaymentSource,
                    PaymentSourceName = p.PaymentSource.ToString(),
                    AmountPaid = p.AmountPaid,
                    PaymentDate = p.PaymentDate,
                    PayrollId = p.PayrollId,
                    ReceiptReference = p.ReceiptReference,
                    Remarks = p.Remarks
                }).ToList(),
                Attachments = attachments.Select(a => new LoanAdvanceAttachmentDto
                {
                    Id = a.Id,
                    EntityType = (int)a.EntityType,
                    EntityId = a.EntityId,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    ContentType = a.ContentType,
                    FileSizeBytes = a.FileSizeBytes,
                    UploadedBy = a.UploadedBy,
                    UploadedByName = nameMap.GetValueOrDefault(a.UploadedBy),
                    CreatedOn = a.CreatedOn
                }).ToList(),
                CreatedOn = x.CreatedOn,
                CreatedBy = x.CreatedBy
            };
        }

        private async Task<Dictionary<string, string>> BuildUserNameMapAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<string, string>();

            var users = await _uow.Repository<User>().Query()
                .Include(u => u.Employee)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            return users.ToDictionary(
                u => u.Id,
                u => u.Employee != null ? $"{u.Employee.FirstName} {u.Employee.LastName}".Trim() : u.Username);
        }

        #endregion
    }
}
