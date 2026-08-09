using Application.Common.Exceptions;
using Application.Common.Responses;
using Application.Interfaces.ErrorLog;
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

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IErrorLogService errorService)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // 🔥 Error Save
                await errorService.LogExceptionAsync(ex, context);

                // Map the Application.Common.Exceptions hierarchy (added
                // for the Loan & Advance module, Phase 6) to their proper
                // HTTP status codes instead of always returning 500 - a
                // NotFoundException/BadRequestException/
                // UnauthorizedException/ForbiddenException reaching this
                // middleware is an EXPECTED outcome (bad input / missing
                // record / auth failure), not a server fault. Every other
                // exception type (including plain System.Exception, as
                // thrown by most pre-existing services) keeps the original
                // 500 behavior unchanged - this is additive, not a
                // behavior change for any other module.
                context.Response.StatusCode = ex switch
                {
                    NotFoundException => (int)HttpStatusCode.NotFound,
                    BadRequestException => (int)HttpStatusCode.BadRequest,
                    UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                    UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                    ForbiddenException => (int)HttpStatusCode.Forbidden,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = context.Response.StatusCode == (int)HttpStatusCode.InternalServerError
                        ? "Something went wrong"
                        : ex.Message,
                    error = ex.Message
                });
            }
        }
    }
}
