using System.Data;
using Dapper;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of IAccountPermissionRepository using Dapper.
/// Uses direct SQL for optimized read-only permission queries.
/// </summary>
public sealed class AccountPermissionRepository : IAccountPermissionRepository
{
    private readonly string _connectionString;
    private const string SystemAdminRoleName = "System Administrator";

    public AccountPermissionRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HrmDatabase")
            ?? throw new InvalidOperationException("Connection string 'HrmDatabase' not found");
    }

    public async Task<HashSet<string>> GetPermissionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DISTINCT
                rp.Module + '.' + rp.Entity + '.' + rp.Action AS PermissionKey
            FROM [Identity].Accounts a
            INNER JOIN [Identity].AccountRoles ar ON a.Id = ar.AccountId
            INNER JOIN [Identity].Roles r ON ar.RoleId = r.Id
            INNER JOIN [Identity].RolePermissions rp ON r.Id = rp.RoleId
            WHERE a.Id = @AccountId
              AND a.Status = 1
              AND r.IsDeleted = 0
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var permissions = await connection.QueryAsync<string>(
            new CommandDefinition(
                sql,
                new { AccountId = accountId },
                cancellationToken: cancellationToken));

        return permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsSuperAdminAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM [Identity].Accounts a
                INNER JOIN [Identity].AccountRoles ar ON a.Id = ar.AccountId
                INNER JOIN [Identity].Roles r ON ar.RoleId = r.Id
                WHERE a.Id = @AccountId
                  AND a.Status = 1
                  AND r.IsDeleted = 0
                  AND r.Name = @SystemAdminRoleName
            ) THEN 1 ELSE 0 END
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { AccountId = accountId, SystemAdminRoleName },
                cancellationToken: cancellationToken));

        return result == 1;
    }

    public async Task<bool> HasPermissionAsync(
        Guid accountId,
        string module,
        string entity,
        string action,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM [Identity].Accounts a
                INNER JOIN [Identity].AccountRoles ar ON a.Id = ar.AccountId
                INNER JOIN [Identity].Roles r ON ar.RoleId = r.Id
                INNER JOIN [Identity].RolePermissions rp ON r.Id = rp.RoleId
                WHERE a.Id = @AccountId
                  AND a.Status = 1
                  AND r.IsDeleted = 0
                  AND rp.Module = @Module
                  AND rp.Entity = @Entity
                  AND rp.Action = @Action
            ) THEN 1 ELSE 0 END
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var result = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { AccountId = accountId, Module = module, Entity = entity, Action = action },
                cancellationToken: cancellationToken));

        return result == 1;
    }

    public async Task<Dictionary<string, int>> GetPermissionsWithScopesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                rp.Module + '.' + rp.Entity + '.' + rp.Action AS PermissionKey,
                MAX(ISNULL(rp.Scope, 4)) AS ScopeLevel
            FROM [Identity].Accounts a
            INNER JOIN [Identity].AccountRoles ar ON a.Id = ar.AccountId
            INNER JOIN [Identity].Roles r ON ar.RoleId = r.Id
            INNER JOIN [Identity].RolePermissions rp ON r.Id = rp.RoleId
            WHERE a.Id = @AccountId
              AND a.Status = 1
              AND r.IsDeleted = 0
            GROUP BY rp.Module, rp.Entity, rp.Action
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var results = await connection.QueryAsync<PermissionScopeDto>(
            new CommandDefinition(
                sql,
                new { AccountId = accountId },
                cancellationToken: cancellationToken));

        return results.ToDictionary(
            r => r.PermissionKey,
            r => r.ScopeLevel,
            StringComparer.OrdinalIgnoreCase);
    }

    private sealed record PermissionScopeDto(string PermissionKey, int ScopeLevel);
}
