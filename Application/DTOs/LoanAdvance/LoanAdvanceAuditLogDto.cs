namespace Application.DTOs.LoanAdvance
{
    public class LoanAdvanceAuditLogDto
    {
        public string Id { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string? PerformedByName { get; set; }
        public DateTime PerformedOn { get; set; }
        public string? IpAddress { get; set; }
    }
}
