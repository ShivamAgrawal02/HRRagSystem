namespace HRRagSystem.Services.Interfaces
{
    public interface IPdfReader
    {
        Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
