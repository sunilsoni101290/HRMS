using Application.Common.Exceptions;
using Application.Common.Responses;
using System.Net;
using System.Text.Json;

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

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            string errorCode = "SERVER_ERROR";

            switch (ex)
            {
                case NotFoundException e:
                    statusCode = HttpStatusCode.NotFound;
                    errorCode = e.ErrorCode;
                    break;

                case BadRequestException e:
                    statusCode = HttpStatusCode.BadRequest;
                    errorCode = e.ErrorCode;
                    break;

                case UnauthorizedException e:
                    statusCode = HttpStatusCode.Unauthorized;
                    errorCode = e.ErrorCode;
                    break;

                case ForbiddenException e:
                    statusCode = HttpStatusCode.Forbidden;
                    errorCode = e.ErrorCode;
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(
                ApiResponse<string>.Fail(ex.Message, errorCode)
            );

            await context.Response.WriteAsync(result);
        }
    }
}
