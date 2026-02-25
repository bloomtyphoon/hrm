namespace HRM.Modules.Personnel.Application;

/// <summary>
/// Assembly marker for Personnel.Application module.
/// Used to locate embedded resources and register services.
///
/// Usage:
/// <code>
/// // Get assembly for embedded resources
/// var assembly = typeof(PersonnelApplicationAssemblyMarker).Assembly;
///
/// // Register permission catalog source
/// factory.FromEmbeddedResource(
///     typeof(PersonnelApplicationAssemblyMarker).Assembly,
///     "HRM.Modules.Personnel.Application.Resources.PermissionCatalog.xml");
/// </code>
/// </summary>
public sealed class PersonnelApplicationAssemblyMarker;
