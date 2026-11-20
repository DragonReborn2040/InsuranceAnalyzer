using InsuranceAnalyzer.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsuranceAnalyzer.Infrastructure.Persistence;

public sealed class InsuranceAnalyzerDbContext(DbContextOptions<InsuranceAnalyzerDbContext> options)
    : DbContext(options)
{
    public DbSet<Insurer> Insurers => Set<Insurer>();
    public DbSet<InsuranceProduct> InsuranceProducts => Set<InsuranceProduct>();
    public DbSet<InsurancePolicy> InsurancePolicies => Set<InsurancePolicy>();
    public DbSet<PolicyDocument> PolicyDocuments => Set<PolicyDocument>();
    public DbSet<PolicyBenefit> PolicyBenefits => Set<PolicyBenefit>();
    public DbSet<PremiumSchedule> PremiumSchedules => Set<PremiumSchedule>();
    public DbSet<SurvivalBenefitPayment> SurvivalBenefitPayments => Set<SurvivalBenefitPayment>();
    public DbSet<PolicyClause> PolicyClauses => Set<PolicyClause>();
    public DbSet<PolicyCalculation> PolicyCalculations => Set<PolicyCalculation>();
    public DbSet<CalculationAdjustment> CalculationAdjustments => Set<CalculationAdjustment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Insurer>(e =>
        {
            e.ToTable("Insurers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Website).HasMaxLength(500);
            e.Property(x => x.CustomerCarePhone).HasMaxLength(50);
        });

        modelBuilder.Entity<InsuranceProduct>(e =>
        {
            e.ToTable("InsuranceProducts");
            e.HasKey(x => x.Id);
            e.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
            e.Property(x => x.ProductCode).HasMaxLength(100);
            e.Property(x => x.PlanType).HasMaxLength(100);
            e.HasOne(x => x.Insurer).WithMany(x => x.Products).HasForeignKey(x => x.InsurerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InsurancePolicy>(e =>
        {
            e.ToTable("InsurancePolicies");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PolicyNumber).IsUnique();
            e.Property(x => x.PolicyNumber).HasMaxLength(100).IsRequired();
            e.Property(x => x.PolicyholderName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PolicyName).HasMaxLength(250);
            e.Property(x => x.PremiumFrequency).HasMaxLength(30);
            e.Property(x => x.CurrencyCode).HasMaxLength(3);
            e.Property(x => x.AnalysisStatus).HasMaxLength(40);
            e.Property(x => x.AnnualizedPremium).HasPrecision(18, 2);
            e.HasOne(x => x.Insurer).WithMany(x => x.Policies).HasForeignKey(x => x.InsurerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.InsuranceProduct).WithMany(x => x.Policies).HasForeignKey(x => x.InsuranceProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PolicyDocument>(e =>
        {
            e.ToTable("PolicyDocuments");
            e.HasKey(x => x.Id);
            e.Property(x => x.OriginalFileName)
                .HasMaxLength(260)
                .IsRequired();
            e.Property(x => x.StoredFileName)
                .HasMaxLength(260)
                .IsRequired();
            e.Property(x => x.StoragePath)
                .HasMaxLength(1000)
                .IsRequired();
            e.Property(x => x.ContentType)
                .HasMaxLength(100);
            e.Property(x => x.Sha256)
                .HasMaxLength(64);
            e.Property(x => x.ProcessingStatus)
                .HasMaxLength(40);
            e.Property(x => x.ExtractedText);
            e.Property(x => x.ProcessingError)
                .HasMaxLength(2000);
            e.HasIndex(x => x.Sha256);
            e.HasIndex(x => new
            {
                x.InsurancePolicyId,
                x.Sha256
            });
            e.HasOne(x => x.InsurancePolicy)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.InsurancePolicyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyBenefit>(e =>
        {
            e.ToTable("PolicyBenefits");
            e.HasKey(x => x.Id);
            e.Property(x => x.BenefitType).HasMaxLength(60).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.SourceClauseReference).HasMaxLength(300);
            e.HasOne(x => x.InsurancePolicy).WithMany(x => x.Benefits)
                .HasForeignKey(x => x.InsurancePolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PremiumSchedule>(e =>
        {
            e.ToTable("PremiumSchedules");
            e.HasKey(x => x.Id);
            e.Property(x => x.PremiumAmount).HasPrecision(18, 2);
            e.HasOne(x => x.InsurancePolicy).WithMany(x => x.PremiumSchedules)
                .HasForeignKey(x => x.InsurancePolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SurvivalBenefitPayment>(e =>
        {
            e.ToTable("SurvivalBenefitPayments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.VerificationStatus).HasMaxLength(40);
            e.Property(x => x.EvidenceOrReference).HasMaxLength(1000);
            e.HasOne(x => x.InsurancePolicy).WithMany(x => x.SurvivalBenefitPayments)
                .HasForeignKey(x => x.InsurancePolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyClause>(e =>
        {
            e.ToTable("PolicyClauses");
            e.HasKey(x => x.Id);
            e.Property(x => x.ClauseType).HasMaxLength(80).IsRequired();
            e.Property(x => x.ClauseText).IsRequired();
            e.Property(x => x.ExtractionStatus).HasMaxLength(40);
            e.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
            e.HasOne(x => x.InsurancePolicy).WithMany(x => x.Clauses)
                .HasForeignKey(x => x.InsurancePolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyCalculation>(e =>
        {
            e.ToTable("PolicyCalculations");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.InsurancePolicyId, x.PolicyYear }).IsUnique();
            foreach (var property in new[] {
                nameof(PolicyCalculation.GuaranteedSurrenderValue),
                nameof(PolicyCalculation.SpecialSurrenderValue),
                nameof(PolicyCalculation.GrossSurrenderValue),
                nameof(PolicyCalculation.SurrenderAdjustment),
                nameof(PolicyCalculation.NetSurrenderValue),
                nameof(PolicyCalculation.GrossMaturityValue),
                nameof(PolicyCalculation.MaturityAdjustment),
                nameof(PolicyCalculation.NetMaturityValue)
            })
                e.Property<decimal?>(property).HasPrecision(18, 2);
            e.Property(x => x.CalculationStatus).HasMaxLength(40);
            e.HasOne(x => x.InsurancePolicy).WithMany(x => x.Calculations)
                .HasForeignKey(x => x.InsurancePolicyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CalculationAdjustment>(e =>
        {
            e.ToTable("CalculationAdjustments");
            e.HasKey(x => x.Id);
            e.Property(x => x.BenefitType).HasMaxLength(60);
            e.Property(x => x.TargetPayout).HasMaxLength(30);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.RuleStatus).HasMaxLength(40);
            e.Property(x => x.EvidenceClause).HasMaxLength(2000);
            e.HasOne(x => x.PolicyCalculation).WithMany()
                .HasForeignKey(x => x.PolicyCalculationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityName).HasMaxLength(150).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.DetailsJson);
        });
    }
}
