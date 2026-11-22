using HRRagSystem.Services.Interfaces;

namespace HRRagSystem.Services.Implementation
{
    public class IngestionService : IIngestionService
    {
        private readonly IDocumentScanner _documentScanner;
        private readonly IDocumentRepository _documentRepository;
        private readonly IChangeDetector _changeDetector;
        public IngestionService(IDocumentScanner documentScanner, IDocumentRepository documentRepository, IChangeDetector changeDetector)
        {
            _documentScanner = documentScanner;
            _documentRepository = documentRepository;
            _changeDetector = changeDetector;
        }
        public async Task PerformIngestAsync()
        {
            var scannedDocuments = await _documentScanner.ScanAsync();
            var existingDocuments = await _documentRepository.GetAllDocumentsAsync();
            var ChangedDocuments = await _changeDetector.DetectChangeASync(scannedDocuments, existingDocuments);
        }
    }
}
