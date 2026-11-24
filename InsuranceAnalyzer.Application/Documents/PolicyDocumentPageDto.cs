using InsuranceAnalyzer.Application.Documents.Extraction;

namespace InsuranceAnalyzer.Application.Documents;

public sealed record PolicyDocumentPageDto(
    int Id,
    int PolicyDocumentId,
    int PageNumber,
    string ExtractionSource,
    string ExtractedText,
    int CharacterCount,
    int AlphanumericCount,
    int SuspiciousCharacterCount,
    double SuspiciousCharacterRatio,
    string QualityStatus,
    bool OcrAttempted,
    IReadOnlyList<DocumentQualityWarning> QualityWarnings);