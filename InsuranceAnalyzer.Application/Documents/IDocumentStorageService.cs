namespace InsuranceAnalyzer.Application.Documents;

public interface IDocumentStorageService
{
    Task<string> SaveAsync(
        int policyId,
        string storedFileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    string GetPhysicalPath(string relativePath);
}