namespace InsuranceAnalyzer.Application.Documents.Extraction;

public sealed class DocumentExtractionResult
{
    public int PageCount { get; init; }

    public IReadOnlyList<DocumentPageExtractionResult> Pages { get; init; }
        = Array.Empty<DocumentPageExtractionResult>();

    public string FullText { get; init; } = string.Empty;

    public int PdfPigPageCount =>
        Pages.Count(x => x.Source == DocumentExtractionSource.PdfPig);

    public int OcrPageCount =>
        Pages.Count(x => x.Source == DocumentExtractionSource.PaddleOcr);

    public int EmptyPageCount =>
        Pages.Count(x => x.Source == DocumentExtractionSource.None);

    public int CharacterCount => FullText.Length;

    public bool UsedOcr => OcrPageCount > 0;

    public bool HasText =>
        Pages.Any(x => x.HasMeaningfulText);
}