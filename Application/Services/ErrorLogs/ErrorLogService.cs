using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Routing;
using Application.Interfaces.ErrorLog;
using Infrastructure.Data;

namespace Application.Services.ErrorLogs
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly ApplicationDbContext _db;

        public ErrorLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogExceptionAsync(Exception ex, HttpContext context, string requestId = null)
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

                Controller = context.GetRouteValue("controller")?.ToString()
                             ?? "Unknown",

                Action = context.GetRouteValue("action")?.ToString()
                         ?? "Unknown",

                Method = context.Request?.Method ?? "",

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
    }
}
