namespace InsuranceAnalyzer.Application.Documents.Extraction;

public sealed class DocumentPageExtractionResult
{
    public int PageNumber { get; init; }

    public string Text { get; init; } = string.Empty;

    public DocumentExtractionSource Source { get; init; }

    // Phase 2D.2B
    public bool OcrAttempted { get; init; }

    public DocumentPageQualityResult? Quality { get; init; }

    public bool HasMeaningfulText =>
        !string.IsNullOrWhiteSpace(Text);

    public int CharacterCount => Text.Length;

    public string QualityStatus =>
        Quality?.QualityStatus ?? "NotEvaluated";

    public IReadOnlyList<DocumentQualityWarning> QualityWarnings =>
        Quality?.Warnings ?? [];
}