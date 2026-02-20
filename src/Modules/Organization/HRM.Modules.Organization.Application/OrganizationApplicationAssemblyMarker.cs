namespace HRM.Modules.Organization.Application;

/// <summary>
/// Assembly marker for Organization.Application module.
/// Used to locate embedded resources and register services.
///
/// Usage:
/// <code>
/// // Get assembly for embedded resources
/// var assembly = typeof(OrganizationApplicationAssemblyMarker).Assembly;
///
/// // Register permission catalog source
/// factory.FromEmbeddedResource(
///     typeof(OrganizationApplicationAssemblyMarker).Assembly,
///     "HRM.Modules.Organization.Application.Resources.PermissionCatalog.xml");
/// </code>
/// </summary>
public sealed class OrganizationApplicationAssemblyMarker;
