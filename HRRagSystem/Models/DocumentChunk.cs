namespace HRRagSystem.Models
{
    public class DocumentChunk
    {
        public string ChunkId { get; set; } = Guid.NewGuid().ToString();

        public string DocumentId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public int ChunkIndex { get; set; }

        public string Content { get; set; } = string.Empty;

        public int TokenCount { get; set; }

        public float[] Embedding { get; set; } = [];

        public string IngestionVersion { get; set; } = string.Empty;
    }
}
