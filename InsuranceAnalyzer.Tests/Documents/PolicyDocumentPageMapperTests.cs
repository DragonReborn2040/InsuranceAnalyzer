
using InsuranceAnalyzer.Application.Documents.Extraction;
using InsuranceAnalyzer.Infrastructure.Documents;
using InsuranceAnalyzer.Infrastructure.Documents.Extraction;
using Microsoft.Extensions.Options;

namespace InsuranceAnalyzer.Tests.Documents;

public sealed class PolicyDocumentPageMapperTests
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
        var original = new DocumentPageExtractionResult
        {
            PageNumber = pageNumber,
            Text = text,
            Source = source,
            OcrAttempted = ocrAttempted
        };

        return new DocumentPageExtractionResult
        {
            PageNumber = original.PageNumber,
            Text = original.Text,
            Source = original.Source,
            OcrAttempted = original.OcrAttempted,
            Quality = _validator.Validate(original)
        };
    }

    [Fact]
    public void ToEntity_MapsNativeTextPage()
    {
        var page = CreateValidatedPage(
            3,
            "This insurance policy provides guaranteed " +
            "surrender values subject to applicable conditions.",
            DocumentExtractionSource.PdfPig,
            false);

        var entity = PolicyDocumentPageMapper.ToEntity(9, page);

        Assert.Equal(9, entity.PolicyDocumentId);
        Assert.Equal(3, entity.PageNumber);
        Assert.Equal("PdfPig", entity.ExtractionSource);
        Assert.Equal(page.Text, entity.ExtractedText);
        Assert.Equal(page.Text.Length, entity.CharacterCount);
        Assert.Equal("Good", entity.QualityStatus);
        Assert.False(entity.OcrAttempted);
    }

    [Fact]
    public void ToEntity_MapsEmptyOcrPage()
    {
        var page = CreateValidatedPage(
            22,
            string.Empty,
            DocumentExtractionSource.None,
            true);

        var entity = PolicyDocumentPageMapper.ToEntity(9, page);

        Assert.Equal("None", entity.ExtractionSource);
        Assert.Equal(string.Empty, entity.ExtractedText);
        Assert.Equal(0, entity.CharacterCount);
        Assert.True(entity.OcrAttempted);
        Assert.Equal("Missing", entity.QualityStatus);
        Assert.Contains("NO_TEXT", entity.QualityWarningsJson);
    }

    [Fact]
    public void ToDto_RestoresSerializedWarnings()
    {
        var page = CreateValidatedPage(
            4,
            "Policy Number 123",
            DocumentExtractionSource.PaddleOcr,
            true);

        var entity = PolicyDocumentPageMapper.ToEntity(12, page);
        entity.Id = 45;

        var dto = PolicyDocumentPageMapper.ToDto(entity);

        Assert.Equal(45, dto.Id);
        Assert.Equal(12, dto.PolicyDocumentId);
        Assert.Equal(4, dto.PageNumber);
        Assert.Equal("PaddleOcr", dto.ExtractionSource);
        Assert.Equal(page.Text, dto.ExtractedText);
        Assert.True(dto.OcrAttempted);
        Assert.Equal("NeedsReview", dto.QualityStatus);

        Assert.Contains(
            dto.QualityWarnings,
            warning => warning.Code == "SHORT_TEXT");
    }

    [Fact]
    public void ToEntity_RejectsUnvalidatedPages()
    {
        var page = new DocumentPageExtractionResult
        {
            PageNumber = 1,
            Text = "Some extracted text",
            Source = DocumentExtractionSource.PdfPig,
            Quality = null
        };

        Assert.Throws<InvalidOperationException>(() =>
            PolicyDocumentPageMapper.ToEntity(9, page));
    }

    [Fact]
    public void ToEntity_RejectsInvalidDocumentId()
    {
        var page = CreateValidatedPage(
            1,
            "This is a sufficiently long insurance policy " +
            "description for validation purposes.",
            DocumentExtractionSource.PdfPig,
            false);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PolicyDocumentPageMapper.ToEntity(0, page));
    }
}
