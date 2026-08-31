using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantService _tenantService;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService) : base(options)
        {
            _tenantService = tenantService;
        }

        #region 📊 API LOGGING
        public DbSet<ApiRequestLog> ApiRequestLogs { get; set; }
        public DbSet<ApiResponseLog> ApiResponseLogs { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        #endregion

        #region 🔐 AUTH MODULE
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<RoleFeature> RoleFeatures { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        #endregion

        #region 🏢 TENANT & SUBSCRIPTION
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<TenantFeature> TenantFeatures { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

        #endregion

        #region 🏢 ORGANIZATION
        public DbSet<Company> Companies { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        #endregion

        #region 🌍 LOCATION
        public DbSet<Country> Countries { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }
        #endregion

        #region 👤 EMPLOYEE
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
        public DbSet<EmployeeBankDetail> EmployeeBankDetails { get; set; }
        public DbSet<EmployeePFDetail> EmployeePFDetails { get; set; }
        public DbSet<EmployeeESICDetail> EmployeeESICDetails { get; set; }
        public DbSet<EmployeeEducationDetail> EmployeeEducationDetails { get; set; }
        public DbSet<EmployeeShiftMapping> EmployeeShiftMappings { get; set; }
        #endregion

        #region ⏱️ ATTENDANCE
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AttendanceLog> AttendanceLogs { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<BiometricDevice> BiometricDevices { get; set; }
        public DbSet<BiometricAttendanceLog> BiometricAttendanceLogs { get; set; }
        public DbSet<EmployeeBiometricMapping> EmployeeBiometricMappings { get; set; }
        public DbSet<BiometricAgent> BiometricAgents { get; set; }
        public DbSet<BiometricDeviceTestRequest> BiometricDeviceTestRequests { get; set; }
        public DbSet<BiometricSyncLog> BiometricSyncLogs { get; set; }

        // eSSL eTimeTrackLite1 direct-SQL attendance integration - the
        // per-tenant sync cursor/lock. History for this feature reuses
        // BiometricSyncLogs above (SyncType = "EsslDbPull") rather than a
        // second history table. See Domain/Entities/EsslAttendanceSyncState.cs.
        public DbSet<EsslAttendanceSyncState> EsslAttendanceSyncStates { get; set; }

        // Editable connection/sync configuration for the eSSL integration -
        // one row per tenant. See Domain/Entities/EsslIntegrationSetting.cs.
        public DbSet<EsslIntegrationSetting> EsslIntegrationSettings { get; set; }

        // Daily Work Entry / Employee Work Tracking module - master data
        // (Client/WorkJob/JobItem/JobType/WorkActivity/WorkEntryReason/
        // DocumentStatus) plus the DailyWorkLog (header, one per
        // Employee+Date, carries the Draft/Submitted/Approved/Rejected
        // workflow) and DailyWorkEntry (line items) transaction tables. See
        // Domain/Entities/DailyWorkLog.cs's remarks for why status lives on
        // the header rather than every line.
        public DbSet<Client> Clients { get; set; }
        public DbSet<WorkJob> WorkJobs { get; set; }
        public DbSet<JobItem> JobItems { get; set; }
        public DbSet<JobType> JobTypes { get; set; }
        public DbSet<WorkActivity> WorkActivities { get; set; }
        public DbSet<WorkEntryReason> WorkEntryReasons { get; set; }
        public DbSet<DocumentStatus> DocumentStatuses { get; set; }
        public DbSet<DailyWorkLog> DailyWorkLogs { get; set; }
        public DbSet<DailyWorkEntry> DailyWorkEntries { get; set; }
        public DbSet<DailyWorkLogApprovalHistory> DailyWorkLogApprovalHistories { get; set; }

        // Employee Job/Work Assignment - the Manager -> Employee assignment
        // layer that sits between the Job master and DailyWorkEntry (see
        // Domain/Entities/EmployeeWorkAssignment.cs's remarks).
        public DbSet<EmployeeWorkAssignment> EmployeeWorkAssignments { get; set; }
        public DbSet<EmployeeWorkAssignmentHistory> EmployeeWorkAssignmentHistories { get; set; }

        public DbSet<AttendanceRegularization> AttendanceRegularizations { get; set; }
        public DbSet<AttendanceRegularizationApprovalHistory> AttendanceRegularizationApprovalHistories { get; set; }

        // Company-wide attendance rules (separate from Shift's per-shift
        // timing) - see Domain/Entities/AttendancePolicy.cs.
        public DbSet<AttendancePolicy> AttendancePolicies { get; set; }

        // Work From Home requests (date-range, single-level approval) - see
        // Domain/Entities/WfhRequest.cs / WfhRequestService.
        public DbSet<WfhRequest> WfhRequests { get; set; }

        // On Duty requests (date-range, single-level approval) - near-exact
        // mirror of WfhRequests, no monthly limit - see
        // Domain/Entities/OnDutyRequest.cs / OnDutyRequestService.
        public DbSet<OnDutyRequest> OnDutyRequests { get; set; }

        // Short Leave requests (a few hours off during a working day,
        // single-level approval) - unlike WfhRequests/OnDutyRequests, does
        // NOT write back to Attendance; deducts a fractional day from a
        // chosen LeaveType's balance instead - see
        // Domain/Entities/ShortLeaveRequest.cs / ShortLeaveRequestService.
        public DbSet<ShortLeaveRequest> ShortLeaveRequests { get; set; }

        // Comp Off candidates ("System-detected, HR-approved") - created only
        // by CompOffDetectionService, reviewed (Approve/Reject) by HR - see
        // Domain/Entities/CompOffCandidate.cs / CompOffService.
        public DbSet<CompOffCandidate> CompOffCandidates { get; set; }

        #endregion

        #region 🧾 PROBATION & CONFIRMATION (Maker-Checker)
        // Foundational Maker-Checker (segregation-of-duties) workflow - see
        // Domain/Entities/ProbationConfirmation.cs / ProbationConfirmationService.
        public DbSet<ProbationConfirmation> ProbationConfirmations { get; set; }
        #endregion

        #region 🧾 PIP (Performance Improvement Plan) (Maker-Checker)
        // Phase 2 of Probation & Confirmation - see
        // Domain/Entities/PipRecord.cs / PipService. Maker-checker applies
        // to the final outcome resolution, not creation.
        public DbSet<PipRecord> PipRecords { get; set; }
        #endregion

        #region 🧾 EMPLOYEE TRANSFER (Maker-Checker)
        // Phase 3 of Probation & Confirmation (Employee Lifecycle) - see
        // Domain/Entities/EmployeeTransfer.cs / EmployeeTransferService.
        public DbSet<EmployeeTransfer> EmployeeTransfers { get; set; }
        #endregion

        #region 🧾 EMPLOYEE FEEDBACK
        // Phase 4 of Probation & Confirmation (Employee Lifecycle) - plain
        // CRUD, no maker-checker - see Domain/Entities/EmployeeFeedback.cs /
        // EmployeeFeedbackService.
        public DbSet<EmployeeFeedback> EmployeeFeedbacks { get; set; }
        #endregion

        #region 🧾 REJOINING
        // Phase 5 (final) of Probation & Confirmation (Employee Lifecycle) -
        // simple, single-step, HR-permission-gated rehire action, no
        // maker-checker - see Domain/Entities/RejoiningHistory.cs /
        // RejoiningService.
        public DbSet<RejoiningHistory> RejoiningHistories { get; set; }
        #endregion

        #region  📅 LEAVE
        public DbSet<LeaveType> LeaveTypes { get; set; }
        public DbSet<LeaveBalance> LeaveBalances { get; set; }
        public DbSet<LeaveBalanceTransaction> LeaveBalanceTransactions { get; set; }
        public DbSet<LeaveApplication> LeaveApplications { get; set; }
        public DbSet<LeaveApprovalHistory> LeaveApprovalHistories { get; set; }
        public DbSet<ApprovalDelegation> ApprovalDelegations { get; set; }
        #endregion

        #region 💰 PAYROLL
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PayrollDetail> PayrollDetails { get; set; }
        public DbSet<Payslip> Payslips { get; set; }
        public DbSet<SalaryComponent> SalaryComponents { get; set; }
        public DbSet<SalaryStructure> SalaryStructures { get; set; }
        public DbSet<SalaryDetail> SalaryDetails { get; set; }

        // Payslip Request approval workflow (Employee -> Reporting Manager
        // -> Finance) - see Domain/Entities/PayslipRequest.cs.
        public DbSet<PayslipRequest> PayslipRequests { get; set; }
        public DbSet<PayslipRequestAudit> PayslipRequestAudits { get; set; }
        #endregion

        #region 🧾 TAXATION (Income Tax / TDS - India)
        // Enterprise Taxation Module - see Domain/Entities/TaxSlab.cs,
        // TaxDeclaration.cs, EmployeeTaxComputation.cs and
        // Application/Services/Taxation/*.
        public DbSet<TaxSlab> TaxSlabs { get; set; }
        public DbSet<TaxDeclaration> TaxDeclarations { get; set; }
        public DbSet<EmployeeTaxComputation> EmployeeTaxComputations { get; set; }
        #endregion

        #region 💼 ASSET
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<AssetAllocation> AssetAllocations { get; set; }
        public DbSet<AssetHistory> AssetHistories { get; set; }
        #endregion

        #region ✅ TASKS
        public DbSet<EmployeeTask> EmployeeTasks { get; set; }
        #endregion

        #region 🎯 RECRUITMENT
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<JobOpening> JobOpenings { get; set; }
        public DbSet<CandidateApplication> CandidateApplications { get; set; }
        public DbSet<InterviewSchedule> InterviewSchedules { get; set; }
        #endregion

        #region 🧑‍💼 ONBOARDING
        public DbSet<OnboardingCase> OnboardingCases { get; set; }
        public DbSet<OnboardingChecklistItem> OnboardingChecklistItems { get; set; }
        public DbSet<OnboardingChecklistTemplateItem> OnboardingChecklistTemplateItems { get; set; }
        #endregion

        #region 📢 NOTIFICATION
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationGroup> NotificationGroups { get; set; }
        public DbSet<NotificationGroupAccess> NotificationGroupAccesses { get; set; }
        public DbSet<NotificationGroupUser> NotificationGroupUsers { get; set; }
        public DbSet<NotificationRecipient> NotificationRecipients { get; set; }
        #endregion

        #region ⚙️ MASTER TABLES
        public DbSet<AppFeature> AppFeatures { get; set; }
        public DbSet<FinancialYear> FinancialYears { get; set; }
        public DbSet<HolidayGroup> HolidayGroups { get; set; }
        public DbSet<HolidayGroupDetail> HolidayGroupDetails { get; set; }
        public DbSet<WeekOff> WeekOffs { get; set; }
        public DbSet<SequenceMaster> SequenceMasters { get; set; }
        #endregion

        #region 📢 ANNOUNCEMENT & EVENTS
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }

        #endregion

        #region 🎫 HELP & SUPPORT
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportTicketReply> SupportTicketReplies { get; set; }
        public DbSet<FaqItem> FaqItems { get; set; }

        #endregion

        #region 💰 LOAN & ADVANCE
        // Masters - see Domain/Entities/LoanType.cs / AdvanceType.cs.
        public DbSet<LoanType> LoanTypes { get; set; }
        public DbSet<AdvanceType> AdvanceTypes { get; set; }

        // Policy (versioned) + its configurable N-level approval matrix -
        // see Domain/Entities/LoanPolicy.cs / LoanPolicyApprovalLevel.cs.
        public DbSet<LoanPolicy> LoanPolicies { get; set; }
        public DbSet<LoanPolicyApprovalLevel> LoanPolicyApprovalLevels { get; set; }

        // Loan request/account (Maker-Checker, N-level) + its EMI schedule
        // and payment ledger - see Domain/Entities/EmployeeLoan.cs.
        public DbSet<EmployeeLoan> EmployeeLoans { get; set; }
        public DbSet<LoanApprovalHistory> LoanApprovalHistories { get; set; }
        public DbSet<LoanEmiSchedule> LoanEmiSchedules { get; set; }
        public DbSet<LoanPaymentHistory> LoanPaymentHistories { get; set; }

        // Advance request/account - near-mirror of EmployeeLoan, no
        // interest amortization - see Domain/Entities/EmployeeAdvance.cs.
        public DbSet<EmployeeAdvance> EmployeeAdvances { get; set; }
        public DbSet<AdvanceApprovalHistory> AdvanceApprovalHistories { get; set; }
        public DbSet<AdvanceInstallment> AdvanceInstallments { get; set; }
        public DbSet<AdvancePaymentHistory> AdvancePaymentHistories { get; set; }

        // Shared/polymorphic - see Domain/Entities/LoanAdvanceAttachment.cs
        // / LoanAdvanceAuditLog.cs.
        public DbSet<LoanAdvanceAttachment> LoanAdvanceAttachments { get; set; }
        public DbSet<LoanAdvanceAuditLog> LoanAdvanceAuditLogs { get; set; }
        #endregion

        #region 🔥 MODEL CONFIGURATION
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================================
            // 🔐 UNIQUE INDEXES
            // =====================================================
            modelBuilder.Entity<ApiResponseLog>(entity =>
            {
                entity.ToTable("ApiResponseLogs");

                entity.HasKey(e => e.Id);

                // 🔹 Identity / Codes
                entity.Property(e => e.Id)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.RequestId)
                    .HasMaxLength(50);

                entity.Property(e => e.CorrelationId)
                    .HasMaxLength(100);

                // 🔹 API Info
                entity.Property(e => e.Endpoint)
                    .HasMaxLength(500);

                entity.Property(e => e.Controller)
                    .HasMaxLength(100);

                entity.Property(e => e.Action)
                    .HasMaxLength(100);

                entity.Property(e => e.Method)
                    .HasMaxLength(10);

                // 🔹 Response
                entity.Property(e => e.ResponseBody)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.StatusCode);

                entity.Property(e => e.IsSuccess);

                // 🔹 Performance
                entity.Property(e => e.ExecutionTimeMs);

                // 🔹 User
                entity.Property(e => e.UserId)
                    .HasMaxLength(50);

                entity.Property(e => e.UserName)
                    .HasMaxLength(150);

                // 🔹 Multi-Tenant
                entity.Property(e => e.CompanyId)
                    .HasMaxLength(50);

                entity.Property(e => e.BranchId)
                    .HasMaxLength(50);

                // 🔹 Client
                entity.Property(e => e.IPAddress)
                    .HasMaxLength(50);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                // 🔹 Time
                entity.Property(e => e.ResponseTime)
                    .IsRequired();

                // 🔹 Extra
                entity.Property(e => e.Remarks)
                    .HasMaxLength(500);

                // 🔹 Indexes (IMPORTANT)
                entity.HasIndex(e => e.Id).IsUnique();
                entity.HasIndex(e => e.RequestId);
                entity.HasIndex(e => e.CorrelationId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.ResponseTime);
            });

            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.ToTable("ErrorLogs");

                entity.HasKey(e => e.Id);

                // 🔹 Identity
                entity.Property(e => e.Id)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.RequestId)
                    .HasMaxLength(50);

                entity.Property(e => e.CorrelationId)
                    .HasMaxLength(100);

                // 🔹 Error Info
                entity.Property(e => e.ErrorMessage)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.ExceptionType)
                    .HasMaxLength(200);

                entity.Property(e => e.StackTrace)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.InnerException)
                    .HasColumnType("nvarchar(max)");

                // 🔹 API Context
                entity.Property(e => e.Endpoint)
                    .HasMaxLength(500);

                entity.Property(e => e.Controller)
                    .HasMaxLength(100);

                entity.Property(e => e.Action)
                    .HasMaxLength(100);

                entity.Property(e => e.Method)
                    .HasMaxLength(10);

                // 🔹 Categorization (System Configurator Error Log screen)
                entity.Property(e => e.ModuleName)
                    .HasMaxLength(150);

                entity.Property(e => e.FeatureName)
                    .HasMaxLength(150);

                entity.HasIndex(e => e.ModuleName);
                entity.HasIndex(e => e.IsResolved);
                entity.HasIndex(e => e.ErrorTime);

                // 🔹 Request Snapshot
                entity.Property(e => e.RequestBody)
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.QueryParams)
                    .HasColumnType("nvarchar(max)");

                // 🔹 User
                entity.Property(e => e.UserId)
                    .HasMaxLength(50);

                entity.Property(e => e.UserName)
                    .HasMaxLength(150);

                // 🔹 Multi-Tenant
                entity.Property(e => e.CompanyId)
                    .HasMaxLength(50);

                // 🔹 Client
                entity.Property(e => e.IPAddress)
                    .HasMaxLength(50);

                entity.Property(e => e.UserAgent)
                    .HasMaxLength(500);

                // 🔹 Severity
                entity.Property(e => e.LogLevel)
                    .HasMaxLength(20);

                // 🔹 Time
                entity.Property(e => e.ErrorTime)
                    .IsRequired();

                // 🔹 Resolution
                entity.Property(e => e.IsResolved);

                entity.Property(e => e.ResolvedBy)
                    .HasMaxLength(100);

                entity.Property(e => e.ResolvedOn);

                // 🔹 Extra
                entity.Property(e => e.Remarks)
                    .HasMaxLength(500);

                // 🔹 Indexes (CRITICAL for debugging)
                entity.HasIndex(e => e.Id).IsUnique();
                entity.HasIndex(e => e.RequestId);
                entity.HasIndex(e => e.CorrelationId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CompanyId);
                entity.HasIndex(e => e.ErrorTime);
                entity.HasIndex(e => e.LogLevel);
            });

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<Employee>()
                .HasIndex(x => x.EmployeeCode)
                .IsUnique();

            modelBuilder.Entity<Asset>()
                .HasIndex(x => x.AssetCode)
                .IsUnique();

            modelBuilder.Entity<Attendance>()
                .HasIndex(x => new { x.EmployeeId, x.Date });

            // AttendanceLog had no explicit indexes before this feature -
            // AttendanceId/EmployeeId are the two lookup paths used by
            // AttendanceService (Include(x => x.Logs)) and
            // AttendanceProcessorService's tag-back query; PunchTime backs
            // the "find the log for this exact punch" lookup used there too.
            modelBuilder.Entity<AttendanceLog>()
                .HasIndex(x => x.AttendanceId);

            modelBuilder.Entity<AttendanceLog>()
                .HasIndex(x => new { x.EmployeeId, x.PunchTime });

            // DB-level idempotency backstop: the same raw biometric punch
            // can never be linked to more than one AttendanceLog. Filtered
            // so manual/web punches (BiometricAttendanceLogId = NULL) are
            // never constrained by this.
            modelBuilder.Entity<AttendanceLog>()
                .HasIndex(x => x.BiometricAttendanceLogId)
                .IsUnique()
                .HasFilter("[BiometricAttendanceLogId] IS NOT NULL")
                .HasDatabaseName("IX_AttendanceLogs_BiometricAttendanceLogId");

            modelBuilder.Entity<AttendanceRegularization>()
                .HasIndex(x => new { x.EmployeeId, x.Date });

            modelBuilder.Entity<AttendancePolicy>()
                .HasIndex(x => x.TenantId);

            modelBuilder.Entity<AttendancePolicy>()
                .HasIndex(x => new { x.TenantId, x.CompanyId, x.IsActive, x.EffectiveFrom });

            // Work From Home requests - EmployeeId for "my requests"/
            // approver-scoped lookups, (TenantId, Status) for admin/HR
            // list + pending-count queries.
            modelBuilder.Entity<WfhRequest>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<WfhRequest>()
                .HasIndex(x => new { x.TenantId, x.Status });

            // On Duty requests - same indexing rationale as WfhRequest
            // above: EmployeeId for "my requests"/approver-scoped lookups,
            // (TenantId, Status) for admin/HR list + pending-count queries.
            modelBuilder.Entity<OnDutyRequest>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<OnDutyRequest>()
                .HasIndex(x => new { x.TenantId, x.Status });

            // Short Leave requests - same indexing rationale as
            // WfhRequest/OnDutyRequest above: EmployeeId for "my requests"/
            // approver-scoped lookups, (TenantId, Status) for admin/HR
            // list + pending-count queries.
            modelBuilder.Entity<ShortLeaveRequest>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<ShortLeaveRequest>()
                .HasIndex(x => new { x.TenantId, x.Status });

            // Comp Off candidates - EmployeeId for "my history"/employee-scoped
            // lookups, (TenantId, Status) for the HR pending-review list, and
            // a UNIQUE (EmployeeId, AttendanceId) index as a DB-level backstop
            // against CompOffDetectionService creating duplicate candidates
            // for the same Attendance row on re-runs (the job also checks
            // this explicitly before inserting - see
            // CompOffDetectionService.RunCycleAsync - this index is the
            // belt-and-braces safety net).
            modelBuilder.Entity<CompOffCandidate>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<CompOffCandidate>()
                .HasIndex(x => new { x.TenantId, x.Status });

            modelBuilder.Entity<CompOffCandidate>()
                .HasIndex(x => new { x.EmployeeId, x.AttendanceId })
                .IsUnique();

            // Probation Confirmations (Maker-Checker) - EmployeeId for
            // "does this employee already have a PendingChecker record
            // open" checks and history lookups, (TenantId, Status) for the
            // HR pending-review / status-filtered list.
            modelBuilder.Entity<ProbationConfirmation>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<ProbationConfirmation>()
                .HasIndex(x => new { x.TenantId, x.Status });

            // PipRecord - EmployeeId for "does this employee already have
            // an active PIP" lookups, (TenantId, FinalOutcome) for the HR
            // "active PIPs" tracking view / outcome-filtered list.
            modelBuilder.Entity<PipRecord>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<PipRecord>()
                .HasIndex(x => new { x.TenantId, x.FinalOutcome });

            // EmployeeTransfer - EmployeeId for "does this employee already
            // have a PendingChecker transfer open" checks and transfer-
            // history lookups, (TenantId, Status) for the HR pending-
            // review / status-filtered list.
            modelBuilder.Entity<EmployeeTransfer>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<EmployeeTransfer>()
                .HasIndex(x => new { x.TenantId, x.Status });

            // EmployeeFeedback - (TenantId, EmployeeId) for the primary
            // "all feedback about this employee" listing (self-service "my
            // feedback" view and HR/manager "feedback about employee X"
            // view both go through GetForEmployeeAsync).
            modelBuilder.Entity<EmployeeFeedback>()
                .HasIndex(x => new { x.TenantId, x.EmployeeId });

            // RejoiningHistory - EmployeeId for "all rejoin history for this
            // employee" lookups (an employee may rejoin more than once over
            // their lifetime), mirroring the other Phase 1-4 entities' index
            // on EmployeeId.
            modelBuilder.Entity<RejoiningHistory>()
                .HasIndex(x => x.EmployeeId);

            // =====================================================
            // 🧾 TAXATION (Income Tax / TDS - India)
            // =====================================================
            // TaxSlab - (FinancialYearId, Regime) for the ComputeAsync
            // slab walk.
            modelBuilder.Entity<TaxSlab>()
                .HasOne(x => x.FinancialYear)
                .WithMany()
                .HasForeignKey(x => x.FinancialYearId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxSlab>()
                .HasIndex(x => new { x.FinancialYearId, x.Regime });

            // TaxDeclaration - one declaration per (EmployeeId,
            // FinancialYearId) - enforced in TaxDeclarationService as an
            // upsert, backed here by a unique index as the belt-and-
            // braces safety net (same pattern as
            // CompOffCandidate.(EmployeeId, AttendanceId)).
            modelBuilder.Entity<TaxDeclaration>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxDeclaration>()
                .HasOne(x => x.FinancialYear)
                .WithMany()
                .HasForeignKey(x => x.FinancialYearId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaxDeclaration>()
                .HasIndex(x => new { x.EmployeeId, x.FinancialYearId })
                .IsUnique();

            modelBuilder.Entity<TaxDeclaration>()
                .HasIndex(x => new { x.TenantId, x.Status });

            // EmployeeTaxComputation - one computed row per (EmployeeId,
            // FinancialYearId), same uniqueness shape as TaxDeclaration
            // above.
            modelBuilder.Entity<EmployeeTaxComputation>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTaxComputation>()
                .HasOne(x => x.FinancialYear)
                .WithMany()
                .HasForeignKey(x => x.FinancialYearId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTaxComputation>()
                .HasOne(x => x.TaxDeclaration)
                .WithMany()
                .HasForeignKey(x => x.TaxDeclarationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTaxComputation>()
                .HasIndex(x => new { x.EmployeeId, x.FinancialYearId })
                .IsUnique();

            modelBuilder.Entity<Payroll>()
                .HasIndex(x => new { x.EmployeeId, x.SalaryMonth });

            // =====================================================
            // 💰 SALARY STRUCTURE
            // =====================================================
            modelBuilder.Entity<SalaryStructure>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryStructure>()
                .HasMany(x => x.SalaryDetails)
                .WithOne(x => x.EmployeeSalaryStructure)
                .HasForeignKey(x => x.SalaryStructureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SalaryDetail>()
                .HasOne(x => x.SalaryComponent)
                .WithMany()
                .HasForeignKey(x => x.SalaryComponentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryStructure>()
                .HasIndex(x => new { x.EmployeeId, x.EffectiveFrom });

            // =====================================================
            // ✅ TASKS
            // =====================================================
            modelBuilder.Entity<EmployeeTask>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeTask>()
                .HasIndex(x => new { x.EmployeeId, x.Status });

            // =====================================================
            // 🧑‍💼 ONBOARDING
            // =====================================================
            modelBuilder.Entity<OnboardingCase>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OnboardingCase>()
                .HasOne(x => x.Candidate)
                .WithMany()
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OnboardingCase>()
                .HasIndex(x => x.EmployeeId);

            modelBuilder.Entity<OnboardingCase>()
                .HasIndex(x => x.TenantId);

            modelBuilder.Entity<OnboardingChecklistItem>()
                .HasOne(x => x.OnboardingCase)
                .WithMany(x => x.ChecklistItems)
                .HasForeignKey(x => x.OnboardingCaseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OnboardingChecklistItem>()
                .HasIndex(x => new { x.OnboardingCaseId, x.StageType });

            modelBuilder.Entity<OnboardingChecklistTemplateItem>()
                .HasIndex(x => new { x.TenantId, x.StageType });

            // =====================================================
            // 🔗 USER / ROLE / PERMISSION
            // =====================================================
            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserRole>()
                .HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RolePermission>()
                .HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 🏢 TENANT / COMPANY / BRANCH (CRITICAL 🔥)
            // =====================================================
            modelBuilder.Entity<Company>()
                .HasOne(x => x.Tenant)
                .WithMany(t => t.Companies)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Branch>()
                .HasOne(x => x.Company)
                .WithMany(c => c.Branches)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Branch>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
            .HasOne(t => t.Country)
            .WithMany()
            .HasForeignKey(t => t.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.State)
                .WithMany()
                .HasForeignKey(t => t.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tenant>()
                .HasOne(t => t.City)
                .WithMany()
                .HasForeignKey(t => t.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
            .HasOne(c => c.Country)
            .WithMany()
            .HasForeignKey(c => c.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
                .HasOne(c => c.State)
                .WithMany()
                .HasForeignKey(c => c.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Company>()
                .HasOne(c => c.City)
                .WithMany()
                .HasForeignKey(c => c.CityId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 👤 SELF REFERENCES
            // =====================================================
            modelBuilder.Entity<Department>()
                .HasOne(d => d.ParentDepartment)
                .WithMany(d => d.ChildDepartments)
                .HasForeignKey(d => d.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Designation>()
                .HasOne(d => d.ParentDesignation)
                .WithMany(d => d.ChildDesignations)
                .HasForeignKey(d => d.ParentDesignationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.ReportingManager)
                .WithMany()
                .HasForeignKey(e => e.ReportingManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 🔁 APPROVAL DELEGATION (two distinct FKs to Employee - must be
            // configured explicitly or EF can't disambiguate them)
            // =====================================================
            modelBuilder.Entity<ApprovalDelegation>()
                .HasOne(x => x.DelegatorEmployee)
                .WithMany()
                .HasForeignKey(x => x.DelegatorEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApprovalDelegation>()
                .HasOne(x => x.DelegateEmployee)
                .WithMany()
                .HasForeignKey(x => x.DelegateEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ApprovalDelegation>()
                .HasIndex(x => new { x.DelegatorEmployeeId, x.StartDate, x.EndDate });

            // =====================================================
            // 🌍 LOCATION (NO CASCADE)
            // =====================================================
            modelBuilder.Entity<State>()
                .HasOne(s => s.Country)
                .WithMany(c => c.States)
                .HasForeignKey(s => s.CountryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<City>()
                .HasOne(c => c.State)
                .WithMany(s => s.Cities)
                .HasForeignKey(c => c.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // 📢 ANNOUNCEMENT
            // =====================================================
            modelBuilder.Entity<Announcement>(entity =>
            {
                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Message)
                    .HasMaxLength(1000);

                entity.HasIndex(x => new { x.PublishDate, x.ExpiryDate });
            });

            // =====================================================
            // 🎉 EVENT
            // =====================================================
            modelBuilder.Entity<Event>(entity =>
            {
                entity.Property(x => x.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.Location)
                    .HasMaxLength(200);

                entity.HasIndex(x => new { x.StartDate, x.EndDate });
            });

            modelBuilder.Entity<EventParticipant>(entity =>
            {
                entity.HasOne(x => x.Event)
                    .WithMany(x => x.Participants)
                    .HasForeignKey(x => x.EventId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Employee)
                    .WithMany()
                    .HasForeignKey(x => x.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.EventId, x.EmployeeId })
                    .IsUnique();
            });

            // =====================================================
            // 💰 LOAN & ADVANCE
            // =====================================================
            // Filtered unique indexes match Phase 4 SQL script's
            // UX_LoanTypes_Tenant_Code / UX_AdvanceTypes_Tenant_Code -
            // scoped by IsDeleted so a soft-deleted Code can be reused.
            modelBuilder.Entity<LoanType>(entity =>
            {
                entity.HasIndex(e => new { e.TenantId, e.Code })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0")
                    .HasDatabaseName("UX_LoanTypes_Tenant_Code");
            });

            modelBuilder.Entity<AdvanceType>(entity =>
            {
                entity.HasIndex(e => new { e.TenantId, e.Code })
                    .IsUnique()
                    .HasFilter("[IsDeleted] = 0")
                    .HasDatabaseName("UX_AdvanceTypes_Tenant_Code");
            });

            modelBuilder.Entity<LoanPolicyApprovalLevel>(entity =>
            {
                entity.HasIndex(e => new { e.LoanPolicyId, e.LevelNumber })
                    .IsUnique()
                    .HasDatabaseName("UX_LoanPolicyApprovalLevels");
            });

            modelBuilder.Entity<EmployeeLoan>(entity =>
            {
                entity.HasIndex(e => new { e.EmployeeId, e.Status })
                    .HasDatabaseName("IX_EmployeeLoans_Employee_Status");
            });

            modelBuilder.Entity<LoanEmiSchedule>(entity =>
            {
                entity.HasIndex(e => new { e.EmployeeLoanId, e.InstallmentNumber })
                    .IsUnique()
                    .HasDatabaseName("UX_LoanEmiSchedules");

                entity.HasIndex(e => new { e.Status, e.DueDate })
                    .HasDatabaseName("IX_LoanEmiSchedules_DueTracking");
            });

            modelBuilder.Entity<EmployeeAdvance>(entity =>
            {
                entity.HasIndex(e => new { e.EmployeeId, e.Status })
                    .HasDatabaseName("IX_EmployeeAdvances_Employee_Status");
            });

            modelBuilder.Entity<AdvanceInstallment>(entity =>
            {
                entity.HasIndex(e => new { e.EmployeeAdvanceId, e.InstallmentNumber })
                    .IsUnique()
                    .HasDatabaseName("UX_AdvanceInstallments");

                entity.HasIndex(e => new { e.Status, e.DueDate })
                    .HasDatabaseName("IX_AdvanceInstallments_DueTracking");
            });

            modelBuilder.Entity<LoanAdvanceAttachment>(entity =>
            {
                entity.HasIndex(e => new { e.EntityType, e.EntityId })
                    .HasDatabaseName("IX_LoanAdvanceAttachments_Entity");
            });

            modelBuilder.Entity<LoanAdvanceAuditLog>(entity =>
            {
                entity.HasIndex(e => new { e.EntityType, e.EntityId, e.PerformedOn })
                    .HasDatabaseName("IX_LoanAdvanceAuditLogs_Entity");

                entity.Property(e => e.OldValuesJson).HasColumnType("nvarchar(max)");
                entity.Property(e => e.NewValuesJson).HasColumnType("nvarchar(max)");
            });

            // =====================================================
            // PHASE 18 - PERFORMANCE: additional indexes discovered once
            // real query patterns existed (Phase 14 reports, Phase 15
            // reminders, Phase 16 audit) - the original Phase 3/4 indexes
            // above covered per-employee lookups (EmployeeId+Status) but not
            // the tenant-wide aggregate scans GetDashboardAsync/
            // GetOutstandingBalanceReportAsync run, nor the payment-history
            // date-range filter, nor the reminder sweep's per-day dedup
            // check against Notifications.
            // =====================================================
            modelBuilder.Entity<EmployeeLoan>(entity =>
            {
                // Dashboard/report aggregates filter by TenantId first, then
                // Status - EmployeeId+Status (above) doesn't help those scans.
                entity.HasIndex(e => new { e.TenantId, e.Status })
                    .HasDatabaseName("IX_EmployeeLoans_Tenant_Status");

                // Was specified in the original Phase 4 SQL design script
                // (docs/LoanAdvanceModule/04-SQL-Scripts.sql) but never made
                // it into this Fluent config - added here on Phase 18 review.
                entity.HasIndex(e => new { e.TenantId, e.CompanyId, e.BranchId })
                    .HasDatabaseName("IX_EmployeeLoans_Tenant_Company_Branch");
            });

            modelBuilder.Entity<EmployeeAdvance>(entity =>
            {
                entity.HasIndex(e => new { e.TenantId, e.Status })
                    .HasDatabaseName("IX_EmployeeAdvances_Tenant_Status");

                entity.HasIndex(e => new { e.TenantId, e.CompanyId, e.BranchId })
                    .HasDatabaseName("IX_EmployeeAdvances_Tenant_Company_Branch");
            });

            modelBuilder.Entity<LoanPaymentHistory>(entity =>
            {
                // Per-loan statement view (EmployeeLoanId, PaymentDate) was
                // in the original Phase 4 script but missing here; the
                // standalone PaymentDate index is new for Phase 14's
                // tenant-wide GetPaymentHistoryReportAsync date-range scan,
                // which never filters by EmployeeLoanId so the composite
                // above wouldn't help it.
                entity.HasIndex(e => new { e.EmployeeLoanId, e.PaymentDate })
                    .HasDatabaseName("IX_LoanPaymentHistories_Loan_Date");

                entity.HasIndex(e => e.PaymentDate)
                    .HasDatabaseName("IX_LoanPaymentHistories_PaymentDate");
            });

            modelBuilder.Entity<AdvancePaymentHistory>(entity =>
            {
                entity.HasIndex(e => new { e.EmployeeAdvanceId, e.PaymentDate })
                    .HasDatabaseName("IX_AdvancePaymentHistories_Advance_Date");

                entity.HasIndex(e => e.PaymentDate)
                    .HasDatabaseName("IX_AdvancePaymentHistories_PaymentDate");
            });

            modelBuilder.Entity<LoanApprovalHistory>(entity =>
            {
                // Composite FK+LevelNumber index from the Phase 4 script -
                // EF only auto-indexes the bare FK column, not this composite.
                entity.HasIndex(e => new { e.EmployeeLoanId, e.LevelNumber })
                    .HasDatabaseName("IX_LoanApprovalHistories_Loan");
            });

            modelBuilder.Entity<AdvanceApprovalHistory>(entity =>
            {
                entity.HasIndex(e => new { e.EmployeeAdvanceId, e.LevelNumber })
                    .HasDatabaseName("IX_AdvanceApprovalHistories_Advance");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                // LoanAdvanceReminderService.AlreadyNotifiedTodayAsync
                // (Phase 15) filters "ReferenceId IN (...) AND CreatedOn >=
                // todayStart" on every sweep - without this, that becomes a
                // full table scan of a table that grows every single day.
                entity.HasIndex(e => new { e.ReferenceId, e.CreatedOn })
                    .HasDatabaseName("IX_Notifications_Reference_CreatedOn");
            });

            // =====================================================
            // 🖐️ BIOMETRIC DEVICE + AGENT INTEGRATION
            // =====================================================
            modelBuilder.Entity<BiometricAgent>(entity =>
            {
                // AgentCode is only guaranteed unique within a tenant (two
                // different clients could both pick "AGENT-01").
                entity.HasIndex(e => new { e.TenantId, e.AgentCode })
                    .IsUnique()
                    .HasDatabaseName("IX_BiometricAgents_Tenant_AgentCode");

                // Purely informational (which branch this agent's machine
                // sits at) - optional, no cascade behavior beyond the
                // global DeleteBehavior.Restrict fix below.
                entity.HasOne(e => e.Branch)
                    .WithMany()
                    .HasForeignKey(e => e.BranchId)
                    .IsRequired(false);
            });

            modelBuilder.Entity<BiometricDevice>(entity =>
            {
                entity.HasIndex(e => new { e.TenantId, e.DeviceCode })
                    .IsUnique()
                    .HasDatabaseName("IX_BiometricDevices_Tenant_DeviceCode");

                // A device is optionally assigned to one agent; deleting an
                // agent must not silently delete its devices (see the global
                // DeleteBehavior.Restrict fix below) - the admin has to
                // reassign/unassign devices first.
                entity.HasOne(d => d.Agent)
                    .WithMany(a => a.Devices)
                    .HasForeignKey(d => d.AgentId)
                    .IsRequired(false);
            });

            modelBuilder.Entity<BiometricDeviceTestRequest>(entity =>
            {
                // The agent polls "give me MY pending test requests" every
                // cycle - this is the hot query, so index exactly what it
                // filters on (AgentId + Status = Pending).
                entity.HasIndex(e => new { e.AgentId, e.Status })
                    .HasDatabaseName("IX_BiometricDeviceTestRequests_Agent_Status");

                // The UI polls "is MY request done yet" by Id (PK, already
                // indexed) but also lists recent attempts per device for
                // troubleshooting.
                entity.HasIndex(e => new { e.DeviceId, e.RequestedOn })
                    .HasDatabaseName("IX_BiometricDeviceTestRequests_Device_RequestedOn");

                entity.HasOne(e => e.Device)
                    .WithMany()
                    .HasForeignKey(e => e.DeviceId)
                    .IsRequired(true);
            });

            modelBuilder.Entity<BiometricAttendanceLog>(entity =>
            {
                // Primary idempotency guard: the same device can never report
                // the same employee punching at the same instant twice. This
                // is the DB-level backstop behind the application-level
                // duplicate check in BiometricSyncService.IngestPunchesAsync
                // (belt-and-braces - a unique index survives races between
                // concurrent ingest calls that the app-level check alone does not).
                entity.HasIndex(e => new { e.TenantId, e.DeviceId, e.EmployeeCode, e.PunchTime })
                    .IsUnique()
                    .HasDatabaseName("IX_BiometricAttendanceLogs_Tenant_Device_Employee_PunchTime");

                // Stronger idempotency guard when the device supplies its own
                // transaction id - filtered so it only applies to rows that
                // actually have one (older/other drivers may leave it null).
                entity.HasIndex(e => new { e.DeviceId, e.DeviceTransactionId })
                    .IsUnique()
                    .HasFilter("[DeviceTransactionId] IS NOT NULL")
                    .HasDatabaseName("IX_BiometricAttendanceLogs_Device_TransactionId");

                // AttendanceProcessorService's main query is
                // "Where(x => !x.IsProcessed)" across the whole table -
                // without this, that becomes a full table scan once the
                // table grows past a trivial size.
                entity.HasIndex(e => new { e.TenantId, e.IsProcessed })
                    .HasDatabaseName("IX_BiometricAttendanceLogs_Tenant_IsProcessed");
            });

            modelBuilder.Entity<BiometricSyncLog>(entity =>
            {
                entity.HasIndex(e => new { e.DeviceId, e.StartTime })
                    .HasDatabaseName("IX_BiometricSyncLogs_Device_StartTime");

                entity.HasOne(e => e.Device)
                    .WithMany()
                    .HasForeignKey(e => e.DeviceId)
                    .IsRequired(false);

                // Query pattern for the eSSL Sync Logs admin screen: latest
                // "EsslDbPull" runs first.
                entity.HasIndex(e => new { e.TenantId, e.SyncType, e.StartTime })
                    .HasDatabaseName("IX_BiometricSyncLogs_Tenant_SyncType_StartTime");
            });

            modelBuilder.Entity<EsslAttendanceSyncState>(entity =>
            {
                // One cursor/lock row per tenant - upserted, never duplicated.
                entity.HasIndex(e => e.TenantId)
                    .IsUnique()
                    .HasDatabaseName("IX_EsslAttendanceSyncStates_Tenant");
            });

            modelBuilder.Entity<EsslIntegrationSetting>(entity =>
            {
                // One configuration row per tenant - upserted, never duplicated.
                entity.HasIndex(e => e.TenantId)
                    .IsUnique()
                    .HasDatabaseName("IX_EsslIntegrationSettings_Tenant");
            });

            // =====================================================
            // Daily Work Entry / Employee Work Tracking module
            // =====================================================

            modelBuilder.Entity<WorkJob>()
                .HasIndex(x => x.JobNumber);

            modelBuilder.Entity<JobItem>()
                .HasIndex(x => x.WorkJobId);

            modelBuilder.Entity<WorkActivity>()
                .HasIndex(x => x.JobTypeId);

            // One header row per Employee+Date - Employee+Date and
            // Employee+Month(via Employee+Date range) are this module's
            // hottest report/lookup paths (spec section 40).
            modelBuilder.Entity<DailyWorkLog>()
                .HasIndex(x => new { x.EmployeeId, x.WorkDate })
                .IsUnique()
                .HasDatabaseName("IX_DailyWorkLogs_Employee_Date");

            modelBuilder.Entity<DailyWorkLog>()
                .HasIndex(x => new { x.TenantId, x.Status });

            modelBuilder.Entity<DailyWorkEntry>()
                .HasIndex(x => x.DailyWorkLogId);

            modelBuilder.Entity<DailyWorkEntry>()
                .HasIndex(x => new { x.WorkJobId, x.JobItemId });

            modelBuilder.Entity<DailyWorkLogApprovalHistory>()
                .HasIndex(x => x.DailyWorkLogId);

            // Backs WorkAssignmentService's "how much has actually been
            // logged against this assignment" aggregation.
            modelBuilder.Entity<DailyWorkEntry>()
                .HasIndex(x => x.AssignmentId);

            // =====================================================
            // Employee Job/Work Assignment
            // =====================================================

            // "My Assigned Jobs" (spec section 7) and the assignment-scoped
            // cascading dropdowns in Daily Work Entry (spec section 9) both
            // filter by Employee+Status - this is the hottest read path.
            modelBuilder.Entity<EmployeeWorkAssignment>()
                .HasIndex(x => new { x.EmployeeId, x.Status });

            // Manager's "assignments I made" / Team Leader's "my team's
            // assignments" views (spec section 20).
            modelBuilder.Entity<EmployeeWorkAssignment>()
                .HasIndex(x => x.AssignedBy);

            modelBuilder.Entity<EmployeeWorkAssignment>()
                .HasIndex(x => new { x.WorkJobId, x.JobItemId });

            modelBuilder.Entity<EmployeeWorkAssignment>()
                .HasIndex(x => x.ReassignedFromId);

            // A self-reference (ReassignedFrom) needs Restrict, not the
            // provider default Cascade, or SQL Server refuses to create the
            // FK ("may cause cycles or multiple cascade paths").
            modelBuilder.Entity<EmployeeWorkAssignment>()
                .HasOne(x => x.ReassignedFrom)
                .WithMany()
                .HasForeignKey(x => x.ReassignedFromId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeWorkAssignmentHistory>()
                .HasIndex(x => x.EmployeeWorkAssignmentId);

            // =====================================================
            // 🔥 GLOBAL FIX (VERY IMPORTANT)
            // =====================================================
            foreach (var fk in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // =====================================================
            // 🗑️ SOFT DELETE FILTER
            // =====================================================
            ApplySoftDeleteFilter(modelBuilder);
        }
        #endregion

        #region 🔥 GLOBAL SOFT DELETE
        private void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var methodInfo = typeof(ApplicationDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static);

                    if (methodInfo == null) continue;

                    var genericMethod = methodInfo.MakeGenericMethod(entityType.ClrType);
                    genericMethod.Invoke(null, new object[] { modelBuilder });
                }
            }
        }
        #endregion

        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder)
        where TEntity : BaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        // ==============================
        // 🔥 AUDIT HANDLING
        // ==============================
        public override int SaveChanges()
        {
            ApplyAudit();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAudit();

            foreach (var entry in ChangeTracker.Entries<IEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (string.IsNullOrEmpty(entry.Entity.Id))
                    {
                        entry.Entity.Id = entry.Entity.GetNewId(); // 🔥 IDManager use
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
        private void ApplyAudit()
        {
            var entries = ChangeTracker.Entries<BaseEntity>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedOn = DateTime.UtcNow;

                if (entry.State == EntityState.Modified)
                    entry.Entity.ModifiedOn = DateTime.UtcNow;
            }
        }
    }
}
