namespace HRRagSystem.Services.Interfaces
{
    public interface ITextCleaner
    {
        Task<string> CleanTextAsync(string text, CancellationToken cancellationToken = default);
    }
}
