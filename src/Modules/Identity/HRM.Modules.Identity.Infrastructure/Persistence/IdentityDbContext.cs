using System.Reflection;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// DbContext for Identity module.
/// Inherits from ModuleDbContext for:
/// - Unit of Work pattern
/// - Domain event dispatching
/// - Soft delete query filters
/// - Audit trail (CreatedAtUtc, ModifiedAtUtc, CreatedById, ModifiedById)
/// - Outbox pattern (OutboxMessages table)
///
/// Tables:
/// - Identity.Accounts: Unified authentication accounts (System + Employee)
/// - Identity.RefreshTokens: Session management tokens
/// - Identity.OutboxMessages: Integration events for reliable publishing
///
/// Schema: "Identity" (shared database with other modules)
/// </summary>
public sealed class IdentityDbContext : ModuleDbContext, IIdentityQueryContext
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options,
        IPublisher publisher,
        ITenantContext? tenantContext = null)
        : base(options, publisher, tenantContext)
    {
    }

    /// <summary>
    /// Module name for distributed locking.
    /// </summary>
    public override string ModuleName => "Identity";

    /// <summary>
    /// Accounts table — unified authentication entity (System + Employee).
    /// </summary>
    public DbSet<Account> Accounts => Set<Account>();

    /// <summary>
    /// Refresh tokens table for session management.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Roles table for role-based access control.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Account-Role assignments table.
    /// </summary>
    public DbSet<AccountRole> AccountRoles => Set<AccountRole>();

    /// <summary>
    /// System profiles table for system/admin accounts.
    /// </summary>
    public DbSet<SystemProfile> SystemProfiles => Set<SystemProfile>();

    /// <summary>
    /// Employee profiles table linking accounts to employees.
    /// </summary>
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();

    /// <summary>
    /// Tenant scope overrides for per-tenant permission scope configuration.
    /// </summary>
    public DbSet<TenantScopeOverride> TenantScopeOverrides => Set<TenantScopeOverride>();

    /// <summary>
    /// Configure entity mappings.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("Identity");

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
