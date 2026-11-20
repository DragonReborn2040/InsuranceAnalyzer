namespace InsuranceAnalyzer.Application.Documents;

public sealed record PdfExtractionResult(
    string Text,
    int PageCount,
    bool HasExtractableText);