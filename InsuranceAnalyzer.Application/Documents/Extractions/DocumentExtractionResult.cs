namespace InsuranceAnalyzer.Application.Documents.Extraction;

public sealed class DocumentExtractionResult
{
    public int PageCount { get; init; }

    public IReadOnlyList<DocumentPageExtractionResult> Pages
    { get; init; } = [];

    public string FullText { get; init; } = string.Empty;

    public int PdfPigPageCount =>
        Pages.Count(x =>
            x.Source == DocumentExtractionSource.PdfPig);

    public int OcrPageCount =>
        Pages.Count(x =>
            x.Source == DocumentExtractionSource.PaddleOcr);

    public int EmptyPageCount =>
        Pages.Count(x =>
            x.Source == DocumentExtractionSource.None);

    // Actual extracted characters, excluding page markers.
    public int CharacterCount =>
        Pages.Sum(x => x.CharacterCount);

    public int OcrAttemptedPageCount =>
        Pages.Count(x => x.OcrAttempted);

    // Preserve existing meaning: OCR contributed usable text.
    public bool UsedOcr => OcrPageCount > 0;

    public bool HasText =>
        Pages.Any(x => x.HasMeaningfulText);

    // Phase 2D.2B quality summaries.
    public int GoodPageCount =>
        Pages.Count(x => x.QualityStatus == "Good");

    public int NeedsReviewPageCount =>
        Pages.Count(x => x.QualityStatus == "NeedsReview");

    public int MissingPageCount =>
        Pages.Count(x => x.QualityStatus == "Missing");

    public int WarningCount =>
        Pages.Sum(x => x.QualityWarnings.Count);

    public int NotEvaluatedPageCount =>
        Pages.Count(x => x.Quality is null);
}