namespace InsuranceAnalyzer.Core.Entities;

public sealed class PolicyCalculation
{
    public int Id { get; set; }
    public int InsurancePolicyId { get; set; }
    public InsurancePolicy? InsurancePolicy { get; set; }
    public int PolicyYear { get; set; }
    public decimal? GuaranteedSurrenderValue { get; set; }
    public decimal? SpecialSurrenderValue { get; set; }
    public decimal? GrossSurrenderValue { get; set; }
    public decimal? SurrenderAdjustment { get; set; }
    public decimal? NetSurrenderValue { get; set; }
    public decimal? GrossMaturityValue { get; set; }
    public decimal? MaturityAdjustment { get; set; }
    public decimal? NetMaturityValue { get; set; }
    public string CalculationStatus { get; set; } = "Draft";
    public string? CalculationNotes { get; set; }
    public DateTime CalculatedUtc { get; set; } = DateTime.UtcNow;
}
