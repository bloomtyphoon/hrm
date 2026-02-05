using System.Reflection;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using HRM.Modules.Organization.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Organization.Infrastructure.Persistence;

/// <summary>
/// DbContext for Organization module.
/// Inherits from ModuleDbContext for:
/// - Unit of Work pattern
/// - Domain event dispatching
/// - Soft delete query filters
/// - Audit trail (CreatedAtUtc, ModifiedAtUtc)
/// - Outbox pattern (OutboxMessages table)
///
/// Tables:
/// - Organization.Companies: Company entities
/// - Organization.Departments: Department entities
/// - Organization.Positions: Position entities
/// - Organization.OutboxMessages: Integration events for reliable publishing
///
/// Schema Separation:
/// - Schema: "Organization" (isolates from other modules)
/// - Same database: HrmDb (shared with Identity, Personnel, etc.)
/// </summary>
public sealed class OrganizationDbContext : ModuleDbContext
{
    public OrganizationDbContext(
        DbContextOptions<OrganizationDbContext> options,
        IPublisher publisher)
        : base(options, publisher)
    {
    }

    /// <summary>
    /// Module name for distributed locking.
    /// CRITICAL: Must be unique across all modules.
    /// </summary>
    public override string ModuleName => "Organization";

    /// <summary>
    /// Companies table - top-level organizational units.
    /// </summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary>
    /// Departments table - organizational units within companies.
    /// </summary>
    public DbSet<Department> Departments => Set<Department>();

    /// <summary>
    /// Positions table - job positions within departments.
    /// </summary>
    public DbSet<Position> Positions => Set<Position>();

    /// <summary>
    /// Configure entity mappings.
    /// Applies all IEntityTypeConfiguration from current assembly.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply base configuration (soft delete filters, outbox, etc.)
        base.OnModelCreating(modelBuilder);

        // Set default schema for Organization module
        modelBuilder.HasDefaultSchema("Organization");

        // Apply entity configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure Company entity
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.TaxId)
                .HasMaxLength(50);

            entity.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("IX_Companies_Code");
        });

        // Configure Department entity
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(e => new { e.CompanyId, e.Code })
                .IsUnique()
                .HasDatabaseName("IX_Departments_CompanyId_Code");

            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("IX_Departments_CompanyId");
        });

        // Configure Position entity
        modelBuilder.Entity<Position>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(e => new { e.DepartmentId, e.Code })
                .IsUnique()
                .HasDatabaseName("IX_Positions_DepartmentId_Code");

            entity.HasIndex(e => e.DepartmentId)
                .HasDatabaseName("IX_Positions_DepartmentId");
        });
    }
}
