namespace Application.DTOs.LoanAdvance
{
    public class LoanAdvanceAttachmentDto
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>1=Loan, 2=Advance - see Domain.Enums.EnumExtensions.LoanAttachmentEntityType.</summary>
        public int EntityType { get; set; }
        public string EntityId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }

        public string UploadedBy { get; set; } = string.Empty;
        public string? UploadedByName { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    /// <summary>Multipart-form upload request - EntityId/EntityType come from the route, the file from IFormFile at the API layer.</summary>
    public class LoanAdvanceAttachmentUploadDto
    {
        public int EntityType { get; set; }
        public string EntityId { get; set; } = string.Empty;
    }
}
