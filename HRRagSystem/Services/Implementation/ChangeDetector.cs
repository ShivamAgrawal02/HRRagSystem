using HRRagSystem.Models;
using HRRagSystem.Services.Interfaces;

namespace HRRagSystem.Services.Implementation
{
    public class ChangeDetector : IChangeDetector
    {
   
        private readonly ILogger<ChangeDetector> _logger;
        public ChangeDetector(ILogger<ChangeDetector> logger)
        {
            _logger = logger;
        }
  
        public Task<DocumentChangesSet> DetectChangeASync(List<DocumentInfo> scannedDocumentsFromFilePath, List<DocumentInfo> documentFromDatabase, CancellationToken cancellationToken = default)
        {
            List<DocumentInfo> newDocuments = new List<DocumentInfo>();
            List<DocumentInfo> updatedDocuments = new List<DocumentInfo>();
            List<DocumentInfo> deletedDocuments = new List<DocumentInfo>();
            foreach(var scannedDocument in  scannedDocumentsFromFilePath)
            {
                bool isDocumentExistsInDatabase = false;
                foreach (var databaseDocument in documentFromDatabase)
                {
                    if(scannedDocument.RelativePath==databaseDocument.RelativePath)
                    {
                        isDocumentExistsInDatabase = true;
                        if(scannedDocument.FileHash != databaseDocument.FileHash)
                        {
                            updatedDocuments.Add(scannedDocument);
                        }
                    }
                }
                if (isDocumentExistsInDatabase==false)
                {
                    newDocuments.Add(scannedDocument);
                }
            }
            foreach (var databaseDocuments in documentFromDatabase)
            {
                var existsInScannedDocuments = scannedDocumentsFromFilePath.Any(d => d.RelativePath == databaseDocuments.RelativePath);
                if (existsInScannedDocuments == false)
                {
                    deletedDocuments.Add(databaseDocuments);
                }
            }

            DocumentChangesSet documentChangesSet = new DocumentChangesSet
            {
                NewDocuments = newDocuments,
                UpdatedDocuments = updatedDocuments,
                DeletedDocuments = deletedDocuments
            };
            return Task.FromResult(documentChangesSet);
        }
    }
}
