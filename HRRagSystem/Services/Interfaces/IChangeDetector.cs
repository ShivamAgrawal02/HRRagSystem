using HRRagSystem.Models;

namespace HRRagSystem.Services.Interfaces
{
    public interface IChangeDetector
    {
        Task<DocumentChangesSet> DetectChangeASync(List<DocumentInfo> scannedDocumentsFromFilePath, List<DocumentInfo> documentFromDatabase,CancellationToken cancellationToken = default);
    }
}
