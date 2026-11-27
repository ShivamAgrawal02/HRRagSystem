using HRRagSystem.Models;
using HRRagSystem.Services.Interfaces;
using System.Security.Cryptography;

namespace HRRagSystem.Services.Implementation
{
    public class DocumentScanner : IDocumentScanner
    {
        private readonly ILogger<DocumentScanner> _logger;
        private readonly string _dataFolderPath;
        public DocumentScanner(ILogger<DocumentScanner> logger, IConfiguration configuration)
        {
            _logger = logger;
            _dataFolderPath = configuration["DataFolderPath"] ?? throw new InvalidOperationException("DataFolderPath is not configured.");
        }
        public async Task<List<DocumentInfo>> ScanAsync(CancellationToken cancellationToken = default)
        {
          
            var fullPath = Path.GetFullPath(_dataFolderPath);
            if (!Directory.Exists(fullPath))
            {
                _logger.LogError($"Data folder path does not exist: {fullPath}");

                throw new DirectoryNotFoundException($"Data folder path does not exist: {fullPath}");
            }

            // empty list of type DocumentInfo to return data
            var documentInfos = new List<DocumentInfo>();
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MatchCasing = MatchCasing.CaseInsensitive
            };

            var files = Directory.EnumerateFiles(fullPath, "*.pdf*", options);
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(!string.Equals(Path.GetExtension(file), ".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning($"Skipping non-PDF file: {file}");
                    continue;
                }
                var fileInfo = new FileInfo(file);
                var fileHash = await CalculateFileHashAsync(fileInfo.FullName, cancellationToken);
                var relativePath = Path.GetRelativePath(
                  fullPath,
                  fileInfo.FullName).Replace('\\', '/');

                var document = new DocumentInfo
                {
                    FileName = fileInfo.Name,
                    RelativePath = relativePath,
                    FileHash = fileHash,
                    FileSize = fileInfo.Length,
                    LastModifiedUtc = fileInfo.LastWriteTimeUtc,
                    Status = DocumentStatus.NotProcessed
                };

                documentInfos.Add(document);
            }
            return documentInfos;
        }

        private static async Task<string> CalculateFileHashAsync(
            string filePath,
            CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                options: FileOptions.Asynchronous |
                         FileOptions.SequentialScan);

            var hash = await SHA256.HashDataAsync(
                stream,
                cancellationToken);

            return Convert.ToHexString(hash);
        }
    }
}
