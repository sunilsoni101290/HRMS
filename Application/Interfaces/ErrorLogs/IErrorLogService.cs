using Application.DTOs.Employee;
using Application.DTOs.ErrorLogs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces.ErrorLog
{
    public interface IErrorLogService
    {
        /// <summary>
        /// Original centralized-logging entry point, called by
        /// API/Middleware/ExceptionMiddleware for every unhandled
        /// exception. Never throws - a logging failure must never take
        /// down the request that triggered it.
        /// </summary>
        Task LogExceptionAsync(Exception ex, HttpContext context, string requestId = null);

        /// <summary>
        /// Same purpose as LogExceptionAsync above, for code that catches
        /// and swallows an exception OUTSIDE of an HTTP request - background
        /// services, the BiometricAgent Worker's poll cycle, scheduled
        /// jobs, etc. (there is no HttpContext to pull Controller/Action/
        /// IPAddress/UserAgent from in those cases). Never throws.
        /// </summary>
        Task LogAsync(
            Exception ex,
            string? module = null,
            string? feature = null,
            string? controller = null,
            string? action = null,
            string? userId = null,
            string? userName = null,
            string? tenantId = null,
            string? correlationId = null);

        /// <summary>Server-side filtered/paginated listing for the Error Log management screen (requirement #5). Permission-checked (System Configurator/Super Admin, or any role explicitly granted View on AppFeatureConstants.ERROR_LOG).</summary>
        Task<PagedResult<ErrorLogDto>> GetAllAsync(ErrorLogFilterDto filter, string actingUserId);

        /// <summary>Full details for one error (requirement #6). Same permission check as GetAllAsync.</summary>
        Task<ErrorLogDto?> GetByIdAsync(string id, string actingUserId);

        /// <summary>Marks one error resolved - sets IsResolved/ResolvedBy/ResolvedOn, preserves the original error data (requirement #7). Requires Edit permission on AppFeatureConstants.ERROR_LOG.</summary>
        Task<bool> ResolveAsync(ResolveErrorLogDto dto, string actingUserId);
    }
}
