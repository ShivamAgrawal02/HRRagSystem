using HRRagSystem.Models;
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
        private async Task ProcessDocumentAsync(DocumentInfo document, CancellationToken cancellationToken, bool isUpdateDocument)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                //Get Document Path
                var documentPath = Path.Combine(_dataFolderPath, document.RelativePath);
                // Add Document to document repository 
                await _documentRepository.UpsertAsync(document);
                // Extract Text and update Document Status
                var extractedText = await _pdfReader.ExtractTextAsync(documentPath, cancellationToken);
                document.Status = DocumentStatus.Extracted;
                await _documentRepository.UpsertAsync(document);
                //Clean and Update Document Status
                var cleanedText = await _textCleaner.CleanTextAsync(extractedText, cancellationToken);
                document.Status = DocumentStatus.Cleaned;
                await _documentRepository.UpsertAsync(document);
                //Chunking and Update document status
                var chunks = await _chunker.ChunkDocumentAsync(cleanedText, document, cancellationToken);
                if (chunks.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"No chunks generated for {document.RelativePath}");
                }
                document.Status = DocumentStatus.Chunked;
                await _documentRepository.UpsertAsync(document);

                // if isUpdate Document is true then it means we are passing a list of updated document, so delete is old chunks from qdrant for better results
                if (isUpdateDocument)
                {
                    await _vectorRepository.DeleteDocumentAsync(document.DocumentId, cancellationToken);
                }
                // Insert the new Chunks into qdrant
                await _vectorRepository.UpsertChunksAsync(chunks, cancellationToken);

                //Updating Document status
                document.Status = DocumentStatus.Stored;
                document.LastIngestedAtUtc = DateTime.UtcNow;
                await _documentRepository.UpsertAsync(document, cancellationToken);
            }
            catch
            {
                document.Status = DocumentStatus.Failed;
                await _documentRepository.UpsertAsync(document,cancellationToken);

                throw;
            }
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
                await ProcessDocumentAsync(newDocument, cancellationToken, false);
            }
            foreach (var updatedDocument in updatedDocuments)
            {
                await ProcessDocumentAsync(updatedDocument, cancellationToken, true);
            }
        }    
        
        
                    
    }

}
