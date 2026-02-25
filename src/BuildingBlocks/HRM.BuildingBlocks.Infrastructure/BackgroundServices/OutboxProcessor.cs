using System.Data;
using System.Text.Json;
using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.BuildingBlocks.Domain.Abstractions.Events;
using HRM.BuildingBlocks.Infrastructure.Outbox;
using HRM.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HRM.BuildingBlocks.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes unprocessed outbox messages.
/// Reads integration events from the database and resolves
/// IIntegrationEventHandler&lt;T&gt; directly from the DI container (no MediatR).
///
/// Flow:
/// 1. Poll database for unprocessed OutboxMessages
/// 2. Deserialize the integration event from JSON
/// 3. Resolve IIntegrationEventHandler&lt;T&gt; from DI
/// 4. Call Handle on each handler
/// 5. Mark message as processed or failed
/// </summary>
public abstract class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval;
    private readonly int _batchSize;
    private readonly int _maxAttempts;

    protected OutboxProcessor(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<OutboxProcessor> logger,
        OutboxSettings settings)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ArgumentNullException.ThrowIfNull(settings);
        _pollingInterval = TimeSpan.FromSeconds(settings.PollingIntervalSeconds);
        _batchSize = settings.BatchSize;
        _maxAttempts = settings.MaxAttempts;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "{ProcessorName} started. Polling interval: {PollingInterval}s, Batch size: {BatchSize}, Max attempts: {MaxAttempts}",
            GetType().Name,
            _pollingInterval.TotalSeconds,
            _batchSize,
            _maxAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "{ProcessorName} encountered an error during processing. Will retry after delay.",
                    GetType().Name);
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("{ProcessorName} stopped", GetType().Name);
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();

        var dbContext = GetDbContext(scope.ServiceProvider);

        if (string.IsNullOrWhiteSpace(dbContext.ModuleName))
        {
            throw new InvalidOperationException(
                $"ModuleName must be set in derived DbContext. " +
                $"Override the ModuleName property in {dbContext.GetType().Name}.");
        }

        // Acquire distributed lock
        var lockResource = $"OutboxProcessor_{dbContext.ModuleName}";
        var lockAcquired = await TryAcquireLockAsync(dbContext, lockResource, cancellationToken);

        if (!lockAcquired)
        {
            _logger.LogDebug(
                "{ProcessorName}: Could not acquire lock '{LockResource}'. Skipping this iteration.",
                GetType().Name,
                lockResource);
            return;
        }

        try
        {
            // Query unprocessed messages from database
            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && m.AttemptCount < _maxAttempts)
                .OrderBy(m => m.OccurredOnUtc)
                .Take(_batchSize)
                .ToListAsync(cancellationToken);

            if (!messages.Any())
            {
                _logger.LogDebug("{ProcessorName}: No pending outbox messages", GetType().Name);
                return;
            }

            _logger.LogInformation(
                "{ProcessorName}: Found {MessageCount} pending outbox messages to process",
                GetType().Name,
                messages.Count);

            var successCount = 0;
            var failureCount = 0;

            foreach (var message in messages)
            {
                var processed = await ProcessSingleMessageAsync(message, scope.ServiceProvider, cancellationToken);

                if (processed)
                    successCount++;
                else
                    failureCount++;
            }

            // Save all changes (marks messages as processed/failed)
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "{ProcessorName}: Processed {TotalCount} messages - {SuccessCount} succeeded, {FailureCount} failed",
                GetType().Name,
                messages.Count,
                successCount,
                failureCount);
        }
        finally
        {
            await ReleaseLockAsync(dbContext, lockResource, cancellationToken);
        }
    }

    /// <summary>
    /// Process a single outbox message by deserializing the integration event
    /// and resolving IIntegrationEventHandler&lt;T&gt; directly from the DI container.
    /// </summary>
    private async Task<bool> ProcessSingleMessageAsync(
        OutboxMessage message,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Processing outbox message {MessageId} (Type: {EventType}, Attempt: {AttemptCount})",
                message.Id,
                message.Type,
                message.AttemptCount + 1);

            // Deserialize integration event
            var eventType = Type.GetType(message.Type);
            if (eventType is null)
            {
                var error = $"Unknown event type: {message.Type}";
                _logger.LogError(error);
                message.MarkAsFailed(error);
                return false;
            }

            var integrationEvent = JsonSerializer.Deserialize(message.Content, eventType);
            if (integrationEvent is null)
            {
                var error = $"Failed to deserialize event: {message.Type}";
                _logger.LogError(error);
                message.MarkAsFailed(error);
                return false;
            }

            // Resolve IIntegrationEventHandler<T> from DI (no MediatR)
            var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerInterfaceType);
            var handlers = serviceProvider.GetService(enumerableType) as IEnumerable<object>;

            if (handlers is null || !handlers.Any())
            {
                _logger.LogDebug(
                    "No handlers registered for {EventType}, marking as processed",
                    eventType.Name);
                message.MarkAsProcessed();
                return true;
            }

            // Call Handle on each handler
            var handleMethod = handlerInterfaceType.GetMethod("Handle")!;
            foreach (var handler in handlers)
            {
                await (Task)handleMethod.Invoke(handler, [integrationEvent, cancellationToken])!;
            }

            message.MarkAsProcessed();

            _logger.LogInformation(
                "Successfully processed outbox message {MessageId} (Type: {EventType})",
                message.Id,
                eventType.Name);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process outbox message {MessageId} (Type: {EventType}, Attempt: {AttemptCount})",
                message.Id,
                message.Type,
                message.AttemptCount + 1);

            message.MarkAsFailed(ex.Message);

            if (!message.CanRetry(_maxAttempts))
            {
                _logger.LogWarning(
                    "Outbox message {MessageId} has reached max attempts ({MaxAttempts})",
                    message.Id,
                    _maxAttempts);
            }

            return false;
        }
    }

    private async Task<bool> TryAcquireLockAsync(
        ModuleDbContext dbContext,
        string lockResource,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = dbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            using var command = connection.CreateCommand();
            command.CommandText = "sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;

            var resourceParam = command.CreateParameter();
            resourceParam.ParameterName = "@Resource";
            resourceParam.Value = lockResource;
            command.Parameters.Add(resourceParam);

            var lockModeParam = command.CreateParameter();
            lockModeParam.ParameterName = "@LockMode";
            lockModeParam.Value = "Exclusive";
            command.Parameters.Add(lockModeParam);

            var lockOwnerParam = command.CreateParameter();
            lockOwnerParam.ParameterName = "@LockOwner";
            lockOwnerParam.Value = "Session";
            command.Parameters.Add(lockOwnerParam);

            var lockTimeoutParam = command.CreateParameter();
            lockTimeoutParam.ParameterName = "@LockTimeout";
            lockTimeoutParam.Value = 0;
            command.Parameters.Add(lockTimeoutParam);

            var returnParam = command.CreateParameter();
            returnParam.ParameterName = "@ReturnValue";
            returnParam.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(returnParam);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var returnValue = (int)returnParam.Value!;
            return returnValue >= 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring lock '{LockResource}'", lockResource);
            return false;
        }
    }

    private async Task ReleaseLockAsync(
        ModuleDbContext dbContext,
        string lockResource,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = dbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
                return;

            using var command = connection.CreateCommand();
            command.CommandText = "sp_releaseapplock";
            command.CommandType = CommandType.StoredProcedure;

            var resourceParam = command.CreateParameter();
            resourceParam.ParameterName = "@Resource";
            resourceParam.Value = lockResource;
            command.Parameters.Add(resourceParam);

            var lockOwnerParam = command.CreateParameter();
            lockOwnerParam.ParameterName = "@LockOwner";
            lockOwnerParam.Value = "Session";
            command.Parameters.Add(lockOwnerParam);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error releasing lock '{LockResource}'. Lock will auto-release when connection closes.",
                lockResource);
        }
    }

    /// <summary>
    /// Get ModuleDbContext from service provider.
    /// Must be implemented by derived class for each module.
    /// </summary>
    protected abstract ModuleDbContext GetDbContext(IServiceProvider serviceProvider);
}
