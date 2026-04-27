using Application.Common.Exceptions;
using Application.Common.Responses;
using Domain.Entities;
using Infrastructure;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, ApplicationDbContext db)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            // 🔹 CorrelationId
            var correlationId = Guid.NewGuid().ToString();

            // 🔹 Read Request Body
            request.EnableBuffering();

            string requestBody = "";
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            // 🔹 Get User Info
            var userId = context.User?.FindFirst("UserId")?.Value;
            var userName = context.User?.Identity?.Name;

            // 🔹 Client Info
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // 🔹 Prepare Log
            var log = new ApiRequestLog
            {
                Id = Guid.NewGuid().ToString(),
                CorrelationId = correlationId,

                Endpoint = request.Path,
                Controller = context.GetRouteValue("controller")?.ToString(),
                Action = context.GetRouteValue("action")?.ToString(),
                Method = request.Method,

                RequestBody = MaskSensitiveData(requestBody),
                QueryParams = request.QueryString.ToString(),
                Headers = JsonConvert.SerializeObject(request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())),

                UserId = userId,
                UserName = userName,

                CompanyId = context.User?.FindFirst("CompanyId")?.Value,
                BranchId = context.User?.FindFirst("BranchId")?.Value,

                IPAddress = ip,
                UserAgent = userAgent,
                Device = GetDevice(userAgent),

                IsAuthenticated = context.User?.Identity?.IsAuthenticated ?? false,

                RequestTime = DateTime.UtcNow,
                IsSuccess = false // default
            };

            // 🔹 Capture Response
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);

                stopwatch.Stop();

                var responseText = await ReadResponseBody(responseBody);

                log.ResponseBody = Truncate(responseText, 5000);
                log.StatusCode = context.Response.StatusCode;
                log.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                log.ResponseTime = DateTime.UtcNow;
                log.IsSuccess = context.Response.StatusCode < 400;

                db.ApiRequestLogs.Add(log);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                log.IsSuccess = false;
                log.ErrorMessage = ex.Message;
                log.ExceptionStackTrace = ex.StackTrace;
                log.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                log.ResponseTime = DateTime.UtcNow;

                db.ApiRequestLogs.Add(log);

                // 🔥 ErrorLog Save
                var error = new ErrorLog
                {
                    Id = Guid.NewGuid().ToString(),
                    CorrelationId = correlationId,
                    RequestId = log.Id,

                    ErrorMessage = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    StackTrace = ex.StackTrace,
                    InnerException = ex.InnerException?.Message,

                    Endpoint = log.Endpoint,
                    Controller = log.Controller,
                    Action = log.Action,
                    Method = log.Method,

                    RequestBody = log.RequestBody,
                    QueryParams = log.QueryParams,

                    UserId = log.UserId,
                    UserName = log.UserName,

                    CompanyId = log.CompanyId,
                    BranchId = log.BranchId,

                    IPAddress = log.IPAddress,
                    UserAgent = log.UserAgent,

                    LogLevel = "Error",
                    ErrorTime = DateTime.UtcNow
                };

                db.ErrorLogs.Add(error);

                await db.SaveChangesAsync();

                throw;
            }
            finally
            {
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        // ==============================
        // 🔧 Helpers
        // ==============================

        private async Task<string> ReadResponseBody(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(stream).ReadToEndAsync();
            stream.Seek(0, SeekOrigin.Begin);
            return text;
        }

        private string MaskSensitiveData(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return input
                .Replace("password", "***")
                .Replace("Password", "***");
        }

        private string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private string GetDevice(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";

            if (userAgent.ToLower().Contains("mobile"))
                return "Mobile";

            if (userAgent.ToLower().Contains("postman"))
                return "API";

            return "Web";
        }

    }
}
