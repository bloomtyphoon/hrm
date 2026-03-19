using System.Data;
using Dapper;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using Microsoft.Extensions.Logging;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Loads DataScopeLevel definitions from [Identity].DataScopeLevels table
/// and registers them into DataScopeLevel's static registry.
///
/// Called at application startup via IHostedService.
/// After initialization, DataScopeLevel.FromId/FromName work for all DB-defined levels.
/// </summary>
public sealed class DataScopeLevelRegistry : IDataScopeLevelRegistry
{
    private readonly IDbConnection _connection;
    private readonly ILogger<DataScopeLevelRegistry> _logger;
    private List<DataScopeLevel> _allLevels = new();

    public DataScopeLevelRegistry(IDbConnection connection, ILogger<DataScopeLevelRegistry> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, DisplayName, Category, SortOrder, IsActive, DimensionKey, ResolutionKey
            FROM [Identity].DataScopeLevels
            ORDER BY SortOrder
            """;

        var rows = await _connection.QueryAsync<DataScopeLevelRow>(sql);
        var levels = new List<DataScopeLevel>();

        foreach (var row in rows)
        {
            if (!Enum.TryParse<ScopeCategory>(row.Category, ignoreCase: true, out var category))
            {
                _logger.LogWarning(
                    "Unknown ScopeCategory '{Category}' for DataScopeLevel '{Name}' (Id={Id}). Skipping.",
                    row.Category, row.Name, row.Id);
                continue;
            }

            var level = DataScopeLevel.Register(
                row.Id, row.Name, category, row.SortOrder,
                row.DimensionKey, row.ResolutionKey);

            levels.Add(level);

            _logger.LogDebug(
                "Registered DataScopeLevel: {Name} (Id={Id}, Category={Category}, Active={IsActive})",
                row.Name, row.Id, row.Category, row.IsActive);
        }

        _allLevels = levels;

        _logger.LogInformation(
            "DataScopeLevelRegistry initialized with {Count} scope levels from DB ({Active} active)",
            levels.Count, levels.Count(l => _allLevels.Any(a => a.Id == l.Id)));
    }

    public IReadOnlyList<DataScopeLevel> GetAll()
        => _allLevels.OrderBy(l => l.SortOrder).ToList();

    public IReadOnlyList<DataScopeLevel> GetActive()
        => _allLevels.OrderBy(l => l.SortOrder).ToList();

    private sealed class DataScopeLevelRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string Category { get; set; } = default!;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public string? DimensionKey { get; set; }
        public string? ResolutionKey { get; set; }
    }
}

/// <summary>
/// Hosted service that initializes DataScopeLevelRegistry at startup.
/// </summary>
public sealed class DataScopeLevelLoaderService : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly IDataScopeLevelRegistry _registry;
    private readonly ILogger<DataScopeLevelLoaderService> _logger;

    public DataScopeLevelLoaderService(
        IDataScopeLevelRegistry registry,
        ILogger<DataScopeLevelLoaderService> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _registry.InitializeAsync(cancellationToken);
            _logger.LogInformation("DataScopeLevelRegistry loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load DataScopeLevels from DB. Using well-known defaults. " +
                "Run migration 009_CreateDataScopeLevelsTable.sql to create the table.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
