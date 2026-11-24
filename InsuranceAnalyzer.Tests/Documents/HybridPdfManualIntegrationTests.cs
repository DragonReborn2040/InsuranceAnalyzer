using InsuranceAnalyzer.Application.Documents.Extraction;
using InsuranceAnalyzer.Infrastructure.Documents.Extraction;
using Microsoft.Extensions.Options;
using Xunit;

namespace InsuranceAnalyzer.Tests.Documents;

public sealed class HybridPdfManualIntegrationTests
{
    //[Fact(Skip = "Run locally after configuring the PDF path.")]
    [Fact]
    public async Task SampleHybridPdf_ExtractsDiagnostics()
    {
        var pdfPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            "Downloads",
            "sample-heavy_merged.pdf");

        Assert.True(
            File.Exists(pdfPath),
            $"Test PDF not found: {pdfPath}");

        var validator = new DocumentExtractionQualityValidator(
            Options.Create(
                new DocumentQualityValidationOptions()));

        var ocrService = new PaddleOcrPageService();

        var extractor = new HybridPdfTextExtractionService(
            ocrService,
            validator);

        var result = await extractor.ExtractAsync(pdfPath);

        Assert.Equal(24, result.PageCount);
        Assert.Equal(24, result.Pages.Count);
        Assert.Equal(0, result.NotEvaluatedPageCount);

        Assert.All(result.Pages, page =>
        {
            Assert.NotNull(page.Quality);
        });

        Assert.All(
            result.Pages.Where(x =>
                x.Source == DocumentExtractionSource.PaddleOcr),
            page => Assert.True(page.OcrAttempted));

        Assert.All(
            result.Pages.Where(x =>
                x.Source == DocumentExtractionSource.None),
            page =>
            {
                Assert.Equal(0, page.CharacterCount);
                Assert.Equal("Missing", page.QualityStatus);
                Assert.True(page.OcrAttempted);
            });

        foreach (var page in result.Pages)
        {
            Console.WriteLine(
                $"Page {page.PageNumber}: " +
                $"Source={page.Source}, " +
                $"OCR={page.OcrAttempted}, " +
                $"Characters={page.CharacterCount}, " +
                $"Quality={page.QualityStatus}, " +
                $"Warnings={string.Join(",",
                    page.QualityWarnings.Select(x => x.Code))}");
        }
    }
}