using InsuranceAnalyzer.Application.Documents.Extraction;
using PaddleOcrNet.Models;
using PaddleOcrNet.Pdf;
using PaddleOcrNet.Services;

namespace InsuranceAnalyzer.Infrastructure.Documents.Extraction;

public sealed class PaddleOcrPageService : IPdfPageOcrService
{
    public async Task<IReadOnlyDictionary<int, string>> ExtractPagesAsync(
        string pdfPath,
        IReadOnlyCollection<int> pageNumbers,
        CancellationToken cancellationToken = default)
    {
        if (pageNumbers.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var result = new Dictionary<int, string>();

        await using var ocr = new PaddleOcrService();

        foreach (var pageNumber in pageNumbers
                     .Distinct()
                     .OrderBy(x => x))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new PdfOcrOptions
            {
                // We explicitly want OCR here.
                // PdfPig has already checked the native text layer.
                TextLayer = PdfTextLayerMode.Ignore,

                // OCR one page only.
                PageRange = pageNumber.ToString(),

                // 200 DPI initially.
                // We can tune this later if a policy has very small print.
                Dpi = 200
            };

            await foreach (var page in ocr.ExtractTextFromPdfPagesAsync(
                               pdfPath,
                               OcrLanguage.English,
                               pdfOptions: options)
                           .WithCancellation(cancellationToken))
            {
                result[pageNumber] =
                    page.Ocr.FullText?.Trim() ?? string.Empty;
            }
        }

        return result;
    }
}