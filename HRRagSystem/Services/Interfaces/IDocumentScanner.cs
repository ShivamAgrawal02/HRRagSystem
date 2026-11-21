using HRRagSystem.Models;

namespace HRRagSystem.Services.Interfaces
{
    public interface IDocumentScanner
    {
        Task<List<DocumentInfo>> ScanAsync(CancellationToken cancellationToken = default);
    }
}
