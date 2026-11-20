namespace InsuranceAnalyzer.Core.Entities;

public sealed class CalculationAdjustment
{
    public int Id { get; set; }
    public int PolicyCalculationId { get; set; }
    public PolicyCalculation? PolicyCalculation { get; set; }
    public string BenefitType { get; set; } = "SurvivalBenefit";
    public string TargetPayout { get; set; } = string.Empty; // Surrender, Maturity, Both
    public decimal Amount { get; set; }
    public string RuleStatus { get; set; } = "Unknown"; // Verified, Unknown, Conflict, InsurerConfirmation
    public string? EvidenceClause { get; set; }
    public int? EvidencePageNumber { get; set; }
}
