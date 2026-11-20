namespace InsuranceAnalyzer.Core.Entities;

public sealed class PolicyBenefit
{
    public int Id { get; set; }
    public int InsurancePolicyId { get; set; }
    public InsurancePolicy? InsurancePolicy { get; set; }
    public string BenefitType { get; set; } = string.Empty; // Guaranteed, Bonus, Survival, Maturity, Rider...
    public string Description { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public int? ApplicablePolicyYear { get; set; }
    public bool IsGuaranteed { get; set; }
    public string? SourceClauseReference { get; set; }
}
