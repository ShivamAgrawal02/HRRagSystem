using HRRagSystem.Database;
using HRRagSystem.Models;
using HRRagSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRRagSystem.Services.Implementation
{
    public class DocumentRepository : IDocumentRepository
    { 
        private readonly ILogger<DocumentRepository> _logger;
        private readonly HRRagSystemSQLiteDbContext _context;

        public DocumentRepository(HRRagSystemSQLiteDbContext context , ILogger<DocumentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task DeleteAsync(DocumentInfo document, CancellationToken cancellationToken = default)
        {
            var existingDocument = await _context.Documents.FindAsync(document.DocumentId, cancellationToken);
            if(existingDocument == null)
            {
                _logger.LogWarning($"Document with ID {document.DocumentId} not found for deletion.");
                return;
            }

            _context.Documents.Remove(existingDocument);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<DocumentInfo>> GetAllDocumentsAsync(CancellationToken cancellationToken = default)
        {
            var documents = await _context.Documents.AsNoTracking().ToListAsync(cancellationToken);
            return documents;
        }

        public async Task UpsertAsync(DocumentInfo document, CancellationToken cancellationToken = default)
        {
            var existingDocument = await _context.Documents.FindAsync(document.DocumentId,cancellationToken);
            if (existingDocument != null)
            {
                _context.Entry(existingDocument).CurrentValues.SetValues(document);
            }
            else
            {
               await _context.Documents.AddAsync(document);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
