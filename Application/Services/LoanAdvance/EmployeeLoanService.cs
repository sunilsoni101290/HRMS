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
    /// <summary>See IEmployeeLoanService for the scope/architecture summary.</summary>
    public class EmployeeLoanService : IEmployeeLoanService
    {
        private readonly IUnitOfWork _uow;
        private readonly ILoanCalculationService _calc;
        private readonly ILoanPolicyService _policyService;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<EmployeeLoanService> _logger;

        public EmployeeLoanService(
            IUnitOfWork uow,
            ILoanCalculationService calc,
            ILoanPolicyService policyService,
            INotificationService notificationService,
            IEmailSender emailSender,
            ILogger<EmployeeLoanService> logger)
        {
            _uow = uow;
            _calc = calc;
            _policyService = policyService;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logger = logger;
        }

        #region Read

        public async Task<List<EmployeeLoanListDto>> GetAllAsync(string tenantId, string actingUserId, string? status = null, string? employeeId = null)
        {
            var isSelfOnly = !await HasPermissionAsync(actingUserId, Actions.View);
            var ownEmployeeId = await GetEmployeeIdForUserAsync(actingUserId);

            if (isSelfOnly && string.IsNullOrEmpty(ownEmployeeId))
                throw new UnauthorizedException("You are not authorized to view loan requests.");

            var query = _uow.Repository<EmployeeLoan>().Query()
                .Include(x => x.Employee)
                .Include(x => x.LoanType)
                .Where(x => x.TenantId == tenantId)
                .AsQueryable();

            if (isSelfOnly)
                query = query.Where(x => x.EmployeeId == ownEmployeeId);
            else if (!string.IsNullOrWhiteSpace(employeeId))
                query = query.Where(x => x.EmployeeId == employeeId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LoanStatus>(status, true, out var statusEnum))
                query = query.Where(x => x.Status == statusEnum);

            var entities = await query.OrderByDescending(x => x.CreatedOn).ToListAsync();

            return entities.Select(MapList).ToList();
        }

        public async Task<EmployeeLoanDto> GetByIdAsync(string id, string tenantId, string actingUserId)
        {
            var entity = await GetEntityWithIncludesAsync(id, tenantId);

            await EnsureCanViewAsync(entity, actingUserId);

            return await AssembleDtoAsync(entity);
        }

        /// <summary>
        /// PHASE 18 - batch-resolves approvers for every candidate loan in a
        /// small, fixed number of queries instead of the original
        /// per-loan N+1 (ResolveLevelApproverUserIdsAsync did 2-5 round
        /// trips PER pending loan). Must produce the exact same result as
        /// that per-loan resolution - see ResolvePrimaryApproverUserIdsAsync/
        /// ResolveActiveDelegateUserIdAsync for the logic being mirrored in
        /// bulk form here. Those single-loan helpers are left untouched and
        /// still used by ApproveAsync/RejectAsync/ResolveCurrentApproverUserIdsAsync,
        /// where only one loan is ever resolved per call so N+1 doesn't apply.
        /// </summary>
        public async Task<List<EmployeeLoanListDto>> GetPendingOnMeAsync(string tenantId, string actingUserId)
        {
            var candidates = await _uow.Repository<EmployeeLoan>().Query()
                .Include(x => x.Employee)
                .Include(x => x.LoanType)
                .Include(x => x.LoanPolicy).ThenInclude(p => p.ApprovalLevels)
                .Where(x => x.TenantId == tenantId && x.Status == LoanStatus.PendingApproval && x.MakerId != actingUserId)
                .ToListAsync();

            if (candidates.Count == 0)
                return new List<EmployeeLoanListDto>();

            var loanLevels = candidates
                .Select(loan => (Loan: loan, Level: loan.LoanPolicy?.ApprovalLevels?.FirstOrDefault(l => l.LevelNumber == loan.CurrentApprovalLevel)))
                .Where(x => x.Level != null)
                .ToList();

            if (loanLevels.Count == 0)
                return new List<EmployeeLoanListDto>();

            // ---- Batch 1: ReportingManager levels - one query for every distinct manager's User.Id. ----
            var reportingManagerEmployeeIds = loanLevels
                .Where(x => x.Level!.ApproverType == ApproverType.ReportingManager)
                .Select(x => x.Loan.Employee?.ReportingManagerId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var managerUserIdMap = reportingManagerEmployeeIds.Count == 0
                ? new Dictionary<string, string>()
                : await _uow.Repository<User>().Query()
                    .Where(u => reportingManagerEmployeeIds.Contains(u.EmployeeId))
                    .ToDictionaryAsync(u => u.EmployeeId, u => u.Id);

            // ---- Batch 2: SpecificRole levels - one query for every distinct role's User.Ids. ----
            var roleIds = loanLevels
                .Where(x => x.Level!.ApproverType == ApproverType.SpecificRole)
                .Select(x => x.Level!.ApproverRoleId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            var roleUserIdsMap = roleIds.Count == 0
                ? new Dictionary<string, List<string>>()
                : (await _uow.Repository<UserRole>().Query()
                    .Where(ur => roleIds.Contains(ur.RoleId))
                    .Select(ur => new { ur.RoleId, ur.UserId })
                    .ToListAsync())
                    .GroupBy(x => x.RoleId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToList());

            // SpecificUser levels need no query - level.ApproverUserId is already loaded.

            var primariesByLoan = new Dictionary<string, HashSet<string>>();
            foreach (var x in loanLevels)
            {
                var primaries = new HashSet<string>();

                switch (x.Level!.ApproverType)
                {
                    case ApproverType.ReportingManager:
                        var mgrEmpId = x.Loan.Employee?.ReportingManagerId;
                        if (mgrEmpId != null && managerUserIdMap.TryGetValue(mgrEmpId, out var mgrUserId))
                            primaries.Add(mgrUserId);
                        break;

                    case ApproverType.SpecificRole:
                        if (x.Level.ApproverRoleId != null && roleUserIdsMap.TryGetValue(x.Level.ApproverRoleId, out var roleUsers))
                            foreach (var u in roleUsers) primaries.Add(u);
                        break;

                    case ApproverType.SpecificUser:
                        if (!string.IsNullOrEmpty(x.Level.ApproverUserId))
                            primaries.Add(x.Level.ApproverUserId);
                        break;
                }

                primariesByLoan[x.Loan.Id] = primaries;
            }

            // ---- Batch 3: active-delegate resolution - does actingUserId currently stand in for ANY primary approver above? ----
            var allPrimaryUserIds = primariesByLoan.Values.SelectMany(s => s).Distinct().ToList();
            var actingIsDelegateForPrimary = new HashSet<string>();

            if (allPrimaryUserIds.Count > 0)
            {
                var primaryEmployeeMap = await _uow.Repository<User>().Query()
                    .Where(u => allPrimaryUserIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.EmployeeId);

                var actingEmployeeId = await GetEmployeeIdForUserAsync(actingUserId);

                if (!string.IsNullOrEmpty(actingEmployeeId))
                {
                    var today = DateTime.UtcNow.Date;
                    var delegatorEmployeeIds = primaryEmployeeMap.Values.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

                    var activeDelegatorEmployeeIds = delegatorEmployeeIds.Count == 0
                        ? new HashSet<string>()
                        : (await _uow.Repository<ApprovalDelegation>().Query()
                            .Where(d => delegatorEmployeeIds.Contains(d.DelegatorEmployeeId) && d.DelegateEmployeeId == actingEmployeeId &&
                                d.IsActive && d.StartDate <= today && d.EndDate >= today)
                            .Select(d => d.DelegatorEmployeeId)
                            .ToListAsync())
                            .ToHashSet();

                    foreach (var kvp in primaryEmployeeMap)
                        if (activeDelegatorEmployeeIds.Contains(kvp.Value))
                            actingIsDelegateForPrimary.Add(kvp.Key);
                }
            }

            var result = new List<EmployeeLoanListDto>();
            foreach (var x in loanLevels)
            {
                var primaries = primariesByLoan[x.Loan.Id];
                if (primaries.Contains(actingUserId) || primaries.Any(p => actingIsDelegateForPrimary.Contains(p)))
                    result.Add(MapList(x.Loan));
            }

            return result.OrderByDescending(x => x.CreatedOn).ToList();
        }

        /// <summary>See IEmployeeLoanService for the reminder-sweep rationale.</summary>
        public async Task<List<string>> ResolveCurrentApproverUserIdsAsync(string employeeLoanId)
        {
            var loan = await _uow.Repository<EmployeeLoan>().Query()
                .Include(x => x.Employee)
                .Include(x => x.LoanPolicy).ThenInclude(p => p.ApprovalLevels)
                .FirstOrDefaultAsync(x => x.Id == employeeLoanId);

            if (loan == null || loan.Status != LoanStatus.PendingApproval)
                return new List<string>();

            var level = loan.LoanPolicy?.ApprovalLevels?.FirstOrDefault(l => l.LevelNumber == loan.CurrentApprovalLevel);
            if (level == null)
                return new List<string>();

            var approvers = await ResolveLevelApproverUserIdsAsync(level, loan);
            return approvers.ToList();
        }

        public async Task<LoanEligibilityDto> CheckEligibilityAsync(string employeeId, string loanTypeId, decimal requestedAmount, int tenureMonths, string tenantId, string actingUserId)
        {
            await EnsureSelfOrHrAsync(employeeId, actingUserId, Actions.Create);
            return await _calc.CheckEligibilityAsync(employeeId, loanTypeId, requestedAmount, tenureMonths, tenantId);
        }

        public async Task<EmiPreviewResponseDto> PreviewEmiAsync(EmiPreviewRequestDto dto, string tenantId, string actingUserId)
        {
            if (dto == null) throw new BadRequestException("Request body is required.");
            if (string.IsNullOrWhiteSpace(dto.LoanTypeId)) throw new BadRequestException("LoanTypeId is required.");
            if (dto.Amount <= 0) throw new BadRequestException("Amount must be greater than zero.");
            if (dto.TenureMonths <= 0) throw new BadRequestException("Tenure Months must be greater than zero.");

            var loanType = await _uow.Repository<LoanType>().Query()
                .FirstOrDefaultAsync(x => x.Id == dto.LoanTypeId && x.TenantId == tenantId && !x.IsDeleted);

            if (loanType == null)
                throw new NotFoundException("Loan Type not found.");

            // Best-effort Company/Branch scoping via the caller's own linked
            // Employee (self-service preview); falls back to a tenant-wide
            // policy match when the caller has none (e.g. an HR user
            // previewing before picking an employee).
            var ownEmployeeId = await GetEmployeeIdForUserAsync(actingUserId);
            Employee? ownEmployee = ownEmployeeId != null
                ? await _uow.Repository<Employee>().GetByIdAsync(ownEmployeeId)
                : null;

            var policy = await _policyService.GetActivePolicyAsync(dto.LoanTypeId, tenantId, ownEmployee?.CompanyId, ownEmployee?.BranchId);

            var syntheticLoan = new EmployeeLoan
            {
                Id = "PREVIEW",
                RequestedAmount = dto.Amount,
                ApprovedAmount = dto.Amount,
                TenureMonths = dto.TenureMonths,
                InterestRatePercent = policy?.InterestRatePercent ?? loanType.DefaultInterestRatePercent,
                InterestMethod = loanType.InterestMethod
            };

            var schedule = _calc.GenerateEmiSchedule(syntheticLoan, DateTime.UtcNow.AddMonths(1));

            return new EmiPreviewResponseDto
            {
                MonthlyEmi = schedule.FirstOrDefault()?.EmiAmount ?? 0,
                TotalInterest = schedule.Sum(x => x.InterestComponent),
                TotalPayable = schedule.Sum(x => x.EmiAmount),
                Schedule = schedule.Select(e => new LoanEmiScheduleDto
                {
                    InstallmentNumber = e.InstallmentNumber,
                    DueDate = e.DueDate,
                    OpeningBalance = e.OpeningBalance,
                    PrincipalComponent = e.PrincipalComponent,
                    InterestComponent = e.InterestComponent,
                    EmiAmount = e.EmiAmount,
                    ClosingBalance = e.ClosingBalance,
                    Status = (int)e.Status,
                    StatusName = e.Status.ToString()
                }).ToList()
            };
        }

        #endregion

        #region Maker: Submit

        public async Task<EmployeeLoanDto> SubmitAsync(LoanSubmitDto dto, string tenantId, string actingUserId)
        {
            if (dto == null) throw new BadRequestException("Request body is required.");
            if (string.IsNullOrWhiteSpace(dto.EmployeeId)) throw new BadRequestException("EmployeeId is required.");
            if (string.IsNullOrWhiteSpace(dto.LoanTypeId)) throw new BadRequestException("LoanTypeId is required.");
            if (dto.RequestedAmount <= 0) throw new BadRequestException("Requested Amount must be greater than zero.");
            if (dto.TenureMonths <= 0) throw new BadRequestException("Tenure Months must be greater than zero.");

            await EnsureSelfOrHrAsync(dto.EmployeeId, actingUserId, Actions.Create);

            var employee = await _uow.Repository<Employee>().Query()
                .FirstOrDefaultAsync(x => x.Id == dto.EmployeeId && x.TenantId == tenantId && !x.IsDeleted);

            if (employee == null)
                throw new NotFoundException("Employee not found.");

            var loanType = await _uow.Repository<LoanType>().Query()
                .FirstOrDefaultAsync(x => x.Id == dto.LoanTypeId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive);

            if (loanType == null)
                throw new NotFoundException("Loan Type not found or inactive.");

            var eligibility = await _calc.CheckEligibilityAsync(dto.EmployeeId, dto.LoanTypeId, dto.RequestedAmount, dto.TenureMonths, tenantId);

            if (!eligibility.IsEligible)
                throw new BadRequestException(eligibility.ReasonIfNotEligible ?? "You are not eligible for this loan.");

            var policy = await _policyService.GetActivePolicyAsync(dto.LoanTypeId, tenantId, employee.CompanyId, employee.BranchId);

            if (policy == null)
                throw new BadRequestException("No active Loan Policy is configured for this Loan Type.");

            var entity = new EmployeeLoan
            {
                TenantId = tenantId,
                CompanyId = employee.CompanyId,
                BranchId = employee.BranchId,
                EmployeeId = dto.EmployeeId,
                LoanTypeId = dto.LoanTypeId,
                LoanPolicyId = policy.Id!,
                RequestedAmount = dto.RequestedAmount,
                TenureMonths = dto.TenureMonths,
                InterestRatePercent = policy.InterestRatePercent ?? loanType.DefaultInterestRatePercent,
                InterestMethod = loanType.InterestMethod,
                Purpose = string.IsNullOrWhiteSpace(dto.Purpose) ? null : dto.Purpose.Trim(),
                Status = LoanStatus.Submitted,
                MakerId = actingUserId,
                MakerActionOn = DateTime.UtcNow,
                OutstandingPrincipal = 0,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            };

            await _uow.Repository<EmployeeLoan>().AddAsync(entity);
            await _uow.SaveChangesAsync(); // need entity.Id for the level resolution below

            var firstLevel = await ResolveFirstApplicableLevelAsync(entity.Id, tenantId);

            entity.CurrentApprovalLevel = firstLevel?.LevelNumber ?? 0;
            entity.Status = firstLevel != null ? LoanStatus.PendingApproval : LoanStatus.Approved;

            _uow.Repository<EmployeeLoan>().Update(entity);
            await _uow.SaveChangesAsync();

            if (entity.Status == LoanStatus.PendingApproval && firstLevel != null)
            {
                var approvers = await ResolveLevelApproverUserIdsAsync(firstLevel, entity);
                await NotifyLoanEventAsync(approvers,
                    "New Loan Request Awaiting Your Approval",
                    $"A loan request of {entity.RequestedAmount:N0} from {employee.FirstName} {employee.LastName} is awaiting your approval.",
                    "Info", entity.Id, tenantId, actingUserId);
            }
            else if (entity.Status == LoanStatus.Approved)
            {
                // No applicable approval level (below every threshold) -
                // straight to Approved; let the applicant AND Finance know.
                await NotifyApplicantAsync(entity, "Loan Request Approved",
                    $"Your loan request of {entity.RequestedAmount:N0} has been approved and is awaiting disbursement.",
                    "Success", tenantId, actingUserId);

                var financeUsers = await ResolveUsersWithPermissionAsync(Actions.Approve);
                await NotifyLoanEventAsync(financeUsers,
                    "Loan Ready for Disbursement",
                    $"{employee.FirstName} {employee.LastName}'s loan of {entity.RequestedAmount:N0} is approved and ready for disbursement.",
                    "Info", entity.Id, tenantId, actingUserId);
            }

            return await GetByIdInternalAsync(entity.Id, tenantId);
        }

        #endregion

        #region Checker: Approve / Reject

        public async Task<EmployeeLoanDto> ApproveAsync(LoanApprovalActionDto dto, string tenantId, string actingUserId)
        {
            var loan = await GetEntityWithIncludesAsync(dto?.EmployeeLoanId, tenantId, forUpdate: true);

            EnsureCheckerIsNotMaker(loan, actingUserId);

            var level = loan.LoanPolicy?.ApprovalLevels?.FirstOrDefault(l => l.LevelNumber == loan.CurrentApprovalLevel);
            if (level == null || loan.Status != LoanStatus.PendingApproval)
                throw new BadRequestException("This loan is not currently awaiting approval.");

            var approvers = await ResolveLevelApproverUserIdsAsync(level, loan);
            if (!approvers.Contains(actingUserId))
                throw new UnauthorizedException("You are not the assigned approver for this loan's current level.");

            var deputyFor = await ResolveDeputyForAsync(level, loan, actingUserId);

            var history = new LoanApprovalHistory
            {
                EmployeeLoanId = loan.Id,
                LevelNumber = level.LevelNumber,
                CheckerId = actingUserId,
                ActedAsDelegateForUserId = deputyFor,
                Decision = ApprovalDecision.Approved,
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim(),
                ActionOn = DateTime.UtcNow,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            };
            await _uow.Repository<LoanApprovalHistory>().AddAsync(history);

            var nextLevel = loan.LoanPolicy!.ApprovalLevels
                .Where(l => l.LevelNumber > level.LevelNumber && l.MinAmountThreshold <= loan.RequestedAmount)
                .OrderBy(l => l.LevelNumber)
                .FirstOrDefault();

            if (nextLevel != null)
            {
                loan.CurrentApprovalLevel = nextLevel.LevelNumber;
                // Status stays PendingApproval.
            }
            else
            {
                loan.Status = LoanStatus.Approved;
                loan.ApprovedAmount = dto.ApprovedAmount ?? loan.RequestedAmount;
            }

            loan.ModifiedBy = actingUserId;
            loan.ModifiedOn = DateTime.UtcNow;
            _uow.Repository<EmployeeLoan>().Update(loan);

            await _uow.SaveChangesAsync();

            if (nextLevel != null)
            {
                var nextApprovers = await ResolveLevelApproverUserIdsAsync(nextLevel, loan);
                await NotifyLoanEventAsync(nextApprovers,
                    "Loan Request Awaiting Your Approval",
                    $"A loan request of {loan.RequestedAmount:N0} from {loan.Employee?.FirstName} {loan.Employee?.LastName} has reached your approval level.",
                    "Info", loan.Id, tenantId, actingUserId);
            }
            else
            {
                await NotifyApplicantAsync(loan, "Loan Request Approved",
                    $"Your loan request of {loan.RequestedAmount:N0} has been fully approved and is awaiting disbursement.",
                    "Success", tenantId, actingUserId);

                var financeUsers = await ResolveUsersWithPermissionAsync(Actions.Approve);
                await NotifyLoanEventAsync(financeUsers,
                    "Loan Ready for Disbursement",
                    $"{loan.Employee?.FirstName} {loan.Employee?.LastName}'s loan of {loan.RequestedAmount:N0} is fully approved and ready for disbursement.",
                    "Info", loan.Id, tenantId, actingUserId);
            }

            return await GetByIdInternalAsync(loan.Id, tenantId);
        }

        public async Task<EmployeeLoanDto> RejectAsync(LoanApprovalActionDto dto, string tenantId, string actingUserId)
        {
            var loan = await GetEntityWithIncludesAsync(dto?.EmployeeLoanId, tenantId, forUpdate: true);

            EnsureCheckerIsNotMaker(loan, actingUserId);

            var level = loan.LoanPolicy?.ApprovalLevels?.FirstOrDefault(l => l.LevelNumber == loan.CurrentApprovalLevel);
            if (level == null || loan.Status != LoanStatus.PendingApproval)
                throw new BadRequestException("This loan is not currently awaiting approval.");

            var approvers = await ResolveLevelApproverUserIdsAsync(level, loan);
            if (!approvers.Contains(actingUserId))
                throw new UnauthorizedException("You are not the assigned approver for this loan's current level.");

            if (string.IsNullOrWhiteSpace(dto.Remarks))
                throw new BadRequestException("Remarks are required to reject a loan request.");

            var deputyFor = await ResolveDeputyForAsync(level, loan, actingUserId);

            await _uow.Repository<LoanApprovalHistory>().AddAsync(new LoanApprovalHistory
            {
                EmployeeLoanId = loan.Id,
                LevelNumber = level.LevelNumber,
                CheckerId = actingUserId,
                ActedAsDelegateForUserId = deputyFor,
                Decision = ApprovalDecision.Rejected,
                Remarks = dto.Remarks.Trim(),
                ActionOn = DateTime.UtcNow,
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            loan.Status = LoanStatus.Rejected;
            loan.ModifiedBy = actingUserId;
            loan.ModifiedOn = DateTime.UtcNow;
            _uow.Repository<EmployeeLoan>().Update(loan);

            await _uow.SaveChangesAsync();

            await NotifyApplicantAsync(loan, "Loan Request Rejected",
                $"Your loan request of {loan.RequestedAmount:N0} was rejected. Reason: {dto.Remarks.Trim()}",
                "Error", tenantId, actingUserId);

            return await GetByIdInternalAsync(loan.Id, tenantId);
        }

        #endregion

        #region Finance: Disburse / Pre-Closure / Settle

        public async Task<EmployeeLoanDto> DisburseAsync(LoanDisbursementDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Approve); // "Finance" = Approve permission on this feature, see AppFeatureConstants.EMPLOYEE_LOAN

            var loan = await GetEntityWithIncludesAsync(dto?.EmployeeLoanId, tenantId, forUpdate: true);

            if (loan.Status != LoanStatus.Approved)
                throw new BadRequestException("Only an Approved loan can be disbursed.");

            if (dto.DisbursedAmount <= 0)
                throw new BadRequestException("Disbursed Amount must be greater than zero.");

            loan.DisbursedAmount = dto.DisbursedAmount;
            loan.ApprovedAmount ??= dto.DisbursedAmount;
            loan.DisbursedOn = dto.DisbursedOn == default ? DateTime.UtcNow : dto.DisbursedOn;
            loan.DisbursementMode = (DisbursementMode)dto.DisbursementMode;
            loan.DisbursementReference = string.IsNullOrWhiteSpace(dto.DisbursementReference) ? null : dto.DisbursementReference.Trim();
            loan.OutstandingPrincipal = dto.DisbursedAmount;
            loan.Status = LoanStatus.Active; // Disbursed -> EMI schedule generated -> Active, in one step (see Phase 2 state machine)
            loan.ModifiedBy = actingUserId;
            loan.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<EmployeeLoan>().Update(loan);

            var schedule = _calc.GenerateEmiSchedule(loan, dto.FirstEmiDueDate == default ? loan.DisbursedOn.Value.AddMonths(1) : dto.FirstEmiDueDate);

            foreach (var installment in schedule)
                await _uow.Repository<LoanEmiSchedule>().AddAsync(installment);

            await _uow.SaveChangesAsync();

            var firstEmi = schedule.OrderBy(e => e.DueDate).FirstOrDefault();
            await NotifyApplicantAsync(loan, "Loan Disbursed",
                $"Your loan has been disbursed: {loan.DisbursedAmount:N0}. " +
                (firstEmi != null ? $"First EMI of {firstEmi.EmiAmount:N2} is due on {firstEmi.DueDate:dd-MMM-yyyy}." : "The EMI schedule has been generated."),
                "Success", tenantId, actingUserId);

            return await GetByIdInternalAsync(loan.Id, tenantId);
        }

        public async Task<LoanPreClosureQuoteDto> GetPreClosureQuoteAsync(string employeeLoanId, string tenantId, string actingUserId)
        {
            var loan = await GetEntityWithIncludesAsync(employeeLoanId, tenantId);
            await EnsureCanViewAsync(loan, actingUserId);

            return await _calc.GetPreClosureQuoteAsync(employeeLoanId, DateTime.UtcNow, tenantId);
        }

        public async Task<EmployeeLoanDto> RequestPreClosureAsync(string employeeLoanId, string tenantId, string actingUserId)
        {
            var loan = await GetEntityWithIncludesAsync(employeeLoanId, tenantId, forUpdate: true);
            await EnsureCanViewAsync(loan, actingUserId); // employee themselves, or HR/Finance

            if (loan.Status != LoanStatus.Active)
                throw new BadRequestException("Only an Active loan can request pre-closure.");

            loan.Status = LoanStatus.PreClosureRequested;
            loan.ModifiedBy = actingUserId;
            loan.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<EmployeeLoan>().Update(loan);
            await _uow.SaveChangesAsync();

            var financeUsers = await ResolveUsersWithPermissionAsync(Actions.Approve);
            await NotifyLoanEventAsync(financeUsers,
                "Loan Pre-Closure Requested",
                $"{loan.Employee?.FirstName} {loan.Employee?.LastName} has requested pre-closure of their loan (outstanding: {loan.OutstandingPrincipal:N0}). Please review the pre-closure quote.",
                "Info", loan.Id, tenantId, actingUserId);

            return await GetByIdInternalAsync(loan.Id, tenantId);
        }

        public async Task<EmployeeLoanDto> SettleAsync(LoanSettlementDto dto, string tenantId, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Approve); // Finance-only

            var loan = await GetEntityWithIncludesAsync(dto?.EmployeeLoanId, tenantId, forUpdate: true);

            if (loan.Status != LoanStatus.PreClosureRequested && loan.Status != LoanStatus.SettlementPending && loan.Status != LoanStatus.Active)
                throw new BadRequestException("This loan is not in a state that can be settled.");

            if (dto.AmountPaid <= 0)
                throw new BadRequestException("Amount Paid must be greater than zero.");

            var principalPaid = Math.Min(dto.AmountPaid, loan.OutstandingPrincipal);
            var interestPaid = Math.Max(0, dto.AmountPaid - principalPaid);

            await _uow.Repository<LoanPaymentHistory>().AddAsync(new LoanPaymentHistory
            {
                EmployeeLoanId = loan.Id,
                PaymentSource = PaymentSource.PreClosure,
                AmountPaid = dto.AmountPaid,
                PrincipalPaid = principalPaid,
                InterestPaid = interestPaid,
                PaymentDate = dto.PaymentDate == default ? DateTime.UtcNow : dto.PaymentDate,
                ReceiptReference = string.IsNullOrWhiteSpace(dto.ReceiptReference) ? null : dto.ReceiptReference.Trim(),
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim(),
                CreatedBy = actingUserId,
                CreatedOn = DateTime.UtcNow
            });

            // Cancel any remaining Pending EMIs - a settled/pre-closed loan
            // has no further scheduled recoveries.
            var pendingEmis = await _uow.Repository<LoanEmiSchedule>().Query(asNoTracking: false)
                .Where(x => x.EmployeeLoanId == loan.Id && x.Status == InstallmentStatus.Pending)
                .ToListAsync();

            foreach (var emi in pendingEmis)
            {
                emi.Status = InstallmentStatus.Cancelled;
                emi.ModifiedBy = actingUserId;
                emi.ModifiedOn = DateTime.UtcNow;
                _uow.Repository<LoanEmiSchedule>().Update(emi);
            }

            loan.OutstandingPrincipal = 0;
            loan.ClosedOn = DateTime.UtcNow;
            loan.ClosureReason = (LoanClosureReason)dto.ClosureReason;
            loan.Status = LoanStatus.Closed;
            loan.ModifiedBy = actingUserId;
            loan.ModifiedOn = DateTime.UtcNow;

            _uow.Repository<EmployeeLoan>().Update(loan);
            await _uow.SaveChangesAsync();

            await NotifyApplicantAsync(loan, "Loan Settled",
                $"Your loan has been settled and closed. Amount paid: {dto.AmountPaid:N2}.",
                "Success", tenantId, actingUserId);

            return await GetByIdInternalAsync(loan.Id, tenantId);
        }

        #endregion

        #region Approval Matrix Resolution

        private async Task<LoanPolicyApprovalLevel?> ResolveFirstApplicableLevelAsync(string employeeLoanId, string tenantId)
        {
            var loan = await _uow.Repository<EmployeeLoan>().Query()
                .Include(x => x.LoanPolicy).ThenInclude(p => p.ApprovalLevels)
                .FirstOrDefaultAsync(x => x.Id == employeeLoanId && x.TenantId == tenantId);

            return loan?.LoanPolicy?.ApprovalLevels?
                .Where(l => l.MinAmountThreshold <= loan.RequestedAmount)
                .OrderBy(l => l.LevelNumber)
                .FirstOrDefault();
        }

        /// <summary>Primary (non-delegate) approver User.Ids for a single approval level, per its ApproverType.</summary>
        private async Task<HashSet<string>> ResolveLevelApproverUserIdsAsync(LoanPolicyApprovalLevel level, EmployeeLoan loan)
        {
            var primary = await ResolvePrimaryApproverUserIdsAsync(level, loan);
            var withDelegates = new HashSet<string>(primary);

            foreach (var userId in primary)
            {
                var delegateUserId = await ResolveActiveDelegateUserIdAsync(userId);
                if (delegateUserId != null)
                    withDelegates.Add(delegateUserId);
            }

            return withDelegates;
        }

        private async Task<HashSet<string>> ResolvePrimaryApproverUserIdsAsync(LoanPolicyApprovalLevel level, EmployeeLoan loan)
        {
            switch (level.ApproverType)
            {
                case ApproverType.ReportingManager:
                    var employee = loan.Employee ?? await _uow.Repository<Employee>().GetByIdAsync(loan.EmployeeId);
                    if (employee?.ReportingManagerId == null)
                        return new HashSet<string>();

                    var managerUserId = await _uow.Repository<User>().Query()
                        .Where(u => u.EmployeeId == employee.ReportingManagerId)
                        .Select(u => u.Id)
                        .FirstOrDefaultAsync();

                    return managerUserId == null ? new HashSet<string>() : new HashSet<string> { managerUserId };

                case ApproverType.SpecificRole:
                    var roleUserIds = await _uow.Repository<UserRole>().Query()
                        .Where(ur => ur.RoleId == level.ApproverRoleId)
                        .Select(ur => ur.UserId)
                        .ToListAsync();
                    return new HashSet<string>(roleUserIds);

                case ApproverType.SpecificUser:
                    return string.IsNullOrEmpty(level.ApproverUserId)
                        ? new HashSet<string>()
                        : new HashSet<string> { level.ApproverUserId };

                default:
                    return new HashSet<string>();
            }
        }

        /// <summary>
        /// If the primary approver (by their linked Employee) has an
        /// active ApprovalDelegation as Delegator, returns the delegate's
        /// User.Id so they can also act - mirrors the existing Leave
        /// module's ApprovalDelegation usage.
        /// </summary>
        private async Task<string?> ResolveActiveDelegateUserIdAsync(string primaryUserId)
        {
            var employeeId = await _uow.Repository<User>().Query()
                .Where(u => u.Id == primaryUserId)
                .Select(u => u.EmployeeId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(employeeId))
                return null;

            var today = DateTime.UtcNow.Date;

            var delegateEmployeeId = await _uow.Repository<ApprovalDelegation>().Query()
                .Where(d => d.DelegatorEmployeeId == employeeId && d.IsActive &&
                    d.StartDate <= today && d.EndDate >= today)
                .Select(d => d.DelegateEmployeeId)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(delegateEmployeeId))
                return null;

            return await _uow.Repository<User>().Query()
                .Where(u => u.EmployeeId == delegateEmployeeId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>If actingUserId acted as someone else's delegate for this level, returns that primary approver's User.Id for the audit trail; otherwise null.</summary>
        private async Task<string?> ResolveDeputyForAsync(LoanPolicyApprovalLevel level, EmployeeLoan loan, string actingUserId)
        {
            var primaries = await ResolvePrimaryApproverUserIdsAsync(level, loan);
            if (primaries.Contains(actingUserId))
                return null;

            foreach (var primaryUserId in primaries)
            {
                var delegateUserId = await ResolveActiveDelegateUserIdAsync(primaryUserId);
                if (delegateUserId == actingUserId)
                    return primaryUserId;
            }

            return null;
        }

        #endregion

        #region Notifications (Phase 15)

        // Best-effort fan-out: in-app Notification (must actually work) then,
        // independently, a best-effort email - mirrors
        // LeaveApplicationService.NotifyLeaveEventAsync's convention exactly.
        // Every failure is caught and logged here, never rethrown, so a
        // notification problem can never make an already-committed loan
        // transition (Submit/Approve/Reject/Disburse/...) appear to fail.
        private async Task NotifyLoanEventAsync(
            IEnumerable<string> userIds,
            string title,
            string message,
            string severity,
            string employeeLoanId,
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
                        redirectUrl: $"/EmployeeLoan/Details/{employeeLoanId}",
                        featureId: AppFeatureConstants.EMPLOYEE_LOAN,
                        referenceId: employeeLoanId,
                        tenantId: tenantId,
                        createdBy: actorUserId ?? "System");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "In-app notification failed for loan {EmployeeLoanId}, user {UserId}.",
                        employeeLoanId, userId);
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
                        "Email notification failed for loan {EmployeeLoanId}, user {UserId}.",
                        employeeLoanId, userId);
                }
            }
        }

        /// <summary>Resolves the loan's applicant User.Id and notifies them - a no-op (logged) if the employee has no linked User account.</summary>
        private async Task NotifyApplicantAsync(EmployeeLoan loan, string title, string message, string severity, string? tenantId, string? actorUserId)
        {
            try
            {
                var userId = await ResolveUserIdForEmployeeAsync(loan.EmployeeId);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation(
                        "Employee {EmployeeId} has no linked User account - skipping applicant notification for loan {EmployeeLoanId}.",
                        loan.EmployeeId, loan.Id);
                    return;
                }

                await NotifyLoanEventAsync(new[] { userId }, title, message, severity, loan.Id, tenantId, actorUserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Applicant notification resolution/dispatch failed for loan {EmployeeLoanId} - the loan transition itself is unaffected.",
                    loan.Id);
            }
        }

        private async Task<string?> ResolveUserIdForEmployeeAsync(string employeeId)
        {
            return await _uow.Repository<User>().Query()
                .Where(u => u.EmployeeId == employeeId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>Every User.Id holding the given action on AppFeatureConstants.EMPLOYEE_LOAN - used to notify "Finance" (Approve permission) as a group rather than one fixed user.</summary>
        private async Task<List<string>> ResolveUsersWithPermissionAsync(string action)
        {
            return await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.EMPLOYEE_LOAN && x.Action == action)
                    on rp.PermissionId equals p.Id
                select ur.UserId
            ).Distinct().ToListAsync();
        }

        #endregion

        #region Authorization Helpers

        private static void EnsureCheckerIsNotMaker(EmployeeLoan loan, string actingUserId)
        {
            if (!string.IsNullOrEmpty(actingUserId) && actingUserId == loan.MakerId)
                throw new UnauthorizedException(
                    "The approver must be a different person than whoever submitted this request - you cannot approve your own submission.");
        }

        private async Task EnsureCanViewAsync(EmployeeLoan loan, string actingUserId)
        {
            var ownEmployeeId = await GetEmployeeIdForUserAsync(actingUserId);
            if (!string.IsNullOrEmpty(ownEmployeeId) && ownEmployeeId == loan.EmployeeId)
                return;

            if (!await HasPermissionAsync(actingUserId, Actions.View))
                throw new UnauthorizedException("You are not authorized to view this loan request.");
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
                throw new UnauthorizedException($"You are not authorized to {action} loan requests.");
        }

        private async Task<bool> HasPermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                return false;

            return await (
                from ur in _uow.Repository<UserRole>().Query()
                join rp in _uow.Repository<RolePermission>().Query().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _uow.Repository<Permission>().Query().Where(x =>
                        x.FeatureId == AppFeatureConstants.EMPLOYEE_LOAN && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();
        }

        #endregion

        #region Data Access / DTO Assembly

        private async Task<EmployeeLoan> GetEntityWithIncludesAsync(string? id, string tenantId, bool forUpdate = false)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new BadRequestException("EmployeeLoanId is required.");

            var query = _uow.Repository<EmployeeLoan>().Query(asNoTracking: !forUpdate)
                .Include(x => x.Employee).ThenInclude(e => e.Department)
                .Include(x => x.Employee).ThenInclude(e => e.Designation)
                .Include(x => x.LoanType)
                .Include(x => x.LoanPolicy).ThenInclude(p => p.ApprovalLevels)
                .Include(x => x.Company)
                .Include(x => x.Branch);

            var entity = await query.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted);

            if (entity == null)
                throw new NotFoundException("Loan request not found.");

            return entity;
        }

        private async Task<EmployeeLoanDto> GetByIdInternalAsync(string id, string tenantId)
        {
            var entity = await GetEntityWithIncludesAsync(id, tenantId);
            return await AssembleDtoAsync(entity);
        }

        private static EmployeeLoanListDto MapList(EmployeeLoan x) => new()
        {
            Id = x.Id,
            EmployeeId = x.EmployeeId,
            EmployeeName = x.Employee != null ? $"{x.Employee.FirstName} {x.Employee.LastName}".Trim() : null,
            EmployeeCode = x.Employee?.EmployeeCode,
            LoanTypeName = x.LoanType?.Name,
            RequestedAmount = x.RequestedAmount,
            ApprovedAmount = x.ApprovedAmount,
            OutstandingPrincipal = x.OutstandingPrincipal,
            Status = (int)x.Status,
            StatusName = x.Status.ToString(),
            CurrentApprovalLevel = x.CurrentApprovalLevel,
            DisbursedOn = x.DisbursedOn,
            CreatedOn = x.CreatedOn
        };

        private async Task<EmployeeLoanDto> AssembleDtoAsync(EmployeeLoan x)
        {
            var approvalHistory = await _uow.Repository<LoanApprovalHistory>().Query()
                .Where(h => h.EmployeeLoanId == x.Id)
                .OrderBy(h => h.LevelNumber)
                .ToListAsync();

            var emiSchedule = await _uow.Repository<LoanEmiSchedule>().Query()
                .Where(e => e.EmployeeLoanId == x.Id)
                .OrderBy(e => e.InstallmentNumber)
                .ToListAsync();

            var paymentHistory = await _uow.Repository<LoanPaymentHistory>().Query()
                .Where(p => p.EmployeeLoanId == x.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            var attachments = await _uow.Repository<LoanAdvanceAttachment>().Query()
                .Where(a => a.EntityType == LoanAttachmentEntityType.Loan && a.EntityId == x.Id)
                .ToListAsync();

            var userIds = approvalHistory
                .SelectMany(h => new[] { h.CheckerId, h.ActedAsDelegateForUserId })
                .Append(x.MakerId)
                .Where(id => !string.IsNullOrEmpty(id))
                .Select(id => id!)
                .Distinct()
                .ToList();

            var nameMap = await BuildUserNameMapAsync(userIds);

            var totalLevels = x.LoanPolicy?.ApprovalLevels?
                .Count(l => l.MinAmountThreshold <= x.RequestedAmount) ?? 0;

            var nextPending = emiSchedule.FirstOrDefault(e => e.Status == InstallmentStatus.Pending);

            return new EmployeeLoanDto
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
                LoanTypeId = x.LoanTypeId,
                LoanTypeName = x.LoanType?.Name,
                LoanPolicyId = x.LoanPolicyId,
                RequestedAmount = x.RequestedAmount,
                ApprovedAmount = x.ApprovedAmount,
                TenureMonths = x.TenureMonths,
                InterestRatePercent = x.InterestRatePercent,
                InterestMethod = (int)x.InterestMethod,
                InterestMethodName = x.InterestMethod.ToString(),
                Purpose = x.Purpose,
                Status = (int)x.Status,
                StatusName = x.Status.ToString(),
                CurrentApprovalLevel = x.CurrentApprovalLevel,
                TotalApprovalLevels = totalLevels,
                MakerId = x.MakerId,
                MakerName = nameMap.GetValueOrDefault(x.MakerId),
                MakerActionOn = x.MakerActionOn,
                MakerRemarks = x.MakerRemarks,
                DisbursedAmount = x.DisbursedAmount,
                DisbursedOn = x.DisbursedOn,
                DisbursementMode = x.DisbursementMode.HasValue ? (int)x.DisbursementMode.Value : null,
                DisbursementModeName = x.DisbursementMode?.ToString(),
                DisbursementReference = x.DisbursementReference,
                OutstandingPrincipal = x.OutstandingPrincipal,
                TotalPrincipalPaid = paymentHistory.Sum(p => p.PrincipalPaid),
                TotalInterestPaid = paymentHistory.Sum(p => p.InterestPaid),
                NextDueDate = nextPending?.DueDate,
                NextDueAmount = nextPending?.EmiAmount,
                DaysPastDue = nextPending != null && nextPending.DueDate < DateTime.UtcNow.Date
                    ? (DateTime.UtcNow.Date - nextPending.DueDate).Days
                    : 0,
                ClosedOn = x.ClosedOn,
                ClosureReason = x.ClosureReason.HasValue ? (int)x.ClosureReason.Value : null,
                ClosureReasonName = x.ClosureReason?.ToString(),
                ApprovalHistory = approvalHistory.Select(h => new LoanApprovalHistoryDto
                {
                    Id = h.Id,
                    EmployeeLoanId = h.EmployeeLoanId,
                    LevelNumber = h.LevelNumber,
                    CheckerId = h.CheckerId,
                    CheckerName = nameMap.GetValueOrDefault(h.CheckerId),
                    ActedAsDelegateForUserId = h.ActedAsDelegateForUserId,
                    ActedAsDelegateForUserName = h.ActedAsDelegateForUserId != null ? nameMap.GetValueOrDefault(h.ActedAsDelegateForUserId) : null,
                    Decision = (int)h.Decision,
                    DecisionName = h.Decision.ToString(),
                    Remarks = h.Remarks,
                    ActionOn = h.ActionOn
                }).ToList(),
                EmiSchedule = emiSchedule.Select(e => new LoanEmiScheduleDto
                {
                    Id = e.Id,
                    EmployeeLoanId = e.EmployeeLoanId,
                    InstallmentNumber = e.InstallmentNumber,
                    DueDate = e.DueDate,
                    OpeningBalance = e.OpeningBalance,
                    PrincipalComponent = e.PrincipalComponent,
                    InterestComponent = e.InterestComponent,
                    EmiAmount = e.EmiAmount,
                    ClosingBalance = e.ClosingBalance,
                    Status = (int)e.Status,
                    StatusName = e.Status.ToString(),
                    RecoveredOn = e.RecoveredOn,
                    PayrollId = e.PayrollId
                }).ToList(),
                PaymentHistory = paymentHistory.Select(p => new LoanPaymentHistoryDto
                {
                    Id = p.Id,
                    EmployeeLoanId = p.EmployeeLoanId,
                    LoanEmiScheduleId = p.LoanEmiScheduleId,
                    PaymentSource = (int)p.PaymentSource,
                    PaymentSourceName = p.PaymentSource.ToString(),
                    AmountPaid = p.AmountPaid,
                    PrincipalPaid = p.PrincipalPaid,
                    InterestPaid = p.InterestPaid,
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
