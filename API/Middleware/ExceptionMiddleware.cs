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

                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Something went wrong",
                    error = ex.Message
                });
            }
        }
    }
}
