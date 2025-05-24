using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using RigelAI.Core;
using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace RigelAI.Core
{
    public class DocumentChatService
    {
        private readonly RigelChatService _rigelChatService;

        public DocumentChatService(RigelChatService rigelChatService)
        {
            _rigelChatService = rigelChatService;
        }

        public async Task<string> HandleDocumentAsync(long userId, Stream fileStream, string fileName, string prompt)
        {
            if (fileStream == null || !fileStream.CanRead)
                return "❌ Invalid file stream.";

            try
            {
                string extension = Path.GetExtension(fileName).ToLowerInvariant();
                string extractedText = extension switch
                {
                    ".pdf" => ExtractTextFromPdf(fileStream),
                    ".docx" => ExtractTextFromDocx(fileStream),
                    ".txt" => ExtractTextFromTxt(fileStream),
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(extractedText))
                    return "⚠️ Unsupported or unreadable document.";

                string combinedMessage = $"{prompt}\n\n{extractedText}";
                return await _rigelChatService.GetResponseAsync(userId, combinedMessage);
            }
            catch (Exception ex)
            {
                return $"❌ Failed to process document: {ex.Message}";
            }
        }

        private string ExtractTextFromPdf(Stream pdfStream)
        {
            using var reader = new PdfReader(pdfStream);
            using var pdfDoc = new PdfDocument(reader);
            var text = new StringBuilder();

            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
            {
                var page = pdfDoc.GetPage(i);
                text.AppendLine(PdfTextExtractor.GetTextFromPage(page));
            }

            return text.ToString().Trim();
        }

        private string ExtractTextFromDocx(Stream docxStream)
        {
            using var mem = new MemoryStream();
            docxStream.CopyTo(mem);
            mem.Position = 0;

            var sb = new StringBuilder();
            using var doc = WordprocessingDocument.Open(mem, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                sb.AppendLine(body.InnerText);
            }

            return sb.ToString().Trim();
        }

        private string ExtractTextFromTxt(Stream txtStream)
        {
            using var reader = new StreamReader(txtStream);
            return reader.ReadToEnd().Trim();
        }
    }
}
