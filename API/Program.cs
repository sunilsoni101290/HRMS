using API.Middleware;
using Application.DTOs.Attendances;
using Application.Interfaces;
using Application.Interfaces.Attendances;
using Application.Interfaces.Auth;
using Application.Interfaces.Company;
using Application.Interfaces.EmployeeInterface;
using Application.Interfaces.ErrorLog;
using Application.Interfaces.JWT_TOKEN;
using Application.Interfaces.Leaves;
using Application.Interfaces.Masters;
using Application.Services;
using Application.Services.Attendances;
using Application.Services.Auth;
using Application.Services.CompanyService;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ======================================================
// DATABASE
// ======================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ERPConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);

            sqlOptions.CommandTimeout(120);
        }));

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

// ======================================================
// CONTROLLERS
// ======================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

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

            ClockSkew = TimeSpan.Zero
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
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantBusinessService, TenantBusinessService>();
builder.Services.AddScoped<IAppFeatureService, AppFeatureService>();
builder.Services.AddScoped<IFinancialYearService, FinancialYearService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IBiometricDeviceService, BiometricDeviceService>();
builder.Services.AddScoped<IBiometricSyncService,BiometricSyncService>();
builder.Services.AddScoped<IAttendanceProcessorService,AttendanceProcessorService>();
builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<ILeaveApplicationService, LeaveApplicationService>();
//builder.Services.AddScoped<ILeaveDashboardService, LeaveDashboardService>();
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
builder.Services.AddScoped<Application.Interfaces.Payroll.IPayrollBusinessService, Application.Services.PayrollService.PayrollBusinessService>();

// ===================== Recruitment Module =====================
builder.Services.AddScoped<Application.Interfaces.Recruitment.IJobOpeningService, Application.Services.Recruitment.JobOpeningService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.ICandidateService, Application.Services.Recruitment.CandidateService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.ICandidateApplicationService, Application.Services.Recruitment.CandidateApplicationService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.IInterviewScheduleService, Application.Services.Recruitment.InterviewScheduleService>();
builder.Services.AddScoped<Application.Interfaces.Recruitment.IRecruitmentDashboardService, Application.Services.Recruitment.RecruitmentDashboardService>();

// ===================== Communication Module =====================
builder.Services.AddScoped<Application.Interfaces.Communication.IAnnouncementService, Application.Services.Communication.AnnouncementService>();
builder.Services.AddScoped<Application.Interfaces.Communication.IEventService, Application.Services.Communication.EventService>();
builder.Services.AddScoped<Application.Interfaces.Communication.INotificationService, Application.Services.Communication.NotificationService>();

// ===================== Tasks + Employee Dashboard =====================
builder.Services.AddScoped<Application.Interfaces.Tasks.IEmployeeTaskService, Application.Services.Tasks.EmployeeTaskService>();
builder.Services.AddScoped<Application.Interfaces.Dashboard.IEmployeeDashboardService, Application.Services.Dashboard.EmployeeDashboardService>();


builder.Services.AddHttpClient();

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

// Swagger
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP API V1");

    c.RoutePrefix = string.Empty;
});

// HTTPS
app.UseHttpsRedirection();

// Routing
app.UseRouting();

// CORS
app.UseCors("AllowAngular");

// Authentication
app.UseAuthentication();

// Custom Tenant Middleware
app.UseMiddleware<TenantMiddleware>();

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

app.Run();