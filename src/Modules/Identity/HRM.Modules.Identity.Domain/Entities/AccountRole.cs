using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Join entity representing an Account-Role assignment.
/// Stored in Identity.AccountRoles table.
/// </summary>
public class AccountRole : Entity
{
    public Guid AccountId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
    public Guid? AssignedById { get; private set; }

    private AccountRole() { }

    public static AccountRole Create(Guid accountId, Guid roleId, Guid? assignedById = null)
    {
        return new AccountRole
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            RoleId = roleId,
            AssignedAtUtc = DateTime.UtcNow,
            AssignedById = assignedById
        };
    }
}
