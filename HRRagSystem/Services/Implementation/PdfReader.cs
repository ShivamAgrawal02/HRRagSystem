using HRRagSystem.Services.Interfaces;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace HRRagSystem.Services.Implementation
{
    public class PdfReader : IPdfReader
    {
        public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "PDF file not found.", filePath);
            }
            StringBuilder extractedText = new StringBuilder();
            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                foreach(Page page in document.GetPages())
                {
                    extractedText.AppendLine(page.Text);
                }
            }
            return Task.FromResult(extractedText.ToString());
        }
    }
}
