using InsuranceAnalyzer.Application.Policies;
using InsuranceAnalyzer.Core.Entities;
using InsuranceAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InsuranceAnalyzer.Infrastructure.Policies;

public sealed class PolicyService(IDbContextFactory<InsuranceAnalyzerDbContext> contextFactory) : IPolicyService
{
    public async Task<IReadOnlyList<InsurancePolicy>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.InsurancePolicies
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<InsurancePolicy> CreateAsync(InsurancePolicy policy, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        policy.CreatedUtc = DateTime.UtcNow;
        policy.UpdatedUtc = DateTime.UtcNow;
        dbContext.InsurancePolicies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }
}
