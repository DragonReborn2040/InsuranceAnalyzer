namespace InsuranceAnalyzer.Application.Documents;

public sealed record PolicyDocumentFile(
    Stream Content,
    string ContentType,
    string OriginalFileName);