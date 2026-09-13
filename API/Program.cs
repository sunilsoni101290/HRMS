using API.BackgroundServices;
using API.Middleware;
using Application.DTOs.Attendances;
using FluentValidation;
using Application.Interfaces;
using Application.Interfaces.Attendances;
using Application.Interfaces.Auth;
using Application.Interfaces.Company;
using Application.Interfaces.EmployeeInterface;
using Application.Interfaces.EmployeeLifecycle;
using Application.Interfaces.ErrorLog;
using Application.Interfaces.JWT_TOKEN;
using Application.Interfaces.Leaves;
using Application.Interfaces.Masters;
using Application.Services;
using Application.Services.Attendances;
using Application.Services.Auth;
using Application.Services.CompanyService;
using Application.Services.EmployeeLifecycle;
using Application.Services.EmployeeServices;
using Application.Services.ErrorLogs;
using Application.Services.JWT_Token;
using Application.Services.Leaves;
using Application.Services.Masters;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// DATABASE
// ======================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ERPConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);

            sqlOptions.CommandTimeout(120);
        });

    // Phase 16 - automatic Loan & Advance audit trail; one interceptor
    // instance per DbContextOptions build (= per request scope), so its
    // between-hooks state is never shared across concurrent requests. See
    // Infrastructure/Interceptors/LoanAdvanceAuditInterceptor.cs.
    options.AddInterceptors(new Infrastructure.Interceptors.LoanAdvanceAuditInterceptor());
});

// eSSL eTimeTrackLite1 direct-SQL attendance integration - EsslDbContext
// (Infrastructure/EsslIntegration/EsslDbContext.cs) is a SEPARATE
// DbContext/connection from ApplicationDbContext (requirement: never fold
// the vendor's database into the primary HRMS EF Core DbContext), but it is
// NOT registered with AddDbContext here - since the Settings page made the
// server/database/credentials editable per-tenant at runtime,
// EsslAttendanceDataSource now builds a short-lived EsslDbContext instance
// per call from that tenant's persisted EsslIntegrationSetting row (falling
// back to EsslDatabase:ConnectionString in appsettings.json only if no row
// has been saved yet) instead of a single fixed connection string resolved
// once at app startup. See EsslAttendanceDataSource.BuildConnectionStringAsync.
//
// Data Protection (built into the ASP.NET Core shared framework - no new
// package) is what encrypts the saved eSSL database password at rest; both
// EsslAttendanceDataSource and EsslAttendanceSyncService create an
// IDataProtector with the exact same purpose string
// ("EsslIntegration.DatabasePassword.v1") so Protect/Unprotect round-trip.
//
// PersistKeysToFileSystem is required under IIS: the default key storage
// location depends on a loaded user profile, which the ApplicationPoolIdentity
// IIS runs this app as does NOT have. Without this, a new key ring is
// silently generated on every app pool recycle/restart - which doesn't just
// invalidate cookies, it makes every eSSL password already encrypted with
// the OLD key permanently undecryptable (Unprotect throws). Keys are stored
// under App_Data\Keys, next to the app, so the IIS Application Pool
// identity needs Modify rights there (see deployment guide's folder
// permissions section) - same folder survives publishes since dotnet
// publish doesn't delete existing files by default (DeleteExistingFiles=false
// in the LocalIIS profile).
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys")))
    .SetApplicationName("HRMS-ERP-API");

/*
 // ======================================================
// CORS
// ======================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
 */

// IIS deployment: the APP site's real origin is read from
// Cors:AllowedOrigins in appsettings (set to http://localhost:8080 in
// appsettings.Production.json to match the IIS "ERP" site) so this never
// silently points at a dev-only port again. Falls back to the original
// dev value if the key is absent, so local `dotnet run` behavior is
// unchanged.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:8081" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMVC", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ======================================================
// CONTROLLERS
// ======================================================
builder.Services.AddControllers(options =>
    {
        // Phase 9 (Validation) - global FluentValidation pass, additive
        // for every controller (see API/Filters/FluentValidationActionFilter.cs).
        options.Filters.Add<API.Filters.FluentValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

// Phase 9 - scans the Application assembly for every AbstractValidator<T>
// (Application/Validators/LoanAdvance/*.cs) and registers each as
// IValidator<T> so FluentValidationActionFilter can resolve them.
builder.Services.AddValidatorsFromAssemblyContaining<Application.Validators.LoanAdvance.LoanSubmitDtoValidator>();

// ======================================================
// JWT AUTHENTICATION
// ======================================================
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrEmpty(jwtKey))
    throw new Exception("JWT Key is missing in appsettings.json");

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;

        options.SaveToken = true;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateIssuer = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,

            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,

            // Zero clock skew means the token is rejected the INSTANT it
            // hits its exp claim, with no tolerance at all for clock drift
            // between the API and APP servers or for the network latency
            // of the request itself. The APP proactively refreshes tokens
            // before sending them (see JwtTokenHelper.IsTokenExpired /
            // JwtAuthorizeAttribute), but that check runs a few
            // milliseconds-to-seconds before the request actually reaches
            // here - with zero tolerance, a request that lands right on
            // the boundary gets a hard 401 even though the APP thought the
            // token was still valid. That intermittent 401 is what made
            // users appear to get randomly logged out mid-session,
            // especially "after calling an API". A small grace window
            // fixes this without weakening security in any meaningful way.
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// ======================================================
// DEPENDENCY INJECTION
// ======================================================
builder.Services.AddScoped<IErrorLogService, ErrorLogService>();
builder.Services.AddScoped<ISequenceService, SequenceService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
// TenantMiddleware removed (was hard-blocking every non-/api/auth request
// with "Tenant not provided" 400 whenever the JWT's TenantId claim was
// empty - see TenantService's remarks). TenantService now resolves TenantId
// itself, on demand, from the current HttpContext via IHttpContextAccessor -
// no middleware needed, and a request is never rejected outright just for
// not carrying a tenant.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantBusinessService, TenantBusinessService>();
builder.Services.AddScoped<IAppFeatureService, AppFeatureService>();
builder.Services.AddScoped<IFinancialYearService, FinancialYearService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
builder.Services.AddScoped<IEmployeeBankDetailService, EmployeeBankDetailService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IBiometricDeviceService, BiometricDeviceService>();
builder.Services.AddScoped<IBiometricSyncService,BiometricSyncService>();
builder.Services.AddScoped<IBiometricAgentService, BiometricAgentService>();
builder.Services.AddScoped<IAttendanceProcessorService,AttendanceProcessorService>();
builder.Services.AddScoped<IEmployeeBiometricMappingService, EmployeeBiometricMappingService>();
builder.Services.AddScoped<IEsslAttendanceDataSource, EsslAttendanceDataSource>();
builder.Services.AddScoped<IEsslAttendanceSyncService, EsslAttendanceSyncService>();
// Singleton in-process queue for manual eSSL sync requests (Sync Now /
// Historical Import / Retry Failed Sync) - consumed by the ALREADY-
// registered EsslAttendanceSyncBackgroundService below, instead of a
// detached Task.Run per request. See IEsslSyncJobQueue's remarks.
builder.Services.AddSingleton<IEsslSyncJobQueue, EsslSyncJobQueue>();
builder.Services.AddScoped<IBiometricSimulatorService, BiometricSimulatorService>();
builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<ILeaveApplicationService, LeaveApplicationService>();
builder.Services.AddScoped<Application.Interfaces.Leaves.IApprovalDelegationService, Application.Services.Leaves.ApprovalDelegationService>();
builder.Services.AddScoped<IAttendanceRegularizationService, AttendanceRegularizationService>();
builder.Services.AddScoped<IAttendancePolicyService, AttendancePolicyService>();
builder.Services.AddScoped<IWfhRequestService, WfhRequestService>();
builder.Services.AddScoped<IOnDutyRequestService, OnDutyRequestService>();
builder.Services.AddScoped<IShortLeaveRequestService, ShortLeaveRequestService>();
builder.Services.AddScoped<ICompOffService, CompOffService>();
builder.Services.AddScoped<IProbationConfirmationService, ProbationConfirmationService>();
builder.Services.AddScoped<IPipService, PipService>();
builder.Services.AddScoped<IEmployeeTransferService, EmployeeTransferService>();
builder.Services.AddScoped<IEmployeeFeedbackService, EmployeeFeedbackService>();
builder.Services.AddScoped<IRejoiningService, RejoiningService>();
builder.Services.AddScoped<IHolidayGroupService, HolidayGroupService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IDesignationService, DesignationService>();
builder.Services.AddScoped<ICountryService, CountryService>();
builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IDropdownService,DropdownService>();
builder.Services.AddScoped<IShiftService, ShiftService>();
builder.Services.AddScoped<IEmployeeShiftMappingService, EmployeeShiftMappingService>();
builder.Services.AddScoped<IHolidayGroupService, HolidayGroupService>();
builder.Services.AddScoped<IWeekOffService, WeekOffService>();

// ===================== Asset Module =====================
builder.Services.AddScoped<Application.Interfaces.Assets.IAssetCategoryService, Application.Services.Assets.AssetCategoryService>();
builder.Services.AddScoped<Application.Interfaces.Assets.IAssetService, Application.Services.Assets.AssetService>();
builder.Services.AddScoped<Application.Interfaces.Assets.IAssetAllocationService, Application.Services.Assets.AssetAllocationService>();

// ===================== Payroll Module =====================
builder.Services.AddScoped<Application.Interfaces.Payroll.ISalaryComponentService, Application.Services.PayrollService.SalaryComponentService>();
builder.Services.AddScoped<Application.Interfaces.Payroll.ISalaryStructureService, Application.Services.PayrollService.SalaryStructureService>();
builder.Services.AddScoped<Application.Interfaces.Payroll.ISalaryTemplateService, Application.Services.PayrollService.SalaryTemplateService>();
builder.Services.AddScoped<Application.Interfaces.Payroll.IPayrollBusinessService, Application.Services.PayrollService.PayrollBusinessService>();
builder.Services.AddScoped<Application.Interfaces.Payroll.ISalaryCalculationService, Application.Services.PayrollService.SalaryCalculationService>();
builder.Services.AddScoped<Application.Interfaces.Payroll.IPayslipRequestService, Application.Services.PayrollService.PayslipRequestService>();

// ===================== Taxation Module =====================
// TaxComputationService is registered first since TaxDeclarationService
// takes an ITaxComputationService dependency (Verify triggers a
// recompute) - order doesn't matter to the DI container itself, but
// matches this codebase's habit of listing dependencies before
// dependents within a module block.
builder.Services.AddScoped<Application.Interfaces.Taxation.ITaxComputationService, Application.Services.Taxation.TaxComputationService>();
builder.Services.AddScoped<Application.Interfaces.Taxation.ITaxSlabService, Application.Services.Taxation.TaxSlabService>();
builder.Services.AddScoped<Application.Interfaces.Taxation.ITaxDeclarationService, Application.Services.Taxation.TaxDeclarationService>();

// ===================== Recruitment Module =====================
builder.Services.AddScoped<Application.Interfaces.Recruitment.IJobOpeningService, Application.Services.Recruitment.JobOpeningService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.ICandidateService, Application.Services.Recruitment.CandidateService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.ICandidateApplicationService, Application.Services.Recruitment.CandidateApplicationService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.IInterviewScheduleService, Application.Services.Recruitment.InterviewScheduleService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.IRecruitmentDashboardService, Application.Services.Recruitment.RecruitmentDashboardService>();

// ===================== Onboarding Module =====================
builder.Services.AddScoped<Application.Interfaces.Onboarding.IOnboardingService, Application.Services.Onboarding.OnboardingService>();

// ===================== Communication Module =====================
builder.Services.AddScoped<Application.Interfaces.Communication.IAnnouncementService, Application.Services.Communication.AnnouncementService>();
builder.Services.AddScoped<Application.Interfaces.Communication.IEventService, Application.Services.Communication.EventService>();
builder.Services.AddScoped<Application.Interfaces.Communication.INotificationService, Application.Services.Communication.NotificationService>();

// ===================== Email (best-effort, no-op if Smtp:Host is unset) =====================
builder.Services.AddScoped<Application.Interfaces.IEmailSender, Application.Services.EmailSender>();

// ===================== Tasks + Employee Dashboard =====================
builder.Services.AddScoped<Application.Interfaces.Tasks.IEmployeeTaskService, Application.Services.Tasks.EmployeeTaskService>();
builder.Services.AddScoped<Application.Interfaces.Dashboard.IEmployeeDashboardService, Application.Services.Dashboard.EmployeeDashboardService>();

// ===================== User Management =====================
builder.Services.AddScoped<Application.Interfaces.Users.IUserService, Application.Services.Users.UserService>();

// ===================== Help & Support =====================
builder.Services.AddScoped<Application.Interfaces.Support.ISupportTicketService, Application.Services.Support.SupportTicketService>();
builder.Services.AddScoped<Application.Interfaces.Support.IFaqService, Application.Services.Support.FaqService>();

// ===================== Security (Role / Permission) =====================
builder.Services.AddScoped<Application.Interfaces.Roles.IRoleService, Application.Services.Roles.RoleService>();
builder.Services.AddScoped<Application.Interfaces.Permissions.IPermissionService, Application.Services.Permissions.PermissionService>();
builder.Services.AddScoped<Application.Interfaces.LoginHistory.ILoginHistoryService, Application.Services.LoginHistory.LoginHistoryService>();

// ===================== Loan & Advance (Phase 6) =====================
// Generic Repository/UnitOfWork - scoped to this module only, see
// Infrastructure/Interfaces/IRepository.cs for why the rest of the app's
// services keep injecting ApplicationDbContext directly instead.
builder.Services.AddScoped<Infrastructure.Interfaces.IUnitOfWork, Infrastructure.Repositories.UnitOfWork>();

builder.Services.AddScoped<Application.Interfaces.LoanAdvance.ILoanTypeService, Application.Services.LoanAdvance.LoanTypeService>();
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.IAdvanceTypeService, Application.Services.LoanAdvance.AdvanceTypeService>();
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.ILoanPolicyService, Application.Services.LoanAdvance.LoanPolicyService>();
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.ILoanCalculationService, Application.Services.LoanAdvance.LoanCalculationService>();
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.IEmployeeLoanService, Application.Services.LoanAdvance.EmployeeLoanService>();
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.IEmployeeAdvanceService, Application.Services.LoanAdvance.EmployeeAdvanceService>();
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.ILoanAdvanceAttachmentService, Application.Services.LoanAdvance.LoanAdvanceAttachmentService>();
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.ILoanAdvanceAuditLogService, Application.Services.LoanAdvance.LoanAdvanceAuditLogService>();
// Phase 8 - payroll recovery batch engine; PayrollBusinessService.GenerateAsync
// (registered above) depends on this, so it must be registered too.
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.IPayrollLoanRecoveryService, Application.Services.LoanAdvance.PayrollLoanRecoveryService>();
// Phase 14 - read-only dashboard/report aggregation; depends on
// IEmployeeLoanService/IEmployeeAdvanceService for PendingOnMe resolution.
builder.Services.AddScoped<Application.Interfaces.LoanAdvance.ILoanReportService, Application.Services.LoanAdvance.LoanReportService>();


builder.Services.AddHttpClient();

// ======================================================
// BACKGROUND SERVICES
// ======================================================
// Stale-pending Leave Application reminders/escalation notices - see
// API/BackgroundServices/LeaveEscalationService.cs. First (and currently
// only) hosted service in this API, so there is no prior registration
// pattern to match here.
builder.Services.AddHostedService<LeaveEscalationService>();

// Leave Policy Engine - automatic leave accrual (per LeaveType.
// AccrualFrequency) and January 1st year-end carry-forward - see
// API/BackgroundServices/LeaveAccrualService.cs.
builder.Services.AddHostedService<LeaveAccrualService>();

// Comp Off detection - "System-detected, HR-approved": scans recent
// Attendance rows for Holiday/WeekOff days worked beyond
// AttendancePolicy.CompOffEligibleExtraHours and creates a PendingReview
// CompOffCandidate for HR to Approve/Reject (see CompOffService) - never
// credits the balance itself. See
// API/BackgroundServices/CompOffDetectionService.cs.
builder.Services.AddHostedService<CompOffDetectionService>();

// Phase 15 - Loan & Advance reminders: stale pending-approval nudges plus
// upcoming/overdue EMI and installment reminders - see
// API/BackgroundServices/LoanAdvanceReminderService.cs.
builder.Services.AddHostedService<API.BackgroundServices.LoanAdvanceReminderService>();

// eSSL eTimeTrackLite1 direct-SQL attendance sync - see
// API/BackgroundServices/EsslAttendanceSyncBackgroundService.cs. No-ops
// every cycle unless EsslDatabase:Enabled = true, so it's always safe to
// leave registered even for a tenant that never configures eSSL.
builder.Services.AddHostedService<API.BackgroundServices.EsslAttendanceSyncBackgroundService>();

// ======================================================
// SWAGGER
// ======================================================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

// ======================================================
// MIDDLEWARE PIPELINE
// ======================================================

// Global Exception Middleware
app.UseGlobalExceptionMiddleware();

// Swagger - controlled by EnableSwagger (defaults to true, so local/dev
// behavior is unchanged unless a config sets it explicitly). Set to
// false in appsettings.Production.json so the IIS deployment doesn't
// expose the API surface/docs at the site root by default; flip it back
// to true there if you want Swagger reachable in production too.
var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger", true);

if (enableSwagger)
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP API V1");

        c.RoutePrefix = string.Empty;
    });
}

// HTTPS - the IIS site (see deployment guide) is bound to HTTP only
// (http://localhost:81), so forcing a redirect to HTTPS in Production
// would send every request to a binding that doesn't exist and break the
// API. Kept for Development, where the "https" launchSettings profile
// provides a real HTTPS endpoint to redirect to. If you later add an
// HTTPS binding in IIS, move this back outside the check.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Routing
app.UseRouting();

// CORS
//app.UseCors("AllowAngular");

app.UseCors("AllowMVC");

// Authentication
app.UseAuthentication();

// REMOVED: app.UseMiddleware<TenantMiddleware>(). This used to hard-reject
// (HTTP 400 "Tenant not provided") EVERY request outside /api/auth whenever
// the caller's JWT had an empty "TenantId" claim (e.g. a user row whose
// TenantId was never seeded) - which broke every endpoint in the API, not
// just tenant-aware ones, and most of the API doesn't even read TenantId.
// TenantService now resolves TenantId lazily, per-request, straight from
// IHttpContextAccessor (X-Tenant-ID header first, then the JWT claim) and
// simply returns "" if neither is present - never blocks the pipeline. See
// Infrastructure/Services/TenantService.cs. TenantMiddleware.cs itself is
// left in API/Middleware/ but is no longer registered/used anywhere; delete
// it directly on disk if you want it gone entirely (this session couldn't
// remove the file itself - see chat).

// Authorization
app.UseAuthorization();

// Controllers
app.MapControllers();

// ======================================================
// DATABASE SEEDER
// ======================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
                  .GetRequiredService<ApplicationDbContext>();

    await DbSeeder.SeedAsync(db);
}

// ======================================================
// eSSL SYNC LOCK RECOVERY (startup)
// ======================================================
// ROOT-CAUSE FIX: EsslAttendanceSyncService.IsSyncRunning is a persisted
// (DB-row) lock, cleared in a `finally` block when a sync run finishes
// normally - see EsslAttendanceSyncService.SyncAsync's remarks. That
// `finally` never gets a chance to run if the process hosting it dies
// mid-run instead of throwing a normal .NET exception: an IIS app pool
// recycle/stop (exactly what happens on every `dotnet publish` to this
// site), a server reboot, or the worker process being killed. The next
// person to click "Sync Now"/"Historical Import" then hits "A sync is
// already in progress for this tenant" - forever, since nothing is
// actually running to ever finish and clear it - until the UI's 30-minute
// stale-lock timer (EsslAttendanceSyncService.DefaultStaleLockMinutes)
// finally allows Reset Stuck Sync.
//
// This process just started. By definition, no eSSL sync from a PREVIOUS
// process instance can still be genuinely running - it died with that
// process. So any row still marked IsSyncRunning = true at this exact
// point is unconditionally stale, and it is always safe to clear it here
// rather than making an admin wait out the 30-minute timer (or use Reset
// Stuck Sync) after every redeploy/restart.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var staleLocks = await db.EsslAttendanceSyncStates
            .Where(s => s.IsSyncRunning)
            .ToListAsync();

        if (staleLocks.Count > 0)
        {
            foreach (var state in staleLocks)
            {
                state.IsSyncRunning = false;
                state.LastSyncStatus = "Interrupted";
                state.LastError = "Sync was interrupted by an application restart (IIS recycle/redeploy/server restart) before it could finish or record a result. The lock has been automatically cleared - you can safely click Sync Now / Historical Import again.";
                state.LastSyncCompletedAt = DateTime.Now;
            }

            await db.SaveChangesAsync();

            logger.LogWarning(
                "eSSL startup recovery: cleared {Count} stale IsSyncRunning lock(s) left over from before this application instance started (tenant IDs: {TenantIds}).",
                staleLocks.Count,
                string.Join(", ", staleLocks.Select(s => s.TenantId)));
        }
    }
    catch (Exception ex)
    {
        // Never let this best-effort cleanup stop the app from starting -
        // worst case, an admin still has the existing 30-minute stale-lock
        // fallback/Reset Stuck Sync button available.
        logger.LogError(ex, "eSSL startup recovery: failed to clear stale IsSyncRunning locks (non-fatal - continuing startup).");
    }
}

app.Run();