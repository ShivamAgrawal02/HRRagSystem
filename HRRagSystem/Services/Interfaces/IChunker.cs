using HRRagSystem.Models;

namespace HRRagSystem.Services.Interfaces
{
    public interface IChunker
    {
        Task<List<DocumentChunk>> ChunkDocumentAsync(string cleanedText, DocumentInfo document, CancellationToken cancellationToken=default);
    }
}
