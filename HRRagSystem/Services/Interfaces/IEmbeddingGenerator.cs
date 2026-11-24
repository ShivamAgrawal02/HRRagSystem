using System.Numerics;

namespace HRRagSystem.Services.Interfaces
{
    public interface IEmbeddingGenerator
    {
        public Task<float[]> GenerateEmbeddingAsync(string sentence);
    }
}
