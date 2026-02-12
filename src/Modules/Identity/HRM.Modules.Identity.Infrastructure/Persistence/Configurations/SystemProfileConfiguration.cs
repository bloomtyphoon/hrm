using HRM.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for SystemProfile.
///
/// Table: Identity.SystemProfiles
/// Primary Key: Id (GUID)
/// Unique Constraint: AccountId (one profile per account)
/// </summary>
internal sealed class SystemProfileConfiguration : IEntityTypeConfiguration<SystemProfile>
{
    public void Configure(EntityTypeBuilder<SystemProfile> builder)
    {
        builder.ToTable("SystemProfiles");

        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.AccountId)
            .IsRequired();

        builder.Property(sp => sp.IsSuperAdmin)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sp => sp.Department)
            .HasMaxLength(200)
            .HasColumnType("NVARCHAR(200)");

        builder.Property(sp => sp.JobTitle)
            .HasMaxLength(200)
            .HasColumnType("NVARCHAR(200)");

        builder.Property(sp => sp.Notes)
            .HasMaxLength(2000)
            .HasColumnType("NVARCHAR(2000)");

        // One-to-one: Account -> SystemProfile
        builder.HasIndex(sp => sp.AccountId)
            .IsUnique()
            .HasDatabaseName("IX_SystemProfiles_AccountId");
    }
}
