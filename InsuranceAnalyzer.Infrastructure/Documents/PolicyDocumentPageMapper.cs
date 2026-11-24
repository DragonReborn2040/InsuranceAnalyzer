
using System.Text.Json;
using InsuranceAnalyzer.Application.Documents;
using InsuranceAnalyzer.Application.Documents.Extraction;
using InsuranceAnalyzer.Core.Entities;

namespace InsuranceAnalyzer.Infrastructure.Documents;

public static class PolicyDocumentPageMapper
{
    public static PolicyDocumentPage ToEntity(
        int documentId,
        DocumentPageExtractionResult page)
    {
        ArgumentNullException.ThrowIfNull(page);

        if (documentId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(documentId));

        if (page.PageNumber <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Page number must be greater than zero.");

        var quality = page.Quality
            ?? throw new InvalidOperationException(
                $"Quality validation is missing for page " +
                $"{page.PageNumber}.");

        return new PolicyDocumentPage
        {
            PolicyDocumentId = documentId,
            PageNumber = page.PageNumber,

            ExtractionSource = page.Source.ToString(),

            // Store actual extracted text, not display markers.
            ExtractedText = page.Text,

            CharacterCount = quality.CharacterCount,
            AlphanumericCount = quality.AlphanumericCount,
            SuspiciousCharacterCount =
                quality.SuspiciousCharacterCount,
            SuspiciousCharacterRatio =
                quality.SuspiciousCharacterRatio,

            QualityStatus = quality.QualityStatus,

            OcrAttempted = page.OcrAttempted,

            QualityWarningsJson =
                JsonSerializer.Serialize(quality.Warnings)
        };
    }

    public static PolicyDocumentPageDto ToDto(
        PolicyDocumentPage entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        IReadOnlyList<DocumentQualityWarning> warnings = [];

        if (!string.IsNullOrWhiteSpace(
                entity.QualityWarningsJson))
        {
            warnings =
                JsonSerializer.Deserialize<
                    List<DocumentQualityWarning>>(
                    entity.QualityWarningsJson)
                ?? [];
        }

        return new PolicyDocumentPageDto(
            entity.Id,
            entity.PolicyDocumentId,
            entity.PageNumber,
            entity.ExtractionSource,
            entity.ExtractedText,
            entity.CharacterCount,
            entity.AlphanumericCount,
            entity.SuspiciousCharacterCount,
            entity.SuspiciousCharacterRatio,
            entity.QualityStatus,
            entity.OcrAttempted,
            warnings);
    }
}
