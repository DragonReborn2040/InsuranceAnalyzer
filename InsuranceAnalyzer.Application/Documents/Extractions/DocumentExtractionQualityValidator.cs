
using InsuranceAnalyzer.Application.Documents.Extraction;
using Microsoft.Extensions.Options;

namespace InsuranceAnalyzer.Infrastructure.Documents.Extraction;

public sealed class DocumentExtractionQualityValidator
    : IDocumentExtractionQualityValidator
{
    private readonly DocumentQualityValidationOptions _options;

    private const string AllowedFormattingCharacters =
        ".,;:!?()[]{}+-=/\\%₹$€£¥'\"&@°–—•…|_";

    public DocumentExtractionQualityValidator(
        IOptions<DocumentQualityValidationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;

        if (_options.MinimumAlphanumericCharacters < 0 ||
            _options.MinimumCharactersForSymbolCheck < 0 ||
            _options.MaximumRepeatedCharacterRun < 2 ||
            _options.MaximumSuspiciousCharacterRatio is < 0 or > 1 ||
            double.IsNaN(_options.MaximumSuspiciousCharacterRatio))
        {
            throw new ArgumentException(
                "Invalid document quality validation settings.",
                nameof(options));
        }
    }

    public DocumentPageQualityResult Validate(
        DocumentPageExtractionResult page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var text = page.Text ?? string.Empty;

        var warnings = new List<DocumentQualityWarning>();

        int characterCount = text.Length;

        int alphanumericCount =
            text.Count(char.IsLetterOrDigit);

        int nonWhitespaceCount =
            text.Count(c => !char.IsWhiteSpace(c));

        int suspiciousCount =
            text.Count(IsSuspiciousCharacter);

        double suspiciousRatio =
            nonWhitespaceCount == 0
                ? 0
                : (double)suspiciousCount / nonWhitespaceCount;

        // Rule 1: Missing text.
        if (string.IsNullOrWhiteSpace(text) ||
            alphanumericCount == 0)
        {
            warnings.Add(new DocumentQualityWarning(
                "NO_TEXT",
                "No readable letters or digits were extracted.",
                "Error"));
        }
        else
        {
            // Rule 2: Suspiciously short text.
            if (alphanumericCount <
                _options.MinimumAlphanumericCharacters)
            {
                warnings.Add(new DocumentQualityWarning(
                    "SHORT_TEXT",
                    $"Page contains only {alphanumericCount} " +
                    "letters or digits.",
                    "Warning"));
            }

            // Rule 3: Unusually high suspicious-symbol ratio.
            if (nonWhitespaceCount >=
                    _options.MinimumCharactersForSymbolCheck &&
                suspiciousRatio >
                    _options.MaximumSuspiciousCharacterRatio)
            {
                warnings.Add(new DocumentQualityWarning(
                    "SUSPICIOUS_SYMBOLS",
                    "Page contains an unusually high proportion " +
                    "of unexpected symbols.",
                    "Warning"));
            }
        }

        // Rule 4: Unicode replacement characters.
        if (text.Contains('\uFFFD'))
        {
            warnings.Add(new DocumentQualityWarning(
                "INVALID_CHARACTERS",
                "Text contains Unicode replacement characters.",
                "Warning"));
        }

        // Rule 5: Excessive repeated characters.
        if (HasSuspiciousRepeatedCharacters(
                text,
                _options.MaximumRepeatedCharacterRun))
        {
            warnings.Add(new DocumentQualityWarning(
                "REPEATED_TEXT",
                "Text contains an unusually long repetition " +
                "of the same character.",
                "Warning"));
        }

        // Rule 6: Unreadable output attributed to OCR.
        if (page.Source == DocumentExtractionSource.PaddleOcr &&
            alphanumericCount == 0)
        {
            warnings.Add(new DocumentQualityWarning(
                "OCR_EMPTY",
                "The OCR result contains no readable text.",
                "Error"));
        }

        // Rule 7: OCR results requiring additional review.
        if (page.Source == DocumentExtractionSource.PaddleOcr &&
            warnings.Any(x =>
                x.Code is "SUSPICIOUS_SYMBOLS"
                    or "INVALID_CHARACTERS"
                    or "REPEATED_TEXT"))
        {
            warnings.Add(new DocumentQualityWarning(
                "OCR_REVIEW_REQUIRED",
                "OCR output may require manual verification.",
                "Warning"));
        }

        return new DocumentPageQualityResult
        {
            PageNumber = page.PageNumber,
            Source = page.Source,
            CharacterCount = characterCount,
            AlphanumericCount = alphanumericCount,
            SuspiciousCharacterCount = suspiciousCount,
            SuspiciousCharacterRatio = suspiciousRatio,
            Warnings = warnings
        };
    }

    private static bool IsSuspiciousCharacter(char character)
    {
        if (char.IsLetterOrDigit(character) ||
            char.IsWhiteSpace(character))
        {
            return false;
        }

        if (AllowedFormattingCharacters.Contains(character))
        {
            return false;
        }

        // Replacement characters are reported separately.
        if (character == '\uFFFD')
        {
            return false;
        }

        return char.IsSymbol(character) ||
               char.IsPunctuation(character) ||
               char.IsControl(character);
    }

    private static bool HasSuspiciousRepeatedCharacters(
        string text,
        int threshold)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        int runLength = 1;

        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] == text[i - 1])
            {
                runLength++;

                if (runLength >= threshold &&
                    IsSuspiciousRepeatedCharacter(text[i]))
                {
                    return true;
                }
            }
            else
            {
                runLength = 1;
            }
        }

        return false;
    }

    private static bool IsSuspiciousRepeatedCharacter(
        char character)
    {
        // Exclude repeated digits because numeric financial
        // tables can contain long numbers and zero sequences.
        // Exclude hyphens used in document separators.

        return char.IsLetter(character) ||
               character is '#' or '@' or '\uFFFD';
    }
}
