namespace InsuranceAnalyzer.Core.Entities;

public sealed class InsuranceProduct
{
    public int Id { get; set; }
    public int InsurerId { get; set; }
    public Insurer? Insurer { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? PlanType { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<InsurancePolicy> Policies { get; set; } = new List<InsurancePolicy>();
}
