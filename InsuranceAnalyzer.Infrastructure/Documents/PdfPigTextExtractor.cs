using System.Text;
using InsuranceAnalyzer.Application.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace InsuranceAnalyzer.Infrastructure.Documents;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public Task<PdfExtractionResult> ExtractAsync(
        string physicalFilePath,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var output = new StringBuilder();
            var actualCharacterCount = 0;

            using var document = PdfDocument.Open(physicalFilePath);

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pageText = ExtractPageText(page);

                output.AppendLine($"--- Page {page.Number} ---");

                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    output.AppendLine(pageText.Trim());

                    actualCharacterCount +=
                        pageText.Count(c => !char.IsWhiteSpace(c));
                }

                output.AppendLine();
            }

            return new PdfExtractionResult(
                output.ToString().Trim(),
                document.NumberOfPages,
                actualCharacterCount > 0);
        }, cancellationToken);
    }

    private static string ExtractPageText(
        UglyToad.PdfPig.Content.Page page)
    {
        // Strategy 1:
        // Preferred extraction for readable page order.
        var text =
            ContentOrderTextExtractor.GetText(
                page,
                true);

        if (!string.IsNullOrWhiteSpace(text))
            return text;

        // Strategy 2:
        // Fallback to words.
        var words =
            page.GetWords(
                NearestNeighbourWordExtractor.Instance)
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .Select(x => x.Text)
            .ToList();

        if (words.Count > 0)
            return string.Join(" ", words);

        // Strategy 3:
        // Last PdfPig fallback.
        if (!string.IsNullOrWhiteSpace(page.Text))
            return page.Text;

        return string.Empty;
    }
}