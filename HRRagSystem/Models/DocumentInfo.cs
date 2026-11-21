namespace HRRagSystem.Models
{
    public class DocumentInfo
    {
        public string DocumentId { get; set; } = Guid.NewGuid().ToString();

        public string FileName { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public string FileHash { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime LastModifiedUtc { get; set; }

        public DateTime LastIngestedAtUtc { get; set; }

        public string IngestionVersion { get; set; } = string.Empty;

        public DocumentStatus Status { get; set; }
    }
}
