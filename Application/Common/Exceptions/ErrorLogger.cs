using Domain.Entities;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common.Exceptions
{
    public class ErrorLogger
    {
        private readonly ApplicationDbContext _db;

        public ErrorLogger(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(
            Exception ex,
            string endpoint = null,
            string method = null,
            string controller = null,
            string action = null,
            string requestId = null,
            string correlationId = null,
            string userId = null,
            string userName = null,
            string companyId = null,
            string branchId = null,
            string ipAddress = null,
            string userAgent = null,
            string requestBody = null,
            string queryParams = null,
            string logLevel = "Error"
        )
        {
            try
            {
                var log = new ErrorLog
                {
                    Id = Guid.NewGuid().ToString(),

                    RequestId = requestId,
                    CorrelationId = correlationId,

                    // 🔹 Error Info
                    ErrorMessage = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException?.Message,

                    // 🔹 API Context
                    Endpoint = endpoint,
                    Method = method,
                    Controller = controller,
                    Action = action,

                    // 🔹 Request Snapshot
                    RequestBody = requestBody,
                    QueryParams = queryParams,

                    // 🔹 User Context
                    UserId = userId,
                    UserName = userName,

                    // 🔹 Multi-Tenant
                    CompanyId = companyId,
                    BranchId = branchId,

                    // 🔹 Client Info
                    IPAddress = ipAddress,
                    UserAgent = userAgent,

                    // 🔹 Severity
                    LogLevel = logLevel,

                    // 🔹 Time
                    ErrorTime = DateTime.UtcNow,

                    // 🔹 Default Flags
                    IsResolved = false
                };

                _db.ErrorLogs.Add(log);
                await _db.SaveChangesAsync();
            }
            catch
            {
                // 🔥 VERY IMPORTANT:
                // Never crash app due to logging failure
                // Optionally log to file / console here
            }
        }
    }
}
