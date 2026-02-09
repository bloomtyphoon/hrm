using HRM.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for AccountRole join entity.
///
/// Table: Identity.AccountRoles
/// Matches existing database schema from migration 004_CreateAccountRolesTable.sql
/// </summary>
internal sealed class AccountRoleConfiguration : IEntityTypeConfiguration<AccountRole>
{
    public void Configure(EntityTypeBuilder<AccountRole> builder)
    {
        builder.ToTable("AccountRoles");

        // Ignore the base Entity.Id property since this table uses composite PK
        builder.Ignore(ar => ar.Id);

        // Composite primary key matching the SQL schema
        builder.HasKey(ar => new { ar.AccountId, ar.RoleId });

        builder.Property(ar => ar.AccountId)
            .IsRequired();

        builder.Property(ar => ar.RoleId)
            .IsRequired();

        builder.Property(ar => ar.AssignedAtUtc)
            .IsRequired()
            .HasColumnType("DATETIME2(7)")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ar => ar.AssignedById);

        // Index for role-based lookups
        builder.HasIndex(ar => ar.RoleId)
            .HasDatabaseName("IX_AccountRoles_RoleId");

        // Index for assignment audit
        builder.HasIndex(ar => ar.AssignedAtUtc)
            .HasDatabaseName("IX_AccountRoles_AssignedAtUtc");
    }
}
