namespace InsuranceAnalyzer.Application.Documents.Extraction;

public interface IPdfPageOcrService
{
    Task<IReadOnlyDictionary<int, string>> ExtractPagesAsync(
        string pdfPath,
        IReadOnlyCollection<int> pageNumbers,
        CancellationToken cancellationToken = default);
}