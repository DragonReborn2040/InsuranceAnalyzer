using InsuranceAnalyzer.Core.Entities;

namespace InsuranceAnalyzer.Application.Policies;

public interface IPolicyService
{
    Task<IReadOnlyList<InsurancePolicy>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InsurancePolicy> CreateAsync(InsurancePolicy policy, CancellationToken cancellationToken = default);
}
