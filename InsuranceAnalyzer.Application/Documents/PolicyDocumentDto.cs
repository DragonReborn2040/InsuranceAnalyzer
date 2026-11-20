namespace InsuranceAnalyzer.Application.Documents;

public sealed record PolicyDocumentDto(
    int Id,
    int InsurancePolicyId,
    string PolicyNumber,
    string PolicyholderName,
    string OriginalFileName,
    long FileSizeBytes,
    string ProcessingStatus,
    DateTime UploadedUtc,
    int? PageCount,
    string? ExtractedText,
    string? ProcessingError);