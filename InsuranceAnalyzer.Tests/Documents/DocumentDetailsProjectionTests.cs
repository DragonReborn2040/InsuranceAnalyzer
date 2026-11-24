using InsuranceAnalyzer.Application.Documents;
using InsuranceAnalyzer.Application.Documents.Extraction;

namespace InsuranceAnalyzer.Tests.Documents;

public sealed class DocumentDetailsProjectionTests
{
    private static PolicyDocumentDto Document(string? text = null) => new(
        1, 1, "POL-1", "Test User", "test.pdf", 1234,
        "CompletedHybrid", DateTime.UtcNow, 2, text, null);

    private static PolicyDocumentPageDto Page(
        int number, string source, string text, int count,
        bool attempted, string status,
        IReadOnlyList<DocumentQualityWarning>? warnings = null) => new(
            number, 1, number, source, text, count,
            count, 0, 0, status, attempted, warnings ?? []);

    [Fact]
    public void StructuredPages_TakePrecedenceOverCombinedText()
    {
        var pages = new[] { Page(1, "PdfPig", "abc", 3, false, "Good") };
        var result = DocumentDetailsProjection.Create(Document("LEGACY TEXT"), pages);
        Assert.True(result.UsesPersistedDiagnostics);
        Assert.Single(result.Pages);
        Assert.Equal("abc", result.Pages[0].Text);
        Assert.Equal(3, result.CharacterCount);
    }

    [Fact]
    public void StructuredCounts_UsePersistedValuesNotTextMarkup()
    {
        var result = DocumentDetailsProjection.Create(Document(), [
            Page(1, "PdfPig", "different text", 7, false, "Good"),
            Page(2, "None", "", 0, true, "Missing", [
                new DocumentQualityWarning("NO_TEXT", "No text", "Error")])
        ]);
        Assert.Equal(7, result.CharacterCount);
        Assert.Equal(1, result.OcrAttemptedCount);
        Assert.Equal(0, result.OcrSuccessCount);
        Assert.Equal(1, result.MissingCount);
        Assert.Equal(1, result.WarningCount);
    }

    [Fact]
    public void OcrAttemptIsDistinctFromOcrSuccess()
    {
        var result = DocumentDetailsProjection.Create(Document(), [
            Page(1, "PaddleOcr", "recognized", 10, true, "Good"),
            Page(2, "None", "", 0, true, "Missing")
        ]);
        Assert.Equal(2, result.OcrAttemptedCount);
        Assert.Equal(1, result.OcrSuccessCount);
    }

    [Fact]
    public void LegacyHybrid_MarkersAndPlaceholderDoNotInflateCounts()
    {
        const string text = "===== PAGE 1 [PdfPig] =====\nabc\n" +
            "===== PAGE 2 [None] =====\n[No text extracted]";
        var result = DocumentDetailsProjection.Create(Document(text), []);
        Assert.False(result.UsesPersistedDiagnostics);
        Assert.Equal(2, result.Pages.Count);
        Assert.Equal(3, result.CharacterCount);
        Assert.Null(result.Pages[0].OcrAttempted);
        Assert.Equal("NotEvaluated", result.Pages[1].QualityStatus);
        Assert.Empty(result.Pages[1].Text);
    }

    [Fact]
    public void LegacyPhase2B_SupportsPageDelimiters()
    {
        var result = DocumentDetailsProjection.Create(
            Document("--- Page 1 ---\nhello\n--- Page 2 ---\nworld"), []);
        Assert.Equal(2, result.Pages.Count);
        Assert.Equal(10, result.CharacterCount);
        Assert.All(result.Pages, p => Assert.Equal("Unknown", p.Source));
    }

    [Fact]
    public void LegacyPlainText_FallsBackToSinglePage()
    {
        var result = DocumentDetailsProjection.Create(Document("old text"), []);
        Assert.Single(result.Pages);
        Assert.Equal(8, result.CharacterCount);
    }

    [Fact]
    public void EmptyLegacyDocument_DoesNotInventPages()
    {
        var result = DocumentDetailsProjection.Create(Document(), []);
        Assert.Empty(result.Pages);
        Assert.Equal(0, result.CharacterCount);
    }
}
