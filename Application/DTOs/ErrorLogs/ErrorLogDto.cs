using System;

namespace Application.DTOs.ErrorLogs
{
    /// <summary>
    /// Used for both the list row and the full details view - the Error Log
    /// screen shows a lighter subset of these fields in the table and
    /// everything in the details modal/page (see requirement #6). Kept as a
    /// single DTO rather than splitting List/Details since ErrorLog has no
    /// heavy fields (RequestBody/QueryParams are the only "big" ones and are
    /// omitted from the list serialization by the API's GetAllAsync
    /// projection anyway, not by having two DTOs).
    /// </summary>
    public class ErrorLogDto
    {
        public string Id { get; set; }
        public string? RequestId { get; set; }
        public string? CorrelationId { get; set; }

        public string ErrorMessage { get; set; }
        public string? ExceptionType { get; set; }
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }

        public string? Endpoint { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? Method { get; set; }

        public string? ModuleName { get; set; }
        public string? FeatureName { get; set; }

        // Only populated on the Details call - kept off the list response
        // to avoid shipping potentially large payloads/query strings for
        // every row on every page load.
        public string? RequestBody { get; set; }
        public string? QueryParams { get; set; }

        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? CompanyId { get; set; }

        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }

        public string? LogLevel { get; set; }

        public DateTime ErrorTime { get; set; }

        public bool IsResolved { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public string? ResolvedBy { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>Filter + server-side pagination request for the Error Log listing (requirement #5).</summary>
    public class ErrorLogFilterDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public string? ModuleName { get; set; }
        public string? FeatureName { get; set; }
        public string? UserName { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }

        /// <summary>null = all, true = resolved only, false = unresolved only.</summary>
        public bool? IsResolved { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>Body of POST api/ErrorLog/resolve (requirement #7). RequestBody-equivalent for marking one error resolved.</summary>
    public class ResolveErrorLogDto
    {
        public string Id { get; set; }
        public string? Remarks { get; set; }
    }
}
