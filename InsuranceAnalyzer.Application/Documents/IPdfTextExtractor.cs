namespace InsuranceAnalyzer.Application.Documents;

public interface IPdfTextExtractor
{
    Task<PdfExtractionResult> ExtractAsync(
        string physicalFilePath,
        CancellationToken cancellationToken = default);
}