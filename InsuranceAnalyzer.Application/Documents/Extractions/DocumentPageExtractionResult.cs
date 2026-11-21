namespace InsuranceAnalyzer.Application.Documents.Extraction;

public sealed class DocumentPageExtractionResult
{
    public int PageNumber { get; init; }

    public string Text { get; init; } = string.Empty;

    public DocumentExtractionSource Source { get; init; }

    public bool HasMeaningfulText =>
        !string.IsNullOrWhiteSpace(Text);

    public int CharacterCount => Text?.Length ?? 0;
}