namespace InsuranceAnalyzer.Application.Documents.Extraction;

public sealed class DocumentQualityValidationOptions
{
    public const string SectionName =
        "DocumentQualityValidation";

    public int MinimumAlphanumericCharacters { get; set; } = 50;

    public int MinimumCharactersForSymbolCheck { get; set; } = 30;

    public double MaximumSuspiciousCharacterRatio
    { get; set; } = 0.35;

    public int MaximumRepeatedCharacterRun
    { get; set; } = 8;
}