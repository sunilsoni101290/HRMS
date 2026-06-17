namespace APP.Helpers
{
    public class ApiException : Exception
    {
        public int StatusCode { get; }

        public string ResponseContent { get; }

        public ApiException(
            string message,
            int statusCode,
            string responseContent)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseContent = responseContent;
        }
    }
}
