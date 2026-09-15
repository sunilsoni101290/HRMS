using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    // Global safety net so APP (MVC)-layer exceptions that never touch the
    // API also land in the shared ErrorLog table - APP has no direct
    // database access, so without this, an exception thrown inside an APP
    // controller/view before (or instead of) any ApiService call would only
    // ever show up in server console/ILogger output, never in the ErrorLog
    // admin screen used for the rest of the system.
    //
    // Deliberately observes only - it never sets context.ExceptionHandled,
    // so it doesn't change any existing error-handling behavior (the
    // standard /Home/Error page, ApiSessionExpiredFilter's redirect-to-
    // Login for UnauthorizedAccessException, etc. all continue to run
    // exactly as before). It just reports the failure on the side, best
    // effort, and gets out of the way.
    //
    // Skips exceptions that already originated from (and were already
    // logged by) an API call:
    //   - ApiException / UnauthorizedAccessException: thrown by ApiService
    //     when the API itself returned an error response - that request
    //     already went through API/Middleware/ExceptionMiddleware and was
    //     logged there. Re-logging here would just duplicate the same
    //     failure under a different label.
    public class ClientErrorLoggingFilter : IAsyncExceptionFilter
    {
        private readonly IApiService _apiService;

        public ClientErrorLoggingFilter(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            var ex = context.Exception;

            if (ex == null || ex is ApiException || ex is UnauthorizedAccessException)
                return;

            try
            {
                var dto = new ClientErrorLogDto
                {
                    Message = ex.Message,
                    ExceptionType = ex.GetType().Name,
                    StackTrace = ex.ToString(),
                    InnerException = ex.InnerException?.ToString() ?? "No Inner Exception",
                    Controller = context.RouteData.Values["controller"]?.ToString() ?? "Unknown",
                    Action = context.RouteData.Values["action"]?.ToString() ?? "Unknown",
                    Url = context.HttpContext.Request?.Path.Value ?? ""
                };

                await _apiService.PostAsync<ClientErrorLogDto, object>("ClientErrorLog", dto);
            }
            catch
            {
                // Best-effort only - reporting the error must never itself
                // cause another error, and must never mask the original
                // exception from the rest of the pipeline.
            }
        }
    }
}
