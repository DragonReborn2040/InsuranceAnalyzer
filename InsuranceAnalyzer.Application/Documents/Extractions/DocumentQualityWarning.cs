namespace InsuranceAnalyzer.Application.Documents.Extraction;

public sealed record DocumentQualityWarning(
    string Code,
    string Message,
    string Severity);