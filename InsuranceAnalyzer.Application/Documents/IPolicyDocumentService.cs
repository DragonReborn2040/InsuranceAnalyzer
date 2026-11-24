using InsuranceAnalyzer.Core.Entities;

namespace InsuranceAnalyzer.Application.Documents;

public interface IPolicyDocumentService
{
    Task<IReadOnlyList<PolicyDocumentDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<PolicyDocumentDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    // Phase 2D.2C: Retrieve structured page diagnostics.
    Task<IReadOnlyList<PolicyDocumentPageDto>> GetPagesAsync(
        int documentId,
        CancellationToken cancellationToken = default);

    Task<PolicyDocument> UploadAsync(
        int insurancePolicyId,
        string originalFileName,
        string contentType,
        long fileSizeBytes,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<PolicyDocumentFile?> GetFileAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);
}