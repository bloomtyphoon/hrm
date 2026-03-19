using System.Xml.Linq;
using HRM.BuildingBlocks.Application.Abstractions.Caching;
using HRM.BuildingBlocks.Domain.Abstractions.Permissions;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Services;
using HRM.Modules.Identity.Domain.ValueObjects;
using HRM.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace HRM.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Implementation of IPermissionCatalogService.
///
/// Two-tier catalog architecture:
/// - Base catalog (XML): Loaded from embedded resources, cached globally
/// - Tenant catalog: Base catalog merged with tenant scope overrides from DB, cached per tenant
///
/// Design:
/// - Each module provides its own IPermissionCatalogSource (XML)
/// - ITenantScopeOverrideProvider loads tenant overrides from DB
/// - Tenant overrides can only restrict scopes (subset of base catalog)
/// - Cache keys: "identity:catalog:permissions" (base), "identity:catalog:permissions:{tenantId}" (tenant)
/// </summary>
public sealed class PermissionCatalogService : IPermissionCatalogService
{
    private const string BaseCatalogCacheKey = "identity:catalog:permissions";
    private const string TenantCatalogCacheKeyPrefix = "identity:catalog:permissions:";
    private readonly IEnumerable<IPermissionCatalogSource> _sources;
    private readonly ITenantScopeOverrideProvider _overrideProvider;
    private readonly ICache _cache;
    private readonly TimeSpan _cacheDuration;

    public PermissionCatalogService(
        IEnumerable<IPermissionCatalogSource> sources,
        ITenantScopeOverrideProvider overrideProvider,
        ICache cache,
        IOptions<IdentityCacheSettings> cacheSettings)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _overrideProvider = overrideProvider ?? throw new ArgumentNullException(nameof(overrideProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheDuration = TimeSpan.FromMinutes(cacheSettings.Value.CatalogCacheDurationMinutes);
    }

    /// <inheritdoc />
    public async Task<List<PermissionModule>> LoadBaseCatalogAsync()
    {
        return await _cache.GetOrCreateAsync(
            BaseCatalogCacheKey,
            async () =>
            {
                var allModules = new List<PermissionModule>();

                foreach (var source in _sources)
                {
                    try
                    {
                        var xmlContent = await source.LoadContentAsync();
                        var modules = ParseCatalog(xmlContent);
                        allModules.AddRange(modules);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to load permission catalog from source '{source.ModuleName}': {ex.Message}",
                            ex);
                    }
                }

                var duplicateModules = allModules
                    .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateModules.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"Duplicate module names found in permission catalogs: {string.Join(", ", duplicateModules)}");
                }

                return allModules;
            },
            _cacheDuration);
    }

    /// <inheritdoc />
    public async Task<List<PermissionModule>> LoadCatalogAsync(Guid tenantId)
    {
        var cacheKey = $"{TenantCatalogCacheKeyPrefix}{tenantId}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var baseModules = await LoadBaseCatalogAsync();
                var overrides = await _overrideProvider.GetOverridesAsync(tenantId);

                if (overrides.Count == 0)
                    return baseModules;

                return MergeCatalogWithOverrides(baseModules, overrides);
            },
            _cacheDuration);
    }

    /// <inheritdoc />
    public Task<List<PermissionModule>> LoadCatalogAsync()
    {
        return LoadBaseCatalogAsync();
    }

    /// <inheritdoc />
    public async Task<PermissionModule?> GetModuleAsync(string moduleName)
    {
        var modules = await LoadBaseCatalogAsync();
        return modules.FirstOrDefault(m => m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public async Task<PermissionEntity?> GetEntityAsync(string moduleName, string entityName)
    {
        var module = await GetModuleAsync(moduleName);
        return module?.GetEntity(entityName);
    }

    /// <inheritdoc />
    public async Task<PermissionAction?> GetActionAsync(string moduleName, string entityName, string actionName)
    {
        var entity = await GetEntityAsync(moduleName, entityName);
        return entity?.GetAction(actionName);
    }

    /// <inheritdoc />
    public async Task<PermissionAction?> GetActionAsync(Guid tenantId, string moduleName, string entityName, string actionName)
    {
        var modules = await LoadCatalogAsync(tenantId);
        var module = modules.FirstOrDefault(m => m.Name.Equals(moduleName, StringComparison.OrdinalIgnoreCase));
        var entity = module?.GetEntity(entityName);
        return entity?.GetAction(actionName);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string moduleName, string entityName, string actionName)
    {
        var action = await GetActionAsync(moduleName, entityName, actionName);
        return action != null;
    }

    #region Private Helper Methods

    /// <summary>
    /// Merge base catalog with tenant scope overrides.
    /// Creates deep copies of modules/entities/actions that have overrides applied.
    /// </summary>
    private static List<PermissionModule> MergeCatalogWithOverrides(
        List<PermissionModule> baseModules,
        List<TenantScopeOverride> overrides)
    {
        // Index overrides by "Module.Entity.Action" key for fast lookup
        var overrideMap = overrides.ToDictionary(
            o => $"{o.Module}.{o.Entity}.{o.Action}",
            StringComparer.OrdinalIgnoreCase);

        var result = new List<PermissionModule>();

        foreach (var baseModule in baseModules)
        {
            var entities = new List<PermissionEntity>();

            foreach (var baseEntity in baseModule.Entities)
            {
                var actions = new List<PermissionAction>();

                foreach (var baseAction in baseEntity.Actions)
                {
                    var key = $"{baseModule.Name}.{baseEntity.Name}.{baseAction.Name}";

                    if (overrideMap.TryGetValue(key, out var scopeOverride))
                    {
                        // Apply override: replace scopes with tenant-specific ones
                        var overriddenScopes = scopeOverride.AllowedScopes
                            .Select(level =>
                            {
                                // Try to preserve displayName from base catalog
                                var baseScope = baseAction.GetScope(level);
                                return baseScope ?? new PermissionScope(level, level.ToString());
                            })
                            .ToList();

                        var defaultScope = scopeOverride.DefaultScope?.ToString() ?? baseAction.DefaultScope;

                        actions.Add(new PermissionAction(
                            baseAction.Name,
                            baseAction.DisplayName,
                            overriddenScopes,
                            baseAction.Constraints,
                            defaultScope));
                    }
                    else
                    {
                        // No override - use base catalog as-is
                        actions.Add(baseAction);
                    }
                }

                entities.Add(new PermissionEntity(baseEntity.Name, baseEntity.DisplayName, actions));
            }

            result.Add(new PermissionModule(baseModule.Name, baseModule.DisplayName, entities));
        }

        return result;
    }

    /// <summary>
    /// Parse catalog XML content to permission modules.
    /// </summary>
    private List<PermissionModule> ParseCatalog(string xmlContent)
    {
        if (string.IsNullOrWhiteSpace(xmlContent))
        {
            throw new ArgumentException("XML content cannot be null or empty", nameof(xmlContent));
        }

        try
        {
            var doc = XDocument.Parse(xmlContent);
            var root = doc.Root ?? throw new InvalidOperationException("XML document has no root element");

            XNamespace ns = root.GetDefaultNamespace();

            var permissionsElement = root.Element(ns + "Permissions")
                ?? throw new InvalidOperationException("Permissions element is required");

            return ParseModules(permissionsElement, ns);
        }
        catch (Exception ex) when (ex is not InvalidOperationException && ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to parse permission catalog: {ex.Message}", ex);
        }
    }

    private List<PermissionModule> ParseModules(XElement permissionsElement, XNamespace ns)
    {
        var modules = new List<PermissionModule>();

        foreach (var moduleElement in permissionsElement.Elements(ns + "Module"))
        {
            var moduleName = moduleElement.Attribute("name")?.Value
                ?? throw new InvalidOperationException("Module name attribute is required");

            var moduleDisplayName = moduleElement.Attribute("displayName")?.Value
                ?? throw new InvalidOperationException("Module displayName attribute is required");

            var entities = ParseEntities(moduleElement, ns);

            modules.Add(new PermissionModule(moduleName, moduleDisplayName, entities));
        }

        return modules;
    }

    private List<PermissionEntity> ParseEntities(XElement moduleElement, XNamespace ns)
    {
        var entities = new List<PermissionEntity>();

        foreach (var entityElement in moduleElement.Elements(ns + "Entity"))
        {
            var entityName = entityElement.Attribute("name")?.Value
                ?? throw new InvalidOperationException("Entity name attribute is required");

            var entityDisplayName = entityElement.Attribute("displayName")?.Value
                ?? throw new InvalidOperationException("Entity displayName attribute is required");

            var actions = ParseActions(entityElement, ns);

            entities.Add(new PermissionEntity(entityName, entityDisplayName, actions));
        }

        return entities;
    }

    private List<PermissionAction> ParseActions(XElement entityElement, XNamespace ns)
    {
        var actions = new List<PermissionAction>();

        foreach (var actionElement in entityElement.Elements(ns + "Action"))
        {
            var actionName = actionElement.Attribute("name")?.Value
                ?? throw new InvalidOperationException("Action name attribute is required");

            var actionDisplayName = actionElement.Attribute("displayName")?.Value
                ?? throw new InvalidOperationException("Action displayName attribute is required");

            var defaultScope = actionElement.Attribute("defaultScope")?.Value;

            var scopes = new List<PermissionScope>();
            var scopesElement = actionElement.Element(ns + "Scopes");
            if (scopesElement != null)
            {
                scopes = ParseScopes(scopesElement, ns);
            }

            var constraints = new List<PermissionConstraint>();
            var constraintsElement = actionElement.Element(ns + "Constraints");
            if (constraintsElement != null)
            {
                constraints = ParseConstraints(constraintsElement, ns);
            }

            actions.Add(new PermissionAction(
                actionName,
                actionDisplayName,
                scopes,
                constraints,
                defaultScope
            ));
        }

        return actions;
    }

    private List<PermissionScope> ParseScopes(XElement scopesElement, XNamespace ns)
    {
        var scopes = new List<PermissionScope>();

        foreach (var scopeElement in scopesElement.Elements(ns + "Scope"))
        {
            var scopeValue = scopeElement.Attribute("value")?.Value
                ?? throw new InvalidOperationException("Scope value attribute is required");

            var scopeDisplayName = scopeElement.Attribute("displayName")?.Value
                ?? throw new InvalidOperationException("Scope displayName attribute is required");

            var readOnlyStr = scopeElement.Attribute("readOnly")?.Value;
            var readOnly = bool.TryParse(readOnlyStr, out var readOnlyValue) && readOnlyValue;

            var scopeLevel = DataScopeLevel.TryFromName(scopeValue)
                ?? throw new InvalidOperationException($"Invalid scope value: {scopeValue}. Ensure the scope level is registered in DataScopeLevels table.");

            scopes.Add(new PermissionScope(scopeLevel, scopeDisplayName, readOnly));
        }

        return scopes;
    }

    private List<PermissionConstraint> ParseConstraints(XElement constraintsElement, XNamespace ns)
    {
        var constraints = new List<PermissionConstraint>();

        foreach (var constraintElement in constraintsElement.Elements(ns + "Constraint"))
        {
            var constraintTypeStr = constraintElement.Attribute("type")?.Value
                ?? throw new InvalidOperationException("Constraint type attribute is required");

            if (!Enum.TryParse<HRM.Modules.Identity.Domain.Enums.ConstraintType>(constraintTypeStr, true, out var constraintType))
            {
                throw new InvalidOperationException($"Invalid constraint type: {constraintTypeStr}");
            }

            var parameters = new Dictionary<string, string>();
            var parametersElement = constraintElement.Element(ns + "Parameters");
            if (parametersElement != null)
            {
                foreach (var paramElement in parametersElement.Elements(ns + "Parameter"))
                {
                    var paramName = paramElement.Attribute("name")?.Value
                        ?? throw new InvalidOperationException("Parameter name attribute is required");

                    var paramValue = paramElement.Attribute("value")?.Value
                        ?? throw new InvalidOperationException("Parameter value attribute is required");

                    parameters[paramName] = paramValue;
                }
            }

            constraints.Add(new PermissionConstraint(constraintType, parameters));
        }

        return constraints;
    }

    #endregion
}
