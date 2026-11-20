namespace InsuranceAnalyzer.Core.Entities;

public sealed class PremiumSchedule
{
    public int Id { get; set; }
    public int InsurancePolicyId { get; set; }
    public InsurancePolicy? InsurancePolicy { get; set; }
    public int PolicyYear { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal PremiumAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
}
