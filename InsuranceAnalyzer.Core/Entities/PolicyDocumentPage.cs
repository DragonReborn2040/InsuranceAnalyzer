namespace InsuranceAnalyzer.Core.Entities;

public sealed class PolicyDocumentPage
{
    public int Id { get; set; }

    public int PolicyDocumentId { get; set; }

    public PolicyDocument PolicyDocument { get; set; } = null!;

    public int PageNumber { get; set; }

    // None, PdfPig, or PaddleOcr
    public string ExtractionSource { get; set; } = "None";

    // The actual extracted text, excluding UI placeholders.
    public string ExtractedText { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int AlphanumericCount { get; set; }

    public int SuspiciousCharacterCount { get; set; }

    public double SuspiciousCharacterRatio { get; set; }

    // Good, NeedsReview, or Missing
    public string QualityStatus { get; set; } = "Missing";

    // Serialized DocumentQualityWarning collection.
    public string? QualityWarningsJson { get; set; }

    // True when OCR was attempted, even if it returned no text.
    public bool OcrAttempted { get; set; }
}