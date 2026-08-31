namespace APP.Models.DTOs
{
    // Shape of the "exists" payload returned by
    // API Employee/check-employee-code (ApiResponse<object>.Data = new { exists }).
    // Newtonsoft's default contract resolver matches JSON property names
    // case-insensitively, so this binds correctly against the API's
    // lowercase "exists" key without needing a [JsonProperty] attribute.
    public class EmployeeCodeExistsDto
    {
        public bool Exists { get; set; }
    }
}
