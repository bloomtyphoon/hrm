using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Abstractions.Authentication;

/// <summary>
/// Identity module's typed user context service.
/// Extends IExecutionContext with Identity-specific vocabulary.
/// Also implements ITenantContext — registered in DI so other services
/// can resolve ITenantContext from this service.
/// </summary>
public interface ICurrentUserService : IExecutionContext, ITenantContext
{
    /// <summary>
    /// Gets the current user's account type (System or Employee).
    /// </summary>
    AccountType AccountType { get; }

    /// <summary>
    /// Gets the current user's employee ID (only for Employee accounts).
    /// Null for System accounts.
    /// </summary>
    Guid? EmployeeId { get; }

    /// <summary>
    /// Gets the current user's primary company ID (only for Employee accounts).
    /// Null for System accounts or employees without a primary company assignment.
    /// </summary>
    Guid? CompanyId { get; }

    /// <summary>
    /// Checks if the current user is a System account.
    /// </summary>
    bool IsSystemAccount();

    /// <summary>
    /// Checks if the current user is an Employee account.
    /// </summary>
    bool IsEmployeeAccount();
}
