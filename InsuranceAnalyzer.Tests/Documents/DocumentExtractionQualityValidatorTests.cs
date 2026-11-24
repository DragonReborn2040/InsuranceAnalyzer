
using InsuranceAnalyzer.Application.Documents.Extraction;
using InsuranceAnalyzer.Infrastructure.Documents.Extraction;
using Microsoft.Extensions.Options;

namespace InsuranceAnalyzer.Tests.Documents;

public sealed class DocumentExtractionQualityValidatorTests
{
    private readonly DocumentExtractionQualityValidator _validator =
        new(Options.Create(new DocumentQualityValidationOptions()));

    private static DocumentPageExtractionResult CreatePage(
        string text,
        DocumentExtractionSource source =
            DocumentExtractionSource.PdfPig)
    {
        return new DocumentPageExtractionResult
        {
            PageNumber = 1,
            Text = text,
            Source = source
        };
    }

    [Fact]
    public void Validate_NormalNativeText_ReturnsGood()
    {
        var page = CreatePage(
            "This insurance policy provides guaranteed surrender " +
            "values and maturity benefits subject to the " +
            "terms and conditions specified in the policy.");

        var result = _validator.Validate(page);

        Assert.Equal("Good", result.QualityStatus);
        Assert.Empty(result.Warnings);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(DocumentExtractionSource.PdfPig, result.Source);
    }

    [Fact]
    public void Validate_EmptyText_ReturnsMissing()
    {
        var page = CreatePage(
            string.Empty,
            DocumentExtractionSource.None);

        var result = _validator.Validate(page);

        Assert.Equal("Missing", result.QualityStatus);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "NO_TEXT");
    }

    [Fact]
    public void Validate_ShortText_ReturnsWarning()
    {
        var page = CreatePage("Policy Number 123");

        var result = _validator.Validate(page);

        Assert.Equal("NeedsReview", result.QualityStatus);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "SHORT_TEXT");
    }

    [Fact]
    public void Validate_HighSymbolRatio_ReturnsWarning()
    {
        var page = CreatePage(
            new string('A', 60) + new string('#', 50));

        var result = _validator.Validate(page);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "SUSPICIOUS_SYMBOLS");
    }

    [Fact]
    public void Validate_ReplacementCharacters_ReturnsWarning()
    {
        var page = CreatePage(
            "Guaranteed surrender value for this policy " +
            "is subject to terms and conditions \uFFFD\uFFFD.");

        var result = _validator.Validate(page);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "INVALID_CHARACTERS");
    }

    [Fact]
    public void Validate_RepeatedLetters_ReturnsWarning()
    {
        var page = CreatePage(
            "Insurance policy surrender values " +
            "AAAAAAAAAAAA and related benefits.");

        var result = _validator.Validate(page);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "REPEATED_TEXT");
    }

    [Fact]
    public void Validate_RepeatedDigits_DoesNotFlagRepetition()
    {
        var page = CreatePage(
            "Guaranteed surrender value table 100000000000 " +
            "for policy year five with applicable conditions.");

        var result = _validator.Validate(page);

        Assert.DoesNotContain(
            result.Warnings,
            x => x.Code == "REPEATED_TEXT");
    }

    [Fact]
    public void Validate_OcrWithInvalidCharacters_FlagsReview()
    {
        var page = CreatePage(
            "Guaranteed surrender value as per policy " +
            "terms \uFFFD\uFFFD conditions.",
            DocumentExtractionSource.PaddleOcr);

        var result = _validator.Validate(page);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "INVALID_CHARACTERS");

        Assert.Contains(
            result.Warnings,
            x => x.Code == "OCR_REVIEW_REQUIRED");
    }

    [Fact]
    public void Validate_EmptyOcrResult_ReturnsMissing()
    {
        var page = CreatePage(
            string.Empty,
            DocumentExtractionSource.PaddleOcr);

        var result = _validator.Validate(page);

        Assert.Equal("Missing", result.QualityStatus);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "OCR_EMPTY");
    }

    [Fact]
    public void Validate_FinancialTable_DoesNotFlagSymbols()
    {
        var page = CreatePage(
            "Policy Year | Premium Paid | GSV Factor\n" +
            "3 | ₹3,00,000 | 30%\n" +
            "4 | ₹4,00,000 | 50%\n" +
            "5 | ₹5,00,000 | 60%\n" +
            "6 | ₹6,00,000 | 70%");

        var result = _validator.Validate(page);

        Assert.DoesNotContain(
            result.Warnings,
            x => x.Code == "SUSPICIOUS_SYMBOLS");
    }

    [Fact]
    public void Validate_PreservesPageNumberAndCharacterCount()
    {
        var page = new DocumentPageExtractionResult
        {
            PageNumber = 7,
            Source = DocumentExtractionSource.PaddleOcr,
            Text = "Guaranteed Surrender Value"
        };

        var result = _validator.Validate(page);

        Assert.Equal(7, result.PageNumber);
        Assert.Equal(page.Text.Length, result.CharacterCount);
        Assert.Equal(
            DocumentExtractionSource.PaddleOcr,
            result.Source);
    }

    [Fact]
    public void Validate_WhitespaceOnly_ReturnsMissing()
    {
        var page = CreatePage(" \r\n\t ");

        var result = _validator.Validate(page);

        Assert.Equal("Missing", result.QualityStatus);

        Assert.Contains(
            result.Warnings,
            x => x.Code == "NO_TEXT");
    }

    [Fact]
    public void Validate_ConfigurableThreshold_IsApplied()
    {
        var options = Options.Create(
            new DocumentQualityValidationOptions
            {
                MinimumAlphanumericCharacters = 10
            });

        var validator =
            new DocumentExtractionQualityValidator(options);

        var result = validator.Validate(
            CreatePage("Policy Number 123"));

        Assert.DoesNotContain(
            result.Warnings,
            x => x.Code == "SHORT_TEXT");
    }
}
