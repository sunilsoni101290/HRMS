namespace Application.DTOs.ErrorLogs
{
    // Payload posted by APP's ClientErrorLoggingFilter for exceptions that
    // occur entirely inside the MVC (APP) layer and never reach the API -
    // e.g. a view-rendering error, a null-reference in a controller action
    // before any ApiService call, a filter/model-binding failure. APP has
    // no direct database access, so these would otherwise never make it
    // into the ErrorLog table at all. Deliberately a small, flat shape -
    // just enough context to triage from the ErrorLog admin screen.
    public class ClientErrorLogDto
    {
        public string? Message { get; set; }
        public string? ExceptionType { get; set; }
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? Url { get; set; }
    }
}
