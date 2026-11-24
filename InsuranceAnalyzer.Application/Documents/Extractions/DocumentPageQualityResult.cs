namespace InsuranceAnalyzer.Application.Documents.Extraction;

public sealed class DocumentPageQualityResult
{
    public int PageNumber { get; init; }

    public DocumentExtractionSource Source { get; init; }

    public int CharacterCount { get; init; }

    public int AlphanumericCount { get; init; }

    public int SuspiciousCharacterCount { get; init; }

    public double SuspiciousCharacterRatio { get; init; }

    public IReadOnlyList<DocumentQualityWarning> Warnings
    { get; init; } = [];

    public bool HasWarnings => Warnings.Count > 0;

    public string QualityStatus =>
        Warnings.Any(x => x.Severity == "Error")
            ? "Missing"
            : HasWarnings
                ? "NeedsReview"
                : "Good";
}