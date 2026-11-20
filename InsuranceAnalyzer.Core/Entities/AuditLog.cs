namespace InsuranceAnalyzer.Core.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? DetailsJson { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
