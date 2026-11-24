namespace InsuranceAnalyzer.Core.Entities;

public sealed class PolicyDocument
{
    public int Id { get; set; }

    public int InsurancePolicyId { get; set; }

    public InsurancePolicy? InsurancePolicy { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long FileSizeBytes { get; set; }

    public string? Sha256 { get; set; }

    public string ProcessingStatus { get; set; } = "Pending";

    public DateTime UploadedUtc { get; set; } = DateTime.UtcNow;

    // Phase 2: Existing combined extraction results.
    // Retain for backward compatibility.
    public string? ExtractedText { get; set; }

    public int? PageCount { get; set; }

    public string? ProcessingError { get; set; }

    // Phase 2D.2: Structured page-level results.
    public ICollection<PolicyDocumentPage> Pages { get; set; }
        = new List<PolicyDocumentPage>();
}