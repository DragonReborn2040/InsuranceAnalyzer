namespace InsuranceAnalyzer.Core.Entities;

public sealed class Insurer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? CustomerCarePhone { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<InsurancePolicy> Policies { get; set; } = new List<InsurancePolicy>();
    public ICollection<InsuranceProduct> Products { get; set; } = new List<InsuranceProduct>();
}
