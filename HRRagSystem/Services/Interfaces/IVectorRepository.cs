using HRRagSystem.Models;
using Qdrant.Client.Grpc;

namespace HRRagSystem.Services.Interfaces
{
    public interface IVectorRepository
    {
        Task EnsureCollectionAsync(
            CancellationToken cancellationToken = default);

        Task UpsertChunksAsync(
            IReadOnlyList<DocumentChunk> chunks,
            CancellationToken cancellationToken = default);

        Task DeleteDocumentAsync(
            string documentId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ScoredPoint>> SearchSimilarAsync(
            float[] queryEmbedding,
            ulong limit = 5,
            CancellationToken cancellationToken = default);

    }
}
