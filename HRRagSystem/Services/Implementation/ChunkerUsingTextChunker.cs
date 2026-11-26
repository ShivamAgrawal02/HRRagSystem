using HRRagSystem.Models;
using HRRagSystem.Services.Interfaces;
using Microsoft.SemanticKernel.Text;
using System.Runtime.InteropServices;


namespace HRRagSystem.Services.Implementation
{
    public class ChunkerUsingTextChunker : IChunker
    {
        private readonly IEmbeddingGenerator _embeddingGenerator;
        private readonly ILogger<ChunkerUsingTextChunker> _logger;

        private const string IngestionVersion = "v1";
        private readonly int _maxLinesChunkSize = 100;         
        private readonly int _maxParagraphChunkSize = 200; // Maximum tokens per chunk
        private readonly int _OverlapParagraphSize = 20; // Maximum tokens per chunk
        public ChunkerUsingTextChunker(ILogger<ChunkerUsingTextChunker> logger, IEmbeddingGenerator embeddingGenerator)
        {
            _logger = logger;
            _embeddingGenerator = embeddingGenerator;
        }
    
        private static int EstimateTokens(string text)
        {
            return text.Length / 4;
        }
        
        public async Task<List<DocumentChunk>> ChunkDocumentAsync(string cleanedText, DocumentInfo document, CancellationToken cancellation = default)
        {
            if(string.IsNullOrWhiteSpace(cleanedText))
            {
                _logger.LogWarning("Received empty text for chunking.");
                return new List<DocumentChunk>();
            }
            
            //create lines
            var lines = TextChunker.SplitPlainTextLines(cleanedText,
                maxTokensPerLine:_maxLinesChunkSize);
           
            //create paragraphs from those lines
            var paragraphs = TextChunker.SplitPlainTextParagraphs(lines,
                maxTokensPerParagraph: _maxParagraphChunkSize,
               overlapTokens:_OverlapParagraphSize);

            //create a list to store our chunks and return them
            var chunks = new List<DocumentChunk>(paragraphs.Count);

            // loop every para from the list of paragraphs and create a DocumentChunk for each one, then add it to the list of chunks
            for (int i = 0; i < paragraphs.Count; i++)
            {
                
                chunks.Add(new DocumentChunk
                {
                    DocumentId = document.DocumentId,
                    FileName = document.FileName,   // adjust to your DocumentInfo property name
                    ChunkIndex = i,
                    Content = paragraphs[i],
                    TokenCount = EstimateTokens(paragraphs[i]),
                    Embedding = await _embeddingGenerator.GenerateEmbeddingAsync(paragraphs[i]),
                    IngestionVersion = IngestionVersion
                });
            }

            _logger.LogInformation("Created {Count} chunks for document {DocumentId}",
                chunks.Count, document.DocumentId);

            return chunks;
        }
    }
}
