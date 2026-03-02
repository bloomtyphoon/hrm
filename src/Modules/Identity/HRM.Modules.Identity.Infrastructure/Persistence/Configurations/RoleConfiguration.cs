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
/// Unique Constraint: (TenantId, Name, CompanyId)
/// Owned Entity: RolePermission (stored in separate table)
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

        // TenantId: required for tenant isolation
        // Global query filter (soft delete + tenant) applied by ModuleDbContext
        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.HasIndex(r => r.TenantId)
            .HasDatabaseName("IX_Roles_TenantId");

        // Unique index: role name unique within tenant + optional company scope
        builder.HasIndex(r => new { r.TenantId, r.Name, r.CompanyId })
            .IsUnique()
            .HasDatabaseName("IX_Roles_TenantId_Name_CompanyId");

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

        // NOTE: Soft delete + tenant query filter is applied globally by ModuleDbContext.
        // Do NOT add HasQueryFilter here — it would overwrite the combined global filter.
    }
}
