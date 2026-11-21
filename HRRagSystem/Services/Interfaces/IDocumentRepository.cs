using HRRagSystem.Models;

namespace HRRagSystem.Services.Interfaces
{
    public interface IDocumentRepository
    {
        Task<List<DocumentInfo>> GetAllDocumentsAsync(CancellationToken cancellationToken = default);
        Task UpsertAsync(DocumentInfo document,CancellationToken cancellationToken = default);
        Task DeleteAsync(DocumentInfo document, CancellationToken cancellationToken = default);
    }
}
