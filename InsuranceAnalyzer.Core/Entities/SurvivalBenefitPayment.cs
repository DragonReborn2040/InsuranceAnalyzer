namespace InsuranceAnalyzer.Core.Entities;

public sealed class SurvivalBenefitPayment
{
    public int Id { get; set; }
    public int InsurancePolicyId { get; set; }
    public InsurancePolicy? InsurancePolicy { get; set; }
    public DateTime PaymentDate { get; set; }
    public int? PolicyYear { get; set; }
    public decimal Amount { get; set; }
    public string? EvidenceOrReference { get; set; }
    public string VerificationStatus { get; set; } = "Unverified";
}
