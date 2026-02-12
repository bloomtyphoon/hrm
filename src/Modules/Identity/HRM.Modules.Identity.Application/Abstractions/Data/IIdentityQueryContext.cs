using HRM.Modules.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Abstractions.Data;

/// <summary>
/// Query context interface for Identity module.
/// Provides read-only access to DbSets for query handlers.
///
/// Purpose:
/// - Dependency Inversion: Application depends on abstraction, not Infrastructure
/// - Query handlers in Application layer can access data without referencing EF Core implementation
///
/// Usage:
/// <code>
/// public class GetAccountsQueryHandler : IQueryHandler&lt;...&gt;
/// {
///     private readonly IIdentityQueryContext _context;
///
///     public async Task&lt;PagedResult&lt;AccountSummaryDto&gt;&gt; Handle(...)
///     {
///         var query = _context.Accounts.AsNoTracking();
///         // ... query logic
///     }
/// }
/// </code>
/// </summary>
public interface IIdentityQueryContext
{
    /// <summary>
    /// Accounts table (read-only access).
    /// </summary>
    DbSet<Account> Accounts { get; }

    /// <summary>
    /// Refresh tokens table (read-only access).
    /// </summary>
    DbSet<RefreshToken> RefreshTokens { get; }

    /// <summary>
    /// Roles table (read-only access).
    /// </summary>
    DbSet<Role> Roles { get; }

    /// <summary>
    /// Account-Role assignments table (read-only access).
    /// </summary>
    DbSet<AccountRole> AccountRoles { get; }

    /// <summary>
    /// System profiles table (read-only access).
    /// </summary>
    DbSet<SystemProfile> SystemProfiles { get; }

    /// <summary>
    /// Employee profiles table (read-only access).
    /// </summary>
    DbSet<EmployeeProfile> EmployeeProfiles { get; }
}
