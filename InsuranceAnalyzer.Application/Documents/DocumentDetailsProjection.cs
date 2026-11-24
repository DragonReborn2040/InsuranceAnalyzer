using System.Text.RegularExpressions;
using InsuranceAnalyzer.Application.Documents.Extraction;

namespace InsuranceAnalyzer.Application.Documents;

/// <summary>
/// Read-only presentation projection. Never changes persisted extraction results.
/// </summary>
public static class DocumentDetailsProjection
{
    public sealed record Page(
        int PageNumber,
        string Text,
        string Source,
        int CharacterCount,
        bool? OcrAttempted,
        string QualityStatus,
        IReadOnlyList<DocumentQualityWarning> Warnings,
        bool HasPersistedDiagnostics);

    public sealed record Result(
        IReadOnlyList<Page> Pages,
        bool UsesPersistedDiagnostics)
    {
        public int CharacterCount => Pages.Sum(x => x.CharacterCount);
        public int OcrAttemptedCount => Pages.Count(x => x.OcrAttempted == true);
        public int OcrSuccessCount => Pages.Count(x =>
            string.Equals(x.Source, "PaddleOcr", StringComparison.OrdinalIgnoreCase));
        public int NeedsReviewCount => Pages.Count(x => x.QualityStatus == "NeedsReview");
        public int MissingCount => Pages.Count(x => x.QualityStatus == "Missing");
        public int WarningCount => Pages.Sum(x => x.Warnings.Count);
    }

    public static Result Create(
        PolicyDocumentDto document,
        IReadOnlyList<PolicyDocumentPageDto>? persistedPages)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (persistedPages is { Count: > 0 })
        {
            var pages = persistedPages
                .OrderBy(x => x.PageNumber)
                .Select(x => new Page(
                    x.PageNumber,
                    x.ExtractedText ?? string.Empty,
                    x.ExtractionSource,
                    x.CharacterCount,
                    x.OcrAttempted,
                    x.QualityStatus,
                    x.QualityWarnings ?? [],
                    true))
                .ToList();

            return new Result(pages, true);
        }

        return new Result(ParseLegacy(document.ExtractedText), false);
    }

    private static IReadOnlyList<Page> ParseLegacy(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        const string hybridPattern =
            @"^===== PAGE (?<page>\d+) \[(?<source>[^\]]+)\] =====[ \t]*\r?\n(?<text>.*?)(?=^===== PAGE \d+ \[[^\]]+\] =====|\z)";

        var hybridMatches = Regex.Matches(text, hybridPattern,
            RegexOptions.Multiline | RegexOptions.Singleline);
        if (hybridMatches.Count > 0)
        {
            return hybridMatches.Select(match => MakeLegacyPage(
                int.Parse(match.Groups["page"].Value),
                match.Groups["text"].Value,
                match.Groups["source"].Value))
                .OrderBy(x => x.PageNumber).ToList();
        }

        const string legacyPattern =
            @"--- Page (?<page>\d+) ---\s*(?<text>.*?)(?=--- Page \d+ ---|\z)";
        var legacyMatches = Regex.Matches(text, legacyPattern,
            RegexOptions.Singleline);
        if (legacyMatches.Count > 0)
        {
            return legacyMatches.Select(match => MakeLegacyPage(
                int.Parse(match.Groups["page"].Value),
                match.Groups["text"].Value, "Unknown"))
                .OrderBy(x => x.PageNumber).ToList();
        }

        return [MakeLegacyPage(1, text, "Unknown")];
    }

    private static Page MakeLegacyPage(int pageNumber, string rawText, string source)
    {
        var content = rawText.Trim();
        var hasText = !string.IsNullOrWhiteSpace(content) &&
                      !string.Equals(content, "[No text extracted]",
                          StringComparison.OrdinalIgnoreCase);
        return new Page(
            pageNumber,
            hasText ? content : string.Empty,
            source,
            hasText ? content.Length : 0,
            null, // Legacy format cannot prove whether OCR was attempted.
            "NotEvaluated",
            [],
            false);
    }
}
