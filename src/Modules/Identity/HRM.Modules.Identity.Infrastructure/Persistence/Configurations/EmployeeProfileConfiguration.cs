using HRM.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for EmployeeProfile.
///
/// Table: Identity.EmployeeProfiles
/// Primary Key: Id (GUID)
/// Unique Constraints: AccountId, EmployeeId (one profile per account/employee)
/// </summary>
internal sealed class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        builder.ToTable("EmployeeProfiles");

        builder.HasKey(ep => ep.Id);

        // TenantId: required for tenant isolation
        // Global query filter (tenant) applied by ModuleDbContext
        builder.Property(ep => ep.TenantId)
            .IsRequired();

        builder.HasIndex(ep => ep.TenantId)
            .HasDatabaseName("IX_EmployeeProfiles_TenantId");

        builder.Property(ep => ep.AccountId)
            .IsRequired();

        builder.Property(ep => ep.EmployeeId)
            .IsRequired();

        builder.Property(ep => ep.DefaultScopeLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ep => ep.CanAccessAllAssignedCompanies)
            .IsRequired()
            .HasDefaultValue(true);

        // One-to-one: Account -> EmployeeProfile
        builder.HasIndex(ep => ep.AccountId)
            .IsUnique()
            .HasDatabaseName("IX_EmployeeProfiles_AccountId");

        // One-to-one: Employee -> EmployeeProfile
        builder.HasIndex(ep => ep.EmployeeId)
            .IsUnique()
            .HasDatabaseName("IX_EmployeeProfiles_EmployeeId");

        // Company access collection (denormalized from Personnel.EmployeeAssignments)
        builder.OwnsMany(ep => ep.CompanyAccess, companyBuilder =>
        {
            companyBuilder.ToTable("EmployeeProfileCompanies");

            companyBuilder.WithOwner().HasForeignKey("EmployeeProfileId");

            companyBuilder.Property(ca => ca.CompanyId)
                .IsRequired();

            companyBuilder.HasIndex("EmployeeProfileId", "CompanyId")
                .IsUnique()
                .HasDatabaseName("IX_EmployeeProfileCompanies_Unique");

            companyBuilder.HasIndex(ca => ca.CompanyId)
                .HasDatabaseName("IX_EmployeeProfileCompanies_CompanyId");
        });
    }
}
