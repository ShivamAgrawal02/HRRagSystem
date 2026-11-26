using HRRagSystem.Models;
using HRRagSystem.Services.Interfaces;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System.Collections;
using System.Runtime.InteropServices;
namespace HRRagSystem.Services.Implementation
{
    public class VectorRepository : IVectorRepository
    {
        private readonly QdrantClient _qdrantClient;
        private readonly string _collectionName;
        private readonly ulong _vectorSize;
        private readonly ILogger<VectorRepository> _logger;

        public VectorRepository(ILogger<VectorRepository> logger, IConfiguration configuration ,QdrantClient qdrantClient)
        {
            _logger = logger;
            _collectionName = configuration["Qdrant:CollectionName"] ?? throw new ArgumentNullException("Qdrant:CollectionName");
            _vectorSize = ulong.Parse(configuration["Qdrant:VectorSize"] ?? throw new ArgumentNullException("Qdrant:VectorSize"));
            _qdrantClient = qdrantClient;

        }
       
        public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
        {
            if(await _qdrantClient.CollectionExistsAsync(_collectionName, cancellationToken))
            {
                _logger.LogInformation($"Collection '{_collectionName}' already exists.");
                return;
            }
            await _qdrantClient.CreateCollectionAsync(_collectionName,
                new VectorParams
                {
                    Size = _vectorSize,
                    Distance = Distance.Cosine
                   
                },
                cancellationToken: cancellationToken);
            _logger.LogInformation($"Collection '{_collectionName}' created successfully.");
        }

        public Task<IReadOnlyList<ScoredPoint>> SearchSimilarAsync(float[] queryEmbedding, ulong limit = 5, CancellationToken cancellationToken = default)
        {
            if(queryEmbedding == null || queryEmbedding.Length != (int)_vectorSize)
            {
                _logger.LogError("Query embedding is null or has an invalid size.");
                throw new ArgumentException("Query embedding is null or has an invalid size.", nameof(queryEmbedding));
            }
            var result = _qdrantClient.QueryAsync(
                collectionName: _collectionName,
                query: queryEmbedding,
                limit: limit,
                cancellationToken: cancellationToken);
            return result;  
        }

        public async Task UpsertChunksAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            if (chunks == null || chunks.Count == 0)
                return;

            var points = new List<PointStruct>();
            foreach(var chunk in chunks)
            {                
                if(chunk.Embedding == null || chunk.Embedding.Length != (int)_vectorSize)
                {
                    _logger.LogError($"Chunk {chunk.ChunkId} has an invalid embedding size.");
                    throw new ArgumentException($"Chunk {chunk.ChunkId} has an invalid embedding size.");
                }
                var point = new PointStruct
                {
                    Id = Guid.Parse(chunk.ChunkId),
                    Vectors = chunk.Embedding,
                    Payload = 
                    {
                        ["DocumentId"] = chunk.DocumentId,
                        ["FileName"] = chunk.FileName,
                        ["ChunkIndex"] = chunk.ChunkIndex,
                        ["Content"] = chunk.Content,
                        ["TokenCount"] = chunk.TokenCount,
                        ["IngestionVersion"] = chunk.IngestionVersion
                    }
                };
                points.Add(point);
            }
            
            var result =await _qdrantClient.UpsertAsync(
                collectionName: _collectionName,
                points: points,
                wait: true,
                cancellationToken: cancellationToken);
            return;
        }
        public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(documentId))
            {
                _logger.LogError("Document ID cannot be null or whitespace.");
                throw new ArgumentException("Document ID cannot be null or whitespace.", nameof(documentId));
            }

            // Create a filter to match the documentId
            var filter = new Filter
            {
                Must =
                {
                    Conditions.MatchKeyword("DocumentId", documentId)
                }
                
            };
            var result = await  _qdrantClient.DeleteAsync(
                            collectionName: _collectionName,
                            filter: filter,
                            wait: true,
                            cancellationToken: cancellationToken );
            
        }
    }
}
