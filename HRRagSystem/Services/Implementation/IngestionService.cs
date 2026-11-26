using HRRagSystem.Services.Interfaces;


namespace HRRagSystem.Services.Implementation
{
    public class IngestionService : IIngestionService
    {
        private readonly IDocumentScanner _documentScanner;
        private readonly IDocumentRepository _documentRepository;
        private readonly IChangeDetector _changeDetector;
        private readonly IVectorRepository _vectorRepository;
        private readonly IPdfReader _pdfReader;
        private readonly ITextCleaner _textCleaner;
        private readonly IChunker _chunker;
        private readonly string _dataFolderPath;
 
        public IngestionService(IDocumentScanner documentScanner, IDocumentRepository documentRepository, IChangeDetector changeDetector,IVectorRepository vectorRepository,IPdfReader pdfReader,ITextCleaner textCleaner, IChunker chunker,IConfiguration configuration)
        {
            _documentScanner = documentScanner;
            _documentRepository = documentRepository;
            _changeDetector = changeDetector;
            _vectorRepository = vectorRepository;
            _pdfReader = pdfReader;
            _textCleaner = textCleaner;
            _chunker = chunker;
            _dataFolderPath =  configuration["DataFolderPath"] ?? throw new InvalidOperationException("DataFolderPath is not configured.");
        }
        public async Task PerformIngestAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var scannedDocuments = await _documentScanner.ScanAsync(cancellationToken);
            var existingDocuments = await _documentRepository.GetAllDocumentsAsync(cancellationToken);
            var ChangedDocuments = await _changeDetector.DetectChangeASync(scannedDocuments, existingDocuments, cancellationToken);
            var updatedDocuments = ChangedDocuments.UpdatedDocuments;
            var newDocuments = ChangedDocuments.NewDocuments;
            var deletedDocuments = ChangedDocuments.DeletedDocuments;
            await _vectorRepository.EnsureCollectionAsync(cancellationToken);
            foreach (var deletedDocument in deletedDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _vectorRepository.DeleteDocumentAsync(deletedDocument.DocumentId,cancellationToken);
                await _documentRepository.DeleteAsync(deletedDocument,cancellationToken);

            }
            foreach (var newDocument in newDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var documentPath = Path.Combine(_dataFolderPath, newDocument.RelativePath);
                var extractedText = await _pdfReader.ExtractTextAsync(documentPath,cancellationToken);
                var cleanedText = await _textCleaner.CleanTextAsync(extractedText,cancellationToken);
                var chunks = await _chunker.ChunkDocumentAsync(cleanedText, newDocument,cancellationToken);
                await _vectorRepository.UpsertChunksAsync(chunks,cancellationToken);
                await _documentRepository.UpsertAsync(newDocument,cancellationToken);
            }
            foreach (var updatedDocument in updatedDocuments)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var documentPath = Path.Combine(_dataFolderPath, updatedDocument.RelativePath);
                var extractedText = await _pdfReader.ExtractTextAsync(documentPath,cancellationToken);
                var cleanedText = await _textCleaner.CleanTextAsync(extractedText,cancellationToken);
                var chunks = await _chunker.ChunkDocumentAsync(cleanedText, updatedDocument,cancellationToken);

                await _vectorRepository.DeleteDocumentAsync(updatedDocument.DocumentId,cancellationToken);
                await _vectorRepository.UpsertChunksAsync(chunks,cancellationToken);
                await _documentRepository.UpsertAsync(updatedDocument,cancellationToken);
            }
        }       
                    
    }
}
