using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// Value object representing a permission scope option.
/// Scopes define the data visibility boundary for a permission action.
/// Immutable by design.
/// </summary>
public sealed class PermissionScope
{
    /// <summary>
    /// Scope level — references DB-driven DataScopeLevel.
    /// </summary>
    public DataScopeLevel Value { get; private set; } = default!;

    /// <summary>
    /// Display name for UI (e.g., "Toàn công ty", "Cùng phòng ban").
    /// Supports localization.
    /// </summary>
    public string DisplayName { get; private set; } = default!;

    /// <summary>
    /// Whether this scope is read-only (cannot be used for Update/Delete).
    /// </summary>
    public bool IsReadOnly { get; private set; }

    /// <summary>Private constructor for EF Core.</summary>
    private PermissionScope()
    {
    }

    /// <summary>Create a new permission scope.</summary>
    public PermissionScope(DataScopeLevel value, string displayName, bool isReadOnly = false)
    {
        Value = value;
        DisplayName = displayName;
        IsReadOnly = isReadOnly;
    }

    public static PermissionScope Global(string displayName = "Toàn hệ thống", bool isReadOnly = false)
        => new(DataScopeLevel.Global, displayName, isReadOnly);

    public static PermissionScope Company(string displayName = "Toàn công ty", bool isReadOnly = false)
        => new(DataScopeLevel.Company, displayName, isReadOnly);

    public static PermissionScope Department(string displayName = "Cùng phòng ban", bool isReadOnly = false)
        => new(DataScopeLevel.Department, displayName, isReadOnly);

    public static PermissionScope Position(string displayName = "Cùng chức danh", bool isReadOnly = false)
        => new(DataScopeLevel.Position, displayName, isReadOnly);

    public static PermissionScope Self(string displayName = "Chỉ bản thân", bool isReadOnly = false)
        => new(DataScopeLevel.Self, displayName, isReadOnly);
}
