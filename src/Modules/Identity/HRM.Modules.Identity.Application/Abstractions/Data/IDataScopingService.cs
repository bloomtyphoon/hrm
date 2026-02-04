using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Abstractions.Data;

/// <summary>
/// [DEPRECATED] Service for applying data scoping filters based on user's scope level.
/// Use IDataScopeRuleProvider + SqlScopeWhereBuilder instead.
/// </summary>
[Obsolete("Use IDataScopeRuleProvider + SqlScopeWhereBuilder instead")]
public interface IDataScopingService
{
    Task<DataScopeContext> GetCurrentScopeAsync(CancellationToken cancellationToken = default);
    string BuildScopeFilter(DataScopeContext scopeContext, dynamic parameters);
    Task<bool> CanAccessEmployeeAsync(
        DataScopeContext scopeContext,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// [DEPRECATED] Contains data scope information for the current user.
/// </summary>
[Obsolete("Use DataScopeContext from Identity.Application.Abstractions.Authorization instead")]
public sealed class DataScopeContext
{
    public required AccountType AccountType { get; init; }
    public required Guid UserId { get; init; }
    public ScopeLevel? ScopeLevel { get; init; }
    public List<Guid> AllowedCompanyIds { get; init; } = new();
    public List<Guid> AllowedDepartmentIds { get; init; } = new();
    public List<Guid> AllowedPositionIds { get; init; } = new();

    public bool IsSystemAccount => AccountType == AccountType.System;
    public bool IsEmployeeAccount => AccountType == AccountType.Employee;
    public bool RequiresScoping => AccountType == AccountType.Employee;
}
