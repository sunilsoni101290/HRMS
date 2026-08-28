using System;

namespace APP.Models.DTOs
{
    /// <summary>Mirrors Application.DTOs.ErrorLogs.ErrorLogDto (API) - see that file's remarks.</summary>
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

        public bool? IsResolved { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ResolveErrorLogDto
    {
        public string Id { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>Mirrors Application.DTOs.Employee.PagedResult&lt;T&gt; - server-side pagination envelope used across newer list screens.</summary>
    public class PagedResult<T>
    {
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<T> Data { get; set; }
    }
}
