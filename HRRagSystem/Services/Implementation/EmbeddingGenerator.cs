using HRRagSystem.Services.Interfaces;
using Mscc.GenerativeAI.Microsoft;
using Microsoft.Extensions.AI;

namespace HRRagSystem.Services.Implementation
{
    public class EmbeddingGenerator : Interfaces.IEmbeddingGenerator
    {
        private readonly ILogger<EmbeddingGenerator> _logger;
        private readonly string _apiKey;
        private readonly string _model;

        IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        public EmbeddingGenerator(ILogger<EmbeddingGenerator> logger, IConfiguration configuration)
        {
            _logger = logger;
            _apiKey = configuration["GeminiApiKey"];
            _model = configuration["GeminiModel"];
            _embeddingGenerator = new GeminiEmbeddingGenerator(
                apiKey: _apiKey,
                model: _model);
        }
        public  async Task<float[]> GenerateEmbeddingAsync(string sentence)
        {
           
            var generatedEmbedding = await _embeddingGenerator.GenerateAsync(
                sentence,
                new EmbeddingGenerationOptions());
            return generatedEmbedding.Vector.ToArray();
        }
    }
}
