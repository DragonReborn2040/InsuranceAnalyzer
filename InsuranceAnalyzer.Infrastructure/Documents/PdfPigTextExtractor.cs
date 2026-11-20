using System.Text;
using InsuranceAnalyzer.Application.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace InsuranceAnalyzer.Infrastructure.Documents;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<PdfExtractionResult> ExtractAsync(
        string physicalFilePath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var text = new StringBuilder();

            using var document = PdfDocument.Open(physicalFilePath);

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                text.AppendLine($"--- Page {page.Number} ---");
                text.AppendLine(
                    ContentOrderTextExtractor.GetText(page));

                text.AppendLine();
            }

            var extractedText = text.ToString().Trim();

            return new PdfExtractionResult(
                extractedText,
                document.NumberOfPages,
                !string.IsNullOrWhiteSpace(extractedText));

        }, cancellationToken);
    }
}