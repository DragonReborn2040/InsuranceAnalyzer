namespace InsuranceAnalyzer.Application.Documents.Extraction;

public interface IDocumentExtractionQualityValidator
{
    DocumentPageQualityResult Validate(
        DocumentPageExtractionResult page);
}