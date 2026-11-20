namespace InsuranceAnalyzer.Core.Entities;

public sealed class PolicyClause
{
    public int Id { get; set; }
    public int InsurancePolicyId { get; set; }
    public InsurancePolicy? InsurancePolicy { get; set; }
    public string ClauseType { get; set; } = string.Empty; // Surrender, Maturity, SurvivalBenefit, Adjustment...
    public string ClauseText { get; set; } = string.Empty;
    public int? PdfPageNumber { get; set; }
    public string ExtractionStatus { get; set; } = "NeedsReview";
    public decimal? ConfidenceScore { get; set; }
}
