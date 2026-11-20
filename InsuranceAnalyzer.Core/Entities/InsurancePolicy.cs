namespace InsuranceAnalyzer.Core.Entities;

public sealed class InsurancePolicy
{
    public int Id { get; set; }
    public int? InsurerId { get; set; }
    public Insurer? Insurer { get; set; }
    public int? InsuranceProductId { get; set; }
    public InsuranceProduct? InsuranceProduct { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyholderName { get; set; } = string.Empty;
    public string? PolicyName { get; set; }
    public DateTime? CommencementDate { get; set; }
    public int? PolicyTermYears { get; set; }
    public int? PremiumPayingTermYears { get; set; }
    public decimal? AnnualizedPremium { get; set; }
    public string PremiumFrequency { get; set; } = "Annual";
    public string CurrencyCode { get; set; } = "INR";
    public string AnalysisStatus { get; set; } = "NotStarted";
    public string? Notes { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<PolicyDocument> Documents { get; set; } = new List<PolicyDocument>();
    public ICollection<PolicyBenefit> Benefits { get; set; } = new List<PolicyBenefit>();
    public ICollection<PremiumSchedule> PremiumSchedules { get; set; } = new List<PremiumSchedule>();
    public ICollection<SurvivalBenefitPayment> SurvivalBenefitPayments { get; set; } = new List<SurvivalBenefitPayment>();
    public ICollection<PolicyClause> Clauses { get; set; } = new List<PolicyClause>();
    public ICollection<PolicyCalculation> Calculations { get; set; } = new List<PolicyCalculation>();
}
