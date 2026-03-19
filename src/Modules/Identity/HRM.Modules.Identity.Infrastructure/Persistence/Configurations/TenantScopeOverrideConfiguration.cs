using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core entity configuration for TenantScopeOverride.
///
/// Table: Identity.TenantScopeOverrides
/// Primary Key: Id (GUID)
/// Unique Constraint: (TenantId, Module, Entity, Action)
/// </summary>
internal sealed class TenantScopeOverrideConfiguration : IEntityTypeConfiguration<TenantScopeOverride>
{
    public void Configure(EntityTypeBuilder<TenantScopeOverride> builder)
    {
        builder.ToTable("TenantScopeOverrides");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Module)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("NVARCHAR(100)");

        builder.Property(x => x.Entity)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("NVARCHAR(100)");

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("NVARCHAR(100)");

        // Store AllowedScopes as JSON array of DataScopeLevel IDs (integers)
        builder.Property(x => x.AllowedScopes)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(
                    v.Select(s => s.Id).ToList(),
                    (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<int>>(
                    v, (System.Text.Json.JsonSerializerOptions?)null)!
                    .Select(i => DataScopeLevel.FromId(i)).ToList())
            .HasColumnType("NVARCHAR(500)")
            .IsRequired();

        // DefaultScope stored as nullable int, converted via DataScopeLevel.FromId
        builder.Property(x => x.DefaultScope)
            .HasConversion(
                v => v != null ? (int?)v.Id : null,
                v => v.HasValue ? DataScopeLevel.FromId(v.Value) : null)
            .IsRequired(false);

        // Unique constraint: one override per (tenant, module, entity, action)
        builder.HasIndex(x => new { x.TenantId, x.Module, x.Entity, x.Action })
            .IsUnique()
            .HasDatabaseName("IX_TenantScopeOverrides_Unique");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_TenantScopeOverrides_TenantId");

        // Ignore computed property
        builder.Ignore(x => x.PermissionKey);
    }
}
