using HRRagSystem.Services.Interfaces;
using System.Text.RegularExpressions;

namespace HRRagSystem.Services.Implementation
{
    public class TextCleaner : ITextCleaner
    {
        private readonly ILogger<TextCleaner> _logger;
        public TextCleaner(ILogger<TextCleaner> logger)
        {
            _logger = logger;
        }
        public Task<string> CleanTextAsync(string text, CancellationToken cancellationToken = default)
        {
            if(string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Received empty text for cleaning.");
                return Task.FromResult(string.Empty);
            }
            // Normalize line endings
            text = text.Replace("\r\n", "\n")
                       .Replace("\r", "\n");

            // Remove unnecessary spaces and tabs
            text = Regex.Replace(text, @"[ \t]+", " ");

            // Remove excessive blank lines
            text = Regex.Replace(text, @"\n{3,}", "\n\n");

            return Task.FromResult(text.Trim());
          
        }
    }q
}
