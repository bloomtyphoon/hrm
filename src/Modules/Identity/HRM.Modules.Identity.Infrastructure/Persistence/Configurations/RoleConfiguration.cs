using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.ValueObjects;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for Role aggregate.
///
/// Table: Identity.Roles
/// Primary Key: Id (GUID)
/// Unique Constraint: Name
/// Owned Entity: RolePermission (stored as JSON column)
/// </summary>
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("NVARCHAR(100)");

        builder.Property(r => r.Description)
            .HasMaxLength(500)
            .HasColumnType("NVARCHAR(500)");

        builder.Property(r => r.IsSystemRole)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CompanyId)
            .IsRequired(false);

        // Unique index on (Name, CompanyId) - role name unique within same company scope
        builder.HasIndex(r => new { r.Name, r.CompanyId })
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name_CompanyId");

        builder.HasIndex(r => r.IsSystemRole)
            .HasDatabaseName("IX_Roles_IsSystemRole");

        builder.HasIndex(r => r.CompanyId)
            .HasDatabaseName("IX_Roles_CompanyId");

        builder.HasIndex(r => r.CreatedAtUtc)
            .HasDatabaseName("IX_Roles_CreatedAtUtc");

        // Configure permissions as owned collection stored in a separate table
        builder.OwnsMany(r => r.Permissions, permissionBuilder =>
        {
            permissionBuilder.ToTable("RolePermissions");

            permissionBuilder.WithOwner().HasForeignKey("RoleId");

            permissionBuilder.Property(p => p.Module)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("NVARCHAR(100)");

            permissionBuilder.Property(p => p.Entity)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("NVARCHAR(100)");

            permissionBuilder.Property(p => p.Action)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("NVARCHAR(100)");

            permissionBuilder.Property(p => p.Scope)
                .HasConversion<int?>()
                .HasColumnName("Scope");

            permissionBuilder.HasIndex("RoleId", "Module", "Entity", "Action", "Scope")
                .IsUnique()
                .HasDatabaseName("IX_RolePermissions_Unique");
        });

        // Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}
