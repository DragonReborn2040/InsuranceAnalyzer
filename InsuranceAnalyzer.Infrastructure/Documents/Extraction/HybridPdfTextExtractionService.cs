using System.Text;
using InsuranceAnalyzer.Application.Documents.Extraction;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace InsuranceAnalyzer.Infrastructure.Documents.Extraction;

public sealed class HybridPdfTextExtractionService
{
    private const int MinimumMeaningfulCharacters = 20;

    private readonly IPdfPageOcrService _ocrService;

    public HybridPdfTextExtractionService(
        IPdfPageOcrService ocrService)
    {
        _ocrService = ocrService;
    }

    public async Task<DocumentExtractionResult> ExtractAsync(
        string pdfPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);

        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException(
                "PDF document was not found.",
                pdfPath);
        }

        var pages = new List<DocumentPageExtractionResult>();

        //
        // PASS 1
        // ------
        // Try PdfPig on every page.
        //
        using (var document = PdfDocument.Open(pdfPath))
        {
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                string text;

                try
                {
                    text = ContentOrderTextExtractor.GetText(page);
                }
                catch
                {
                    text = string.Empty;
                }

                text = NormalizeText(text);

                var meaningful = IsMeaningfulText(text);

                pages.Add(new DocumentPageExtractionResult
                {
                    PageNumber = page.Number,
                    Text = meaningful
                        ? text
                        : string.Empty,
                    Source = meaningful
                        ? DocumentExtractionSource.PdfPig
                        : DocumentExtractionSource.None
                });
            }
        }

        //
        // PASS 2
        // ------
        // OCR only pages PdfPig could not meaningfully extract.
        //
        var pagesNeedingOcr = pages
            .Where(x => x.Source == DocumentExtractionSource.None)
            .Select(x => x.PageNumber)
            .ToArray();

        if (pagesNeedingOcr.Length > 0)
        {
            var ocrResults =
                await _ocrService.ExtractPagesAsync(
                    pdfPath,
                    pagesNeedingOcr,
                    cancellationToken);

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];

                if (page.Source != DocumentExtractionSource.None)
                {
                    continue;
                }

                if (!ocrResults.TryGetValue(
                        page.PageNumber,
                        out var ocrText))
                {
                    continue;
                }

                ocrText = NormalizeText(ocrText);

                if (!IsMeaningfulText(ocrText))
                {
                    continue;
                }

                pages[i] = new DocumentPageExtractionResult
                {
                    PageNumber = page.PageNumber,
                    Text = ocrText,
                    Source = DocumentExtractionSource.PaddleOcr
                };
            }
        }

        //
        // PASS 3
        // ------
        // Reconstruct complete document in page order.
        //
        var fullText = BuildFullText(pages);

        return new DocumentExtractionResult
        {
            PageCount = pages.Count,
            Pages = pages,
            FullText = fullText
        };
    }

    private static bool IsMeaningfulText(string? text)
    {
        if (text?.Count(char.IsLetterOrDigit) < 10)
        {
            return false;
        }

        var meaningfulCharacters = text.Count(char.IsLetterOrDigit);

        return meaningfulCharacters >= MinimumMeaningfulCharacters;
    }

    private static string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Replace("\0", string.Empty).Trim();
    }

    private static string BuildFullText(
        IEnumerable<DocumentPageExtractionResult> pages)
    {
        var builder = new StringBuilder();

        foreach (var page in pages.OrderBy(x => x.PageNumber))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
                builder.AppendLine();
            }

            builder.AppendLine(
                $"===== PAGE {page.PageNumber} [{page.Source}] =====");

            builder.AppendLine();

            if (string.IsNullOrWhiteSpace(page.Text))
            {
                builder.AppendLine("[No text extracted]");
            }
            else
            {
                builder.Append(page.Text);
            }
        }

        return builder.ToString().Trim();
    }
}