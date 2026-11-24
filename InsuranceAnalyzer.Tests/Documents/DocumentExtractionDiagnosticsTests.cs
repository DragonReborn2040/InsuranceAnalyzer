
using InsuranceAnalyzer.Application.Documents.Extraction;
using InsuranceAnalyzer.Infrastructure.Documents.Extraction;
using Microsoft.Extensions.Options;

namespace InsuranceAnalyzer.Tests.Documents;

public sealed class DocumentExtractionDiagnosticsTests
{
    private readonly IDocumentExtractionQualityValidator _validator =
        new DocumentExtractionQualityValidator(
            Options.Create(new DocumentQualityValidationOptions()));

    private DocumentPageExtractionResult CreateValidatedPage(
        int pageNumber,
        string text,
        DocumentExtractionSource source,
        bool ocrAttempted)
    {
        var page = new DocumentPageExtractionResult
        {
            PageNumber = pageNumber,
            Text = text,
            Source = source,
            OcrAttempted = ocrAttempted
        };

        return new DocumentPageExtractionResult
        {
            PageNumber = page.PageNumber,
            Text = page.Text,
            Source = page.Source,
            OcrAttempted = page.OcrAttempted,
            Quality = _validator.Validate(page)
        };
    }

    [Fact]
    public void NativePage_HasGoodQuality_WithoutOcr()
    {
        var page = CreateValidatedPage(
            1,
            "This insurance policy contains guaranteed " +
            "surrender values and maturity benefits " +
            "subject to applicable policy conditions.",
            DocumentExtractionSource.PdfPig,
            false);

        Assert.Equal("Good", page.QualityStatus);
        Assert.False(page.OcrAttempted);
        Assert.Equal(
            DocumentExtractionSource.PdfPig,
            page.Source);
        Assert.Empty(page.QualityWarnings);
    }

    [Fact]
    public void SuccessfulOcrPage_TracksAttemptAndSource()
    {
        var page = CreateValidatedPage(
            2,
            "The survival benefit shall be payable " +
            "at the end of the fifth policy year " +
            "subject to policy conditions.",
            DocumentExtractionSource.PaddleOcr,
            true);

        Assert.True(page.OcrAttempted);
        Assert.Equal(
            DocumentExtractionSource.PaddleOcr,
            page.Source);
        Assert.Equal("Good", page.QualityStatus);
    }

    [Fact]
    public void FailedOcrPage_HasMissingQuality()
    {
        var page = CreateValidatedPage(
            3,
            string.Empty,
            DocumentExtractionSource.None,
            true);

        Assert.True(page.OcrAttempted);
        Assert.Equal(0, page.CharacterCount);
        Assert.Equal("Missing", page.QualityStatus);

        Assert.Contains(
            page.QualityWarnings,
            warning => warning.Code == "NO_TEXT");
    }

    [Fact]
    public void DocumentResult_CalculatesCorrectSummaries()
    {
        var pages = new[]
        {
            CreateValidatedPage(
                1,
                "This insurance policy provides guaranteed " +
                "surrender values and other policy benefits.",
                DocumentExtractionSource.PdfPig,
                false),

            CreateValidatedPage(
                2,
                "Survival benefits are payable according " +
                "to the insurance policy schedule.",
                DocumentExtractionSource.PaddleOcr,
                true),

            CreateValidatedPage(
                3,
                string.Empty,
                DocumentExtractionSource.None,
                true)
        };

        var result = new DocumentExtractionResult
        {
            PageCount = pages.Length,
            Pages = pages,

            // Deliberately includes generated markers.
            FullText = "===== PAGE 1 =====\n" +
                       pages[0].Text +
                       "\n===== PAGE 2 =====\n" +
                       pages[1].Text +
                       "\n===== PAGE 3 =====\n" +
                       "[No text extracted]"
        };

        Assert.Equal(3, result.PageCount);

        Assert.Equal(1, result.PdfPigPageCount);
        Assert.Equal(1, result.OcrPageCount);
        Assert.Equal(1, result.EmptyPageCount);

        Assert.Equal(2, result.OcrAttemptedPageCount);

        Assert.Equal(2, result.GoodPageCount);
        Assert.Equal(0, result.NeedsReviewPageCount);
        Assert.Equal(1, result.MissingPageCount);

        Assert.Equal(0, result.NotEvaluatedPageCount);

        Assert.Equal(
            pages.Sum(x => x.Text.Length),
            result.CharacterCount);

        Assert.True(result.UsedOcr);
        Assert.True(result.HasText);
    }
}
