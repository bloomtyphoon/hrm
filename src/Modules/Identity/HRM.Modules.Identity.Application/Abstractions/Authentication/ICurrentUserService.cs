using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Abstractions.Authentication;

/// <summary>
/// Identity module's typed user context service.
/// Extends IExecutionContext with Identity-specific vocabulary.
/// </summary>
public interface ICurrentUserService : IExecutionContext
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
    /// Checks if the current user is a System account.
    /// </summary>
    bool IsSystemAccount();

    /// <summary>
    /// Checks if the current user is an Employee account.
    /// </summary>
    bool IsEmployeeAccount();
}
