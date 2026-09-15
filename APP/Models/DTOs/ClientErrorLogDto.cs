namespace APP.Models.DTOs
{
    // Mirrors Application.DTOs.ErrorLogs.ClientErrorLogDto field-for-field.
    // APP has no direct reference to entities/DTOs it doesn't already mirror
    // locally (it only ever talks to the API over HTTP via IApiService), so
    // this local copy - not the Application-layer one - is what gets
    // serialized and posted to POST api/ClientErrorLog.
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
