
using System.Text;
using InsuranceAnalyzer.Application.Documents.Extraction;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace InsuranceAnalyzer.Infrastructure.Documents.Extraction;

public sealed class HybridPdfTextExtractionService
{
    private const int MinimumMeaningfulCharacters = 20;

    private readonly IPdfPageOcrService _ocrService;
    private readonly IDocumentExtractionQualityValidator _qualityValidator;

    public HybridPdfTextExtractionService(
        IPdfPageOcrService ocrService,
        IDocumentExtractionQualityValidator qualityValidator)
    {
        _ocrService = ocrService ??
            throw new ArgumentNullException(nameof(ocrService));

        _qualityValidator = qualityValidator ??
            throw new ArgumentNullException(nameof(qualityValidator));
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
        // Try native extraction using PdfPig.
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

                bool meaningful = IsMeaningfulText(text);

                pages.Add(new DocumentPageExtractionResult
                {
                    PageNumber = page.Number,

                    Text = meaningful
                        ? text
                        : string.Empty,

                    Source = meaningful
                        ? DocumentExtractionSource.PdfPig
                        : DocumentExtractionSource.None,

                    OcrAttempted = false
                });
            }
        }

        //
        // PASS 2
        // OCR only pages without meaningful native text.
        //
        var pagesNeedingOcr = pages
            .Where(x =>
                x.Source == DocumentExtractionSource.None)
            .Select(x => x.PageNumber)
            .ToArray();

        if (pagesNeedingOcr.Length > 0)
        {
            // The OCR service is invoked for every page in
            // pagesNeedingOcr. Mark them as attempted.
            var pendingOcrPages = pagesNeedingOcr.ToHashSet();

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];

                if (!pendingOcrPages.Contains(page.PageNumber))
                    continue;

                pages[i] = new DocumentPageExtractionResult
                {
                    PageNumber = page.PageNumber,
                    Text = page.Text,
                    Source = page.Source,
                    OcrAttempted = true
                };
            }

            // Preserve the existing OCR fallback service.
            var ocrResults =
                await _ocrService.ExtractPagesAsync(
                    pdfPath,
                    pagesNeedingOcr,
                    cancellationToken);

            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];

                if (page.Source != DocumentExtractionSource.None)
                    continue;

                if (!ocrResults.TryGetValue(
                        page.PageNumber,
                        out var ocrText))
                {
                    // OCR attempted but returned no page result.
                    // Keep Source=None and Text empty.
                    continue;
                }

                ocrText = NormalizeText(ocrText);

                if (!IsMeaningfulText(ocrText))
                {
                    // Preserve the Phase 2C behavior:
                    // unmeaningful OCR results are not accepted.
                    continue;
                }

                pages[i] = new DocumentPageExtractionResult
                {
                    PageNumber = page.PageNumber,
                    Text = ocrText,
                    Source = DocumentExtractionSource.PaddleOcr,
                    OcrAttempted = true
                };
            }
        }

        //
        // PASS 3 — PHASE 2D.2B
        // Validate every page after extraction finishes.
        //
        var validatedPages =
            new List<DocumentPageExtractionResult>(pages.Count);

        foreach (var page in pages.OrderBy(x => x.PageNumber))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var quality = _qualityValidator.Validate(page);

            validatedPages.Add(new DocumentPageExtractionResult
            {
                PageNumber = page.PageNumber,
                Text = page.Text,
                Source = page.Source,
                OcrAttempted = page.OcrAttempted,
                Quality = quality
            });
        }

        //
        // PASS 4
        // Reconstruct combined text in original page order.
        //
        var fullText = BuildFullText(validatedPages);

        return new DocumentExtractionResult
        {
            PageCount = validatedPages.Count,
            Pages = validatedPages,
            FullText = fullText
        };
    }

    private static bool IsMeaningfulText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Count(char.IsLetterOrDigit)
            >= MinimumMeaningfulCharacters;
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
