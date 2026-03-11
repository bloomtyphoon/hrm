using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Events;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.SoftDelete;
using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
using HRM.BuildingBlocks.Domain.Entities;
using HRM.BuildingBlocks.Infrastructure.Inbox;
using HRM.BuildingBlocks.Infrastructure.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Base DbContext for all modules implementing UnitOfWork pattern
///
/// Responsibilities:
/// 1. Provide DbSet for OutboxMessages (transactional outbox pattern)
/// 2. Implement IUnitOfWork.CommitAsync() with domain event dispatch
/// 3. Implement IModuleContext for basic database operations
/// 4. Automatic audit field updates (CreatedAtUtc, ModifiedAtUtc)
///
/// Usage Pattern:
/// Each module creates its own DbContext inheriting from this:
///
/// <code>
/// public class IdentityDbContext : ModuleDbContext
/// {
///     public override string ModuleName => "Identity";
///
///     public DbSet<Account> Accounts => Set<Account>();
///     public DbSet<User> Users => Set<User>();
///
///     public IdentityDbContext(DbContextOptions<IdentityDbContext> options, IPublisher publisher)
///         : base(options, publisher)
///     {
///     }
///
///     protected override void OnModelCreating(ModelBuilder modelBuilder)
///     {
///         base.OnModelCreating(modelBuilder);
///         modelBuilder.HasDefaultSchema("identity");
///         modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
///     }
/// }
/// </code>
///
/// Key Features:
/// - Domain event dispatch BEFORE SaveChanges (synchronous, in transaction)
/// - OutboxMessage creation by domain event handlers (in same transaction)
/// - Automatic audit trail (CreatedAtUtc, ModifiedAtUtc)
/// - Transaction rollback on any failure
/// - Clean separation of domain and integration events
/// </summary>
public abstract class ModuleDbContext : DbContext, IModuleUnitOfWork
{
    private readonly IPublisher _publisher;
    private readonly ITenantContext? _tenantContext;

    /// <summary>
    /// Module name for identification
    /// Must be overridden by derived classes
    ///
    /// Examples: "Identity", "Personnel", "Organization"
    /// </summary>
    public abstract string ModuleName { get; }

    /// <summary>
    /// OutboxMessages table for this module.
    /// Each module has its own outbox table for isolation.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <summary>
    /// InboxMessages table for this module.
    /// Tracks processed integration events for idempotency (Inbox pattern).
    /// </summary>
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    /// <summary>
    /// Protected constructor for derived module DbContexts.
    /// </summary>
    /// <param name="options">DbContext options (connection string, etc.)</param>
    /// <param name="publisher">MediatR publisher for domain events</param>
    /// <param name="tenantContext">
    /// Current tenant context. Null for background services (no HTTP context).
    /// When null, the DbContext operates as SystemTenant (system-wide access),
    /// which is semantically equivalent but makes the intent explicit:
    /// background services are authorised system-level operations, not anonymous requests.
    /// </param>
    protected ModuleDbContext(DbContextOptions options, IPublisher publisher, ITenantContext? tenantContext = null)
        : base(options)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns the current tenant ID from the request context.
    /// Evaluated at query time (not at model build time) — safe for per-request scoping.
    ///
    /// When ITenantContext is absent (background services / DI scopes without HTTP context),
    /// returns WellKnownTenants.SystemTenantId to make the intent explicit:
    /// "this context has system-wide access" rather than "no context = uncontrolled bypass".
    /// The EF Core filter behaviour (allow all rows) is identical to the previous null path,
    /// but the SystemTenantId path is deliberate and auditable.
    /// </summary>
    private Guid? GetCurrentTenantId() =>
        _tenantContext?.TenantId ?? WellKnownTenants.SystemTenantId;

    /// <summary>
    /// Add an integration event to the outbox for reliable asynchronous publishing.
    /// The event is serialized to JSON and stored as an OutboxMessage.
    /// It will be saved atomically with domain changes during SaveChanges/CommitAsync.
    /// The OutboxProcessor background service will pick it up and publish to event bus.
    /// </summary>
    public void AddIntegrationEvent(IIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var message = OutboxMessage.Create(
            type: integrationEvent.GetType().AssemblyQualifiedName!,
            content: JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            occurredOnUtc: integrationEvent.OccurredOnUtc);

        OutboxMessages.Add(message);
    }

    /// <summary>
    /// Check if an integration event has already been processed by a specific handler.
    /// Used by the Inbox pattern for idempotent event processing.
    /// </summary>
    public async Task<bool> IsEventProcessedAsync(
        Guid eventId,
        string handlerName,
        CancellationToken cancellationToken = default)
    {
        return await InboxMessages.AnyAsync(
            m => m.EventId == eventId && m.HandlerName == handlerName,
            cancellationToken);
    }

    /// <summary>
    /// Mark an integration event as processed by a specific handler.
    /// Should be called after successful processing, within the same transaction.
    /// </summary>
    public void MarkEventAsProcessed(Guid eventId, string eventType, string handlerName)
    {
        InboxMessages.Add(InboxMessage.Create(eventId, eventType, handlerName));
    }

    /// <summary>
    /// Commit all changes with domain event dispatch
    ///
    /// Workflow:
    /// 1. Collect domain events from tracked entities
    /// 2. Dispatch domain events synchronously (BEFORE SaveChanges)
    /// 3. Domain event handlers create OutboxMessages
    /// 4. Save all changes in single transaction (entities + outbox messages)
    /// 5. Clear domain events from entities
    ///
    /// Transaction Guarantee:
    /// - All changes saved atomically
    /// - If any step fails, entire transaction rolls back
    /// - No partial commits
    ///
    /// Example Timeline:
    /// T+0ms:   CommitAsync() called
    /// T+1ms:   Collect domain events (AccountCreatedDomainEvent)
    /// T+2ms:   Dispatch event to handlers
    /// T+5ms:   Handler creates AccountRegisteredIntegrationEvent
    /// T+6ms:   Handler creates OutboxMessage
    /// T+7ms:   SaveChanges() saves Account + OutboxMessage
    /// T+10ms:  Clear domain events
    /// T+11ms:  Return success
    ///
    /// Later:
    /// T+60s:   OutboxProcessor publishes integration event
    /// </summary>
    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        // Step 1: Collect domain events from all tracked entities
        var domainEvents = ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.DomainEvents.Any())
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        // Step 2: Dispatch domain events synchronously (in transaction)
        // Handlers can create OutboxMessages which will be saved in step 3
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        // Step 3: Save all changes atomically
        // - Domain entities (Account, Employee, etc.)
        // - OutboxMessages created by domain event handlers
        // - All in SINGLE database transaction
        var result = await base.SaveChangesAsync(cancellationToken);

        // Step 4: Clear domain events to prevent duplicate dispatch
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return result;
    }

    /// <summary>
    /// Override SaveChangesAsync - audit fields handled by AuditInterceptor
    /// This is called by CommitAsync and can also be called directly by background services
    ///
    /// Note: Audit field updates (CreatedById, ModifiedById, ModifiedAtUtc) are now
    /// handled by AuditInterceptor which is registered as an EF Core SaveChangesInterceptor.
    /// The interceptor has access to ICurrentUserService for user tracking.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit updates handled by AuditInterceptor
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Configure conventions for all modules
    /// Override in derived classes to add module-specific configurations
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure OutboxMessage entity
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Content)
                .IsRequired();

            entity.Property(e => e.OccurredOnUtc)
                .IsRequired();

            entity.Property(e => e.Error)
                .HasMaxLength(2000);

            // Index for efficient querying of unprocessed messages
            entity.HasIndex(e => e.ProcessedOnUtc)
                .HasDatabaseName("IX_OutboxMessages_ProcessedOnUtc");

            // Index for ordering by occurrence time
            entity.HasIndex(e => e.OccurredOnUtc)
                .HasDatabaseName("IX_OutboxMessages_OccurredOnUtc");
        });

        // Configure InboxMessage entity
        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventId)
                .IsRequired();

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.HandlerName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ProcessedOnUtc)
                .IsRequired();

            // Unique index for idempotency check: same event + same handler = already processed
            entity.HasIndex(e => new { e.EventId, e.HandlerName })
                .IsUnique()
                .HasDatabaseName("IX_InboxMessages_EventId_HandlerName");

            // Index for cleanup queries
            entity.HasIndex(e => e.ProcessedOnUtc)
                .HasDatabaseName("IX_InboxMessages_ProcessedOnUtc");
        });

        // Configure global query filters for soft delete
        // Automatically exclude soft-deleted entities from all queries
        // Can be disabled per query with: query.IgnoreQueryFilters()
        ConfigureSoftDeleteQueryFilter(modelBuilder);

        // Configure global query filters for multi-tenancy
        // Automatically filter entities by current tenant
        // System tenant (WellKnownTenants.SystemTenantId) bypasses the filter
        ConfigureTenantQueryFilter(modelBuilder);
    }

    /// <summary>
    /// Configure global query filter to exclude soft-deleted entities
    /// Applies to all entities implementing ISoftDeletable
    ///
    /// How It Works:
    /// 1. Iterate through all entity types in the model
    /// 2. Check if entity implements ISoftDeletable interface
    /// 3. Build expression: e => e.IsDeleted == false
    /// 4. Set as global query filter
    ///
    /// Effect:
    /// - All queries automatically filter: WHERE IsDeleted = 0
    /// - Applies to: Find, FirstOrDefault, Where, ToList, etc.
    /// - Cascades to navigation properties
    ///
    /// Override Filter:
    /// To include soft-deleted entities in specific queries:
    /// <code>
    /// var allEmployees = await context.Employees
    ///     .IgnoreQueryFilters()  // Include soft-deleted
    ///     .ToListAsync();
    /// </code>
    ///
    /// Benefits:
    /// - Prevents accidental access to deleted data
    /// - Consistent behavior across application
    /// - No manual WHERE clauses needed
    /// - Safer than manual filtering
    ///
    /// Example Generated SQL:
    /// <code>
    /// -- Before (manual):
    /// SELECT * FROM Employees WHERE IsDeleted = 0 AND Department = 'IT'
    ///
    /// -- After (automatic):
    /// SELECT * FROM Employees WHERE IsDeleted = 0 AND Department = 'IT'
    /// </code>
    /// </summary>
    /// <param name="modelBuilder">Model builder</param>
    /// <summary>
    /// Configure global query filter for multi-tenancy.
    /// Applied to all entities implementing ITenantEntity.
    ///
    /// Filter logic:
    ///   GetCurrentTenantId() == null              → background service, no filter (bypass)
    ///   GetCurrentTenantId() == SystemTenantId    → system admin, sees all data (bypass)
    ///   GetCurrentTenantId() == entity.TenantId   → customer tenant, filtered access
    ///
    /// IMPORTANT: EF Core evaluates GetCurrentTenantId() at query execution time
    /// (not at model build time), making it safe for per-request tenant scoping.
    /// This works because the filter expression closes over 'this' (the DbContext instance),
    /// and DbContext is registered as Scoped (new instance per request).
    /// </summary>
    private void ConfigureTenantQueryFilter(ModelBuilder modelBuilder)
    {
        var getMethod = typeof(ModuleDbContext)
            .GetMethod(nameof(GetCurrentTenantId), BindingFlags.NonPublic | BindingFlags.Instance)!;
        var contextExpr = Expression.Constant(this);
        var systemTenantIdExpr = Expression.Constant(
            (Guid?)WellKnownTenants.SystemTenantId, typeof(Guid?));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProp = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
            var tenantIdAsNullable = Expression.Convert(tenantIdProp, typeof(Guid?));

            // GetCurrentTenantId() — evaluated per query
            var currentTenantIdExpr = Expression.Call(contextExpr, getMethod);

            // Condition 1: GetCurrentTenantId() == null (background service / anonymous)
            var isNullContext = Expression.Equal(
                currentTenantIdExpr,
                Expression.Constant(null, typeof(Guid?)));

            // Condition 2: GetCurrentTenantId() == SystemTenantId (system admin bypass)
            var isSystemTenant = Expression.Equal(currentTenantIdExpr, systemTenantIdExpr);

            // Condition 3: e.TenantId == GetCurrentTenantId() (customer tenant match)
            var matchesTenant = Expression.Equal(tenantIdAsNullable, currentTenantIdExpr);

            // Tenant filter: (null context) OR (system tenant) OR (tenant match)
            Expression filterBody = Expression.OrElse(
                isNullContext,
                Expression.OrElse(isSystemTenant, matchesTenant));

            // If the entity also implements ISoftDeletable, combine both filters.
            // This replaces the soft-delete-only filter set by ConfigureSoftDeleteQueryFilter.
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var isDeletedProp = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = Expression.Equal(isDeletedProp, Expression.Constant(false));
                filterBody = Expression.AndAlso(notDeleted, filterBody);
            }

            entityType.SetQueryFilter(Expression.Lambda(filterBody, parameter));
        }
    }

    private void ConfigureSoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        // Get all entity types that implement ISoftDeletable (but NOT ITenantEntity —
        // those are handled in ConfigureTenantQueryFilter with combined filter)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)
                && !typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Build expression: e => e.IsDeleted == false
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var filterExpression = Expression.Lambda(
                    Expression.Equal(property, Expression.Constant(false)),
                    parameter
                );

                // Set as global query filter
                entityType.SetQueryFilter(filterExpression);
            }
        }
    }
}
