namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Strongly-typed permission descriptor replacing magic strings.
///
/// Format: {Module}.{Entity}.{Action}
/// Example: Personnel.Employee.View, Organization.Company.Create
///
/// Usage:
/// <code>
/// // Define (in module Application layer)
/// public static readonly PermissionDescriptor View = new("Personnel", "Employee", "View");
///
/// // Use in handler
/// var rule = await dataScopeService.GetScopeRuleAsync(userId, PersonnelPermissions.Employee.View, ct);
///
/// // Name property matches XML format
/// descriptor.Name == "Personnel.Employee.View"  // true
/// </code>
///
/// Design:
/// - record: immutable, value equality, safe for dictionary key and cache
/// - Name auto-constructed from Module.Entity.Action
/// - Matches PermissionCatalog.xml and RouteSecurityMap.xml string format
/// </summary>
public sealed record PermissionDescriptor
{
    /// <summary>Module name (e.g., "Personnel", "Organization", "Identity")</summary>
    public string Module { get; }

    /// <summary>Entity name (e.g., "Employee", "Company", "Department")</summary>
    public string Entity { get; }

    /// <summary>Action name (e.g., "View", "Create", "Update")</summary>
    public string Action { get; }

    /// <summary>Full permission key: "{Module}.{Entity}.{Action}"</summary>
    public string Name { get; }

    public PermissionDescriptor(string module, string entity, string action)
    {
        Module = module;
        Entity = entity;
        Action = action;
        Name = $"{module}.{entity}.{action}";
    }

    public override string ToString() => Name;
}
