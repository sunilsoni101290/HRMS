using Application.Common.Exceptions;
using Application.DTOs.Employee;
using Application.DTOs.ErrorLogs;
using Application.Interfaces.ErrorLog;
using Domain.Entities;
using Domain.Helper;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.ErrorLogs
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly ApplicationDbContext _db;

        public ErrorLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ==================================================================
        // CENTRALIZED LOGGING (unchanged behavior - see requirement #8:
        // logging an error must never itself cause another error)
        // ==================================================================

        public async Task LogExceptionAsync(Exception ex, HttpContext context, string requestId = null)
        {
            try
            {
                string requestBody = "";

                try
                {
                    using (var reader = new StreamReader(
                        context.Request.Body,
                        Encoding.UTF8,
                        detectEncodingFromByteOrderMarks: false,
                        leaveOpen: true))
                    {
                        requestBody = await reader.ReadToEndAsync();

                        context.Request.Body.Position = 0;
                    }
                }
                catch
                {
                    requestBody = "Unable to read request body";
                }

                var controller = context.GetRouteValue("controller")?.ToString() ?? "Unknown";
                var (module, feature) = InferModuleAndFeature(controller);

                var log = new ErrorLog
                {
                    // 🔹 Identification
                    Id = IDManager.GetNewId(new ErrorLog()),

                    RequestId = requestId ?? "",

                    CorrelationId = context.Items["CorrelationId"]?.ToString()
                                    ?? Guid.NewGuid().ToString(),

                    // 🔹 Error Info
                    ErrorMessage = ex.Message ?? "",

                    ExceptionType = ex.GetType().Name ?? "",

                    StackTrace = ex.ToString(),

                    InnerException = ex.InnerException?.ToString()
                                     ?? "No Inner Exception",

                    // 🔹 API Context
                    Endpoint = context.Request?.Path.Value ?? "",

                    Controller = controller,

                    Action = context.GetRouteValue("action")?.ToString()
                             ?? "Unknown",

                    Method = context.Request?.Method ?? "",

                    // 🔹 Categorization
                    ModuleName = module,
                    FeatureName = feature,

                    // 🔹 Request Snapshot
                    RequestBody = requestBody,

                    QueryParams = context.Request?.QueryString.ToString() ?? "",

                    // 🔹 User Context
                    UserId = context.User?.FindFirst("UserId")?.Value ?? "",

                    UserName = context.User?.Identity?.Name ?? "",

                    // 🔹 Multi-Tenant
                    CompanyId = context.User?.FindFirst("CompanyId")?.Value ?? "",

                    // 🔹 Client Info
                    IPAddress = context.Connection?.RemoteIpAddress?.ToString() ?? "",

                    UserAgent = context.Request?.Headers["User-Agent"].ToString() ?? "",

                    // 🔹 Severity
                    LogLevel = "Error",

                    // 🔹 Time
                    ErrorTime = DateTime.UtcNow,

                    // 🔹 Resolution
                    IsResolved = false,

                    ResolvedOn = null,

                    ResolvedBy = "",

                    // 🔹 Extra
                    Remarks = ""
                };

                _db.ErrorLogs.Add(log);

                await _db.SaveChangesAsync();
            }
            catch (Exception)
            {
                return;
            }
        }

        // Same purpose as LogExceptionAsync, for exceptions caught OUTSIDE
        // an HTTP request (BiometricAgent worker cycles, background
        // services, etc.) - see requirement #9 (Biometric Device
        // Integration errors: connection failure, invalid IP/port, SDK/COM
        // registration issue, timeout, invalid device response...) and #10
        // (HRMS service-layer exceptions in general). Deliberately mirrors
        // LogExceptionAsync's try/catch-and-swallow shape so a logging
        // failure can never cascade into a second error.
        public async Task LogAsync(
            Exception ex,
            string? module = null,
            string? feature = null,
            string? controller = null,
            string? action = null,
            string? userId = null,
            string? userName = null,
            string? tenantId = null,
            string? correlationId = null)
        {
            try
            {
                var (inferredModule, inferredFeature) = InferModuleAndFeature(controller);

                var log = new ErrorLog
                {
                    Id = IDManager.GetNewId(new ErrorLog()),

                    RequestId = "",
                    CorrelationId = correlationId ?? Guid.NewGuid().ToString(),

                    ErrorMessage = ex.Message ?? "",
                    ExceptionType = ex.GetType().Name ?? "",
                    StackTrace = ex.ToString(),
                    InnerException = ex.InnerException?.ToString() ?? "No Inner Exception",

                    Endpoint = "",
                    Controller = controller ?? "",
                    Action = action ?? "",
                    Method = "",

                    ModuleName = module ?? inferredModule,
                    FeatureName = feature ?? inferredFeature,

                    RequestBody = "",
                    QueryParams = "",

                    UserId = userId ?? "",
                    UserName = userName ?? "",
                    CompanyId = tenantId ?? "",

                    IPAddress = "",
                    UserAgent = "",

                    LogLevel = "Error",
                    ErrorTime = DateTime.UtcNow,

                    IsResolved = false,
                    ResolvedOn = null,
                    ResolvedBy = "",
                    Remarks = ""
                };

                _db.ErrorLogs.Add(log);

                await _db.SaveChangesAsync();
            }
            catch
            {
                // Never let logging failure crash the caller.
            }
        }

        // ==================================================================
        // ERROR LOG MANAGEMENT SCREEN (System Configurator / Super Admin)
        // ==================================================================

        public async Task<PagedResult<ErrorLogDto>> GetAllAsync(ErrorLogFilterDto filter, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var query = _db.ErrorLogs.AsNoTracking().AsQueryable();

            if (filter.DateFrom.HasValue)
                query = query.Where(x => x.ErrorTime >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
            {
                // Inclusive of the whole "To" day.
                var to = filter.DateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.ErrorTime < to);
            }

            if (!string.IsNullOrWhiteSpace(filter.ModuleName))
                query = query.Where(x => x.ModuleName == filter.ModuleName);

            if (!string.IsNullOrWhiteSpace(filter.FeatureName))
                query = query.Where(x => x.FeatureName == filter.FeatureName);

            if (!string.IsNullOrWhiteSpace(filter.UserName))
                query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));

            if (!string.IsNullOrWhiteSpace(filter.ErrorMessage))
                query = query.Where(x => x.ErrorMessage != null && x.ErrorMessage.Contains(filter.ErrorMessage));

            if (!string.IsNullOrWhiteSpace(filter.Controller))
                query = query.Where(x => x.Controller == filter.Controller);

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(x => x.Action == filter.Action);

            if (filter.IsResolved.HasValue)
                query = query.Where(x => x.IsResolved == filter.IsResolved.Value);

            var total = await query.CountAsync();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 500 ? 20 : filter.PageSize;

            // Inline projection (not a call to the static ToDto helper) -
            // EF Core can only translate an expression tree it can read
            // into SQL, not an arbitrary C# method call. RequestBody/
            // QueryParams are deliberately left off the list projection
            // (see ErrorLogDto's remark) - GetByIdAsync includes them.
            var entities = await query
                .OrderByDescending(x => x.ErrorTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = entities.Select(x => ToDto(x, includeHeavyFields: false)).ToList();

            return new PagedResult<ErrorLogDto>
            {
                TotalRecords = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = items
            };
        }

        public async Task<ErrorLogDto?> GetByIdAsync(string id, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.View);

            var entity = await _db.ErrorLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);

            return entity == null ? null : ToDto(entity, includeHeavyFields: true);
        }

        public async Task<bool> ResolveAsync(ResolveErrorLogDto dto, string actingUserId)
        {
            await EnsurePermissionAsync(actingUserId, Actions.Edit);

            var entity = await _db.ErrorLogs.FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                return false;

            var resolvedByName = await _db.Users
                .Where(u => u.Id == actingUserId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? actingUserId;

            // Preserve the original error information (requirement #7) -
            // only the resolution fields are touched, nothing else.
            entity.IsResolved = true;
            entity.ResolvedOn = DateTime.UtcNow;
            entity.ResolvedBy = resolvedByName;
            entity.Remarks = dto.Remarks ?? entity.Remarks;

            await _db.SaveChangesAsync();

            return true;
        }

        // ==================================================================
        // PERMISSION CHECK - same shape as
        // LoanAdvanceAuditLogService.EnsurePermissionAsync (the codebase's
        // existing "HasPermission(FeatureId, Action)" convention): a real,
        // data-driven RolePermission/Permission check against
        // AppFeatureConstants.ERROR_LOG, not a hard-coded role-name string.
        // In practice only Super Admin/System Configurator ever hold this
        // permission (see DbSeeder.ReconcilePermissionsAsync's
        // fullAccessRoles loop), but a tenant could grant it to another
        // role later without any code change here.
        // ==================================================================

        private async Task EnsurePermissionAsync(string? actingUserId, string action)
        {
            if (string.IsNullOrEmpty(actingUserId))
                throw new UnauthorizedException("You are not authorized to view the error log.");

            var allowed = await (
                from ur in _db.UserRoles.AsNoTracking()
                join rp in _db.RolePermissions.AsNoTracking().Where(x => x.IsAllowed) on ur.RoleId equals rp.RoleId
                join p in _db.Permissions.AsNoTracking().Where(x =>
                        x.FeatureId == AppFeatureConstants.ERROR_LOG && x.Action == action)
                    on rp.PermissionId equals p.Id
                where ur.UserId == actingUserId
                select p.Id
            ).AnyAsync();

            if (!allowed)
                throw new UnauthorizedException("You are not authorized to view the error log.");
        }

        // Best-effort categorization purely for filtering/scanning - never
        // required, never blocks logging if it can't determine anything.
        private static (string? Module, string? Feature) InferModuleAndFeature(string? controller)
        {
            if (string.IsNullOrWhiteSpace(controller))
                return (null, null);

            var c = controller.Trim();

            if (c.StartsWith("Biometric", StringComparison.OrdinalIgnoreCase))
                return ("Biometric Device Integration", c);

            return ("HRMS", c);
        }

        private static ErrorLogDto ToDto(ErrorLog x, bool includeHeavyFields)
        {
            return new ErrorLogDto
            {
                Id = x.Id,
                RequestId = x.RequestId,
                CorrelationId = x.CorrelationId,

                ErrorMessage = x.ErrorMessage,
                ExceptionType = x.ExceptionType,
                StackTrace = x.StackTrace,
                InnerException = x.InnerException,

                Endpoint = x.Endpoint,
                Controller = x.Controller,
                Action = x.Action,
                Method = x.Method,

                ModuleName = x.ModuleName,
                FeatureName = x.FeatureName,

                RequestBody = includeHeavyFields ? x.RequestBody : null,
                QueryParams = includeHeavyFields ? x.QueryParams : null,

                UserId = x.UserId,
                UserName = x.UserName,
                CompanyId = x.CompanyId,

                IPAddress = x.IPAddress,
                UserAgent = x.UserAgent,

                LogLevel = x.LogLevel,
                ErrorTime = x.ErrorTime,

                IsResolved = x.IsResolved,
                ResolvedOn = x.ResolvedOn,
                ResolvedBy = x.ResolvedBy,
                Remarks = x.Remarks
            };
        }
    }
}
