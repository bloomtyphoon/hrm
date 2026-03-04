# HRM Solution Structure

## Overview

HRM (Human Resource Management) is an **ASP.NET Core 8** application built as a **Modular Monolith** following Domain-Driven Design (DDD), CQRS, and Clean Architecture principles. Each business domain is encapsulated in an independent module with its own database schema, while sharing common infrastructure through a BuildingBlocks layer.

### Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8, C# 12 |
| Web API | ASP.NET Core 8 Minimal API |
| Web Frontend | ASP.NET Core 8 MVC, Bootstrap 5.3, Bootstrap Icons |
| ORM | Entity Framework Core 8 (SQL Server) |
| Mediator / CQRS | MediatR 14 |
| Validation | FluentValidation 12 |
| Architecture | Modular Monolith, DDD, CQRS, Clean Architecture |
| Auth | JWT Bearer tokens + ASP.NET Core Cookie auth (HRM.Web) |
| Messaging | Transactional Outbox Pattern (no external broker) |

---

## Directory Structure

```
HRM.slnx
src/
├── BuildingBlocks/
│   ├── HRM.BuildingBlocks.Domain/          # Pure domain primitives (no framework deps)
│   ├── HRM.BuildingBlocks.Application/     # CQRS contracts, pipeline behaviors
│   └── HRM.BuildingBlocks.Infrastructure/  # EF Core base, middleware, security impl
│
├── Modules/
│   ├── Identity/
│   │   ├── HRM.Modules.Identity.Domain/
│   │   ├── HRM.Modules.Identity.IntegrationEvents/
│   │   ├── HRM.Modules.Identity.Application/
│   │   ├── HRM.Modules.Identity.Infrastructure/
│   │   └── HRM.Modules.Identity.Api/
│   ├── Organization/               # same 5-project structure
│   ├── Personnel/                  # same 5-project structure
│   └── Attendance/                 # same 5-project structure
│
├── Apps/
│   ├── HRM.Api/                    # Minimal API host (entry point for backend)
│   └── HRM.Web/                    # ASP.NET Core MVC frontend
│
└── Database/
    ├── Identity/                   # SQL migration scripts
    ├── Organization/
    ├── Personnel/
    └── Attendance/
```

---

## BuildingBlocks Layer

The BuildingBlocks layer contains shared infrastructure and abstractions. No module may reference another module — all cross-cutting concerns go through BuildingBlocks.

### HRM.BuildingBlocks.Domain

Pure domain primitives with no framework dependencies.

#### Entity Hierarchy

```
Entity
│  Guid Id
│  List<IDomainEvent> DomainEvents
│  Equality by Id (not reference)
│
├── AuditableEntity
│     DateTime CreatedAtUtc
│     DateTime? ModifiedAtUtc
│     Guid? CreatedById
│     Guid? ModifiedById
│
└── SoftDeletableEntity (also AuditableEntity)
      bool IsDeleted
      DateTime? DeletedAtUtc
      Guid? DeletedById
```

#### Result Pattern

All command handlers return `Result` or `Result<TValue>` — no exceptions for business logic flows.

```csharp
// Void result (success or failure)
public sealed class Result
{
    public bool IsSuccess { get; }
    public DomainError? Error { get; }

    public static Result Success();
    public static Result Failure(DomainError error);
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<DomainError, TResult> onFailure);
}

// Typed result
public sealed class Result<TValue>
{
    public bool IsSuccess { get; }
    public TValue Value { get; }     // only valid when IsSuccess = true
    public DomainError? Error { get; }

    public static Result<TValue> Success(TValue value);
    public static Result<TValue> Failure(DomainError error);
}
```

#### DomainError Types

All domain errors derive from `DomainError` with a `Code` (machine-readable) and `Message` (human-readable):

| Type | HTTP Status | Use Case |
|---|---|---|
| `NotFoundError` | 404 | Resource not found |
| `ConflictError` | 409 | Duplicate/state conflict |
| `ValidationError` | 400 | Input validation failure (with `Details` dict) |
| `UnauthorizedError` | 401 | Authentication failure |
| `ForbiddenError` | 403 | Authorization failure |
| `FailureError` | 500 | System/infrastructure error |

#### Events

```csharp
// Raised inside aggregates, dispatched by ModuleDbContext before SaveChanges
public abstract record DomainEvent : IDomainEvent;

// Serialized to OutboxMessages table for reliable async delivery
public interface IIntegrationEvent { Guid Id { get; } DateTime OccurredOnUtc { get; } }
```

#### Security Abstractions

```csharp
// Identifies which "level" of data a user can see for a permission
public enum DataScopeLevel { None = 0, Self = 1, EmployeeSet = 2, Position = 3, Department = 4, Company = 5, Global = 6 }

// Immutable compiled scope filter - passed to query handlers
public sealed class DataScopeRule
{
    public DataScopeLevel Level { get; }
    public Guid? SelfEmployeeId { get; }          // for Self scope
    public IReadOnlySet<Guid> EmployeeIds { get; } // for EmployeeSet scope
    public IReadOnlySet<Guid> DimensionIds { get; } // for Company/Department/Position scope

    public static DataScopeRule Self(Guid employeeId);
    public static DataScopeRule EmployeeSet(Guid selfId, IEnumerable<Guid> employeeIds);
    public static DataScopeRule Company(IEnumerable<Guid> companyIds);
    public static DataScopeRule Global();
    public static DataScopeRule None();
}

// Entities that belong to a specific employee (own their data)
public interface IScopedEntity { Guid OwnerId { get; } }

// Entities that belong to a tenant (multi-tenancy)
public interface ITenantEntity { Guid TenantId { get; } }
```

---

### HRM.BuildingBlocks.Application

CQRS contracts and MediatR pipeline behaviors.

#### CQRS Abstractions

```csharp
// Command returning a value (mutating operation)
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

// Module-scoped command — declares which DbContext to commit via UoW
public interface IModuleCommand<TResponse> : ICommand<TResponse>, IHasModuleName { }
public interface IHasModuleName { string ModuleName { get; } }

// Command with no return value
public interface IModuleCommand : ICommand, IHasModuleName { }

// Auditable command — AuditBehavior injects IpAddress + UserAgent automatically
public interface IAuditableCommand { string? IpAddress { get; set; } string? UserAgent { get; set; } }

// Query (read-only)
public interface IQuery<TResponse> : IRequest<TResponse> { }
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where TQuery : IQuery<TResponse> { }

// Pagination
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);
```

#### MediatR Pipeline Behaviors (Order of Execution)

```
Request
  │
  ▼ 1. LoggingBehavior       — Log request name; start Stopwatch; log elapsed on completion
  │
  ▼ 2. AuditBehavior         — If IAuditableCommand: inject IpAddress + UserAgent from IClientInfoService
  │
  ▼ 3. ValidationBehavior    — Run FluentValidation validators in parallel
  │                             If any fail: return Result.Failure(ValidationError) immediately
  │
  ▼ 4. Handler               — Business logic, domain model operations, repository calls
  │
  ▼ 5. UnitOfWorkBehavior    — If result.IsSuccess and IHasModuleName:
  │                             resolve IModuleUnitOfWork by ModuleName → CommitAsync()
  │                             (saves changes + dispatches domain events + outbox in one TX)
  ▼
Response
```

> Queries skip step 5 (no UoW) because they don't modify state.

#### Cross-Module Contracts

Modules communicate with each other only through BuildingBlocks-defined contracts:

```csharp
// Personnel module implements this; Attendance module depends on it
public interface IPersonnelQuery
{
    Task<IReadOnlyList<Guid>> GetEmployeeCompanyIdsAsync(Guid employeeId, CancellationToken ct);
    // ... other cross-module query methods
}

// Organization module implements this; other modules depend on it
public interface IOrganizationQuery { /* company/dept/position lookups */ }

// Personnel module implements this; Attendance module depends on it
public interface IDataScopeService
{
    Task<DataScopeRule> GetScopeRuleAsync(Guid userId, PermissionDescriptor permission, CancellationToken ct);
}
```

#### Execution Context

```csharp
// Populated per HTTP request from ClaimsPrincipal
public interface IExecutionContext
{
    Guid UserId { get; }
    Guid TenantId { get; }
    string? FullName { get; }
}
```

---

### HRM.BuildingBlocks.Infrastructure

Framework-specific implementations of the abstractions above.

#### ModuleDbContext (Base Class for All Module DbContexts)

```csharp
public abstract class ModuleDbContext : DbContext, IModuleUnitOfWork
{
    public abstract string ModuleName { get; }
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply global query filters:
        // 1. Tenant isolation: ITenantEntity → WHERE TenantId = @currentTenantId
        // 2. Soft delete:      ISoftDeletable → WHERE IsDeleted = 0
        ApplyGlobalFilters(modelBuilder);
    }

    // CommitAsync: dispatch domain events → serialize integration events to outbox → SaveChanges
    public async Task CommitAsync(CancellationToken ct)
    {
        // 1. Collect domain events from all tracked entities
        // 2. MediatR.Publish each domain event (handlers may add OutboxMessages)
        // 3. SaveChangesAsync — ALL in one DB transaction
    }
}
```

#### AuditInterceptor

EF Core `SaveChangesInterceptor` that auto-sets `CreatedAtUtc`, `ModifiedAtUtc`, `CreatedById`, `ModifiedById` on entities implementing `IAuditableEntity`.

#### OutboxProcessor

```csharp
// Base class for per-module outbox background services
public abstract class OutboxProcessor : BackgroundService
{
    // Every ~5 seconds:
    // 1. SELECT TOP N FROM [Module].OutboxMessages WHERE ProcessedAtUtc IS NULL
    // 2. Deserialize each → resolve IIntegrationEventHandler<T> from DI → handle
    // 3. UPDATE SET ProcessedAtUtc = NOW
}
```

#### RoutePermissionMiddleware

Reads all `RouteSecurityMap.xml` files (one per module, embedded resources) and checks permission before the endpoint executes:

```
Request → match route to RouteSecurityMap entry
        → IPermissionService.HasPermissionAsync(userId, permissionDescriptor)
        → 403 Forbidden if no permission
        → store PermissionDescriptor in HttpContext.Items["CurrentPermission"]
        → continue to endpoint handler
```

#### ResultExtensions

Maps `Result<T>` / `DomainError` to ASP.NET Core `IResult`:

```csharp
// Simple usage:
return result.ToHttpResult(id => Results.Created($"/api/attendance/{id}", id));

// Void command:
return result.ToHttpResult(); // 204 No Content on success

// DomainError → HTTP mapping:
// NotFoundError   → 404 { code, message }
// ConflictError   → 409 { code, message }
// ValidationError → 400 { code, message, details }
// UnauthorizedError → 401 { code, message }
// ForbiddenError  → 403 { code, message }
// FailureError    → 500 Problem
```

---

## Module Structure (5 Projects per Module)

Every business module follows this exact pattern:

```
HRM.Modules.<Module>.Domain
  Entities/              — Aggregate roots, entities, value objects
  Events/                — Domain events (raised inside aggregates)
  Errors/                — Static error factory methods
  Abstractions/          — Repository interfaces, domain service interfaces

HRM.Modules.<Module>.IntegrationEvents
  *IntegrationEvent.cs   — Plain DTOs; implement IIntegrationEvent
                           No domain logic, no framework deps

HRM.Modules.<Module>.Application
  Commands/<Name>/       — Command, CommandHandler, CommandValidator (3 files per command)
  Queries/<Name>/        — Query, QueryHandler, DTOs
  Abstractions/          — Repository interfaces (used by handlers), query context interfaces
  Security/              — Permission constants (PermissionDescriptor instances)
  Resources/
    PermissionCatalog.xml  — Embedded resource: declares permissions + allowed scopes
  DependencyInjection/   — AddXxxApplication() extension method (registers MediatR + validators)

HRM.Modules.<Module>.Infrastructure
  Persistence/
    *DbContext.cs          — Inherits ModuleDbContext; sets schema; applies EF configs
    Configurations/        — IEntityTypeConfiguration<T> classes
    Repositories/          — IRepository implementations
  DomainEventHandlers/   — INotificationHandler<DomainEvent> → create integration events
  BackgroundServices/    — *OutboxProcessor.cs (extends OutboxProcessor)
  Security/
    RouteSecurityMap.xml   — Embedded resource: declares protected routes
  DependencyInjection.cs — AddXxxModule() extension method (registers DbContext, repos, etc.)

HRM.Modules.<Module>.Api
  Contracts/             — Request/response DTOs (separate from Application DTOs)
  Endpoints/             — Static classes with MapXxxEndpoints() extension method
  DependencyInjection/   — MapXxxEndpoints() routing registration
```

**Key isolation rule**: No module project may reference another module project. All shared contracts live in BuildingBlocks.

---

## Identity Module

**Purpose**: Authentication, authorization, user accounts, roles, and permissions.

**DB Schema**: `Identity.*`

### Entities

| Entity | Description |
|---|---|
| `Account` | User account: email, passwordHash, status (Active/Suspended/Deactivated), profileType (System/Employee) |
| `RefreshToken` | JWT refresh token: tokenHash, expiry, deviceName, ipAddress, userAgent, isRevoked |
| `Role` | Named role with a set of permission grants and scope grants |
| `AccountRole` | Junction: Account ↔ Role (many-to-many) |
| `EmployeeCompanyAccess` | Which companies an employee account can access |
| `SystemProfile` | Profile for system admin accounts (name, email) |
| `EmployeeProfile` | Profile for employee accounts (links to Personnel.Employee via employeeId Guid) |

### Commands

| Command | Description |
|---|---|
| `RegisterAccount` | Create new account with email + password + profileType |
| `Login` | Verify credentials → issue JWT + RefreshToken (with IAuditableCommand for IP/UA) |
| `Logout` | Revoke current refresh token |
| `RefreshToken` | Exchange expired JWT for new one using refresh token |
| `ChangeMyPassword` | Authenticated user changes their own password |
| `ResetAccountPassword` | Admin resets another account's password |
| `UpdateProfile / UpdateSystemProfile / UpdateEmployeeProfile` | Update profile fields |
| `CreateSystemProfile / CreateEmployeeProfile` | Create profile for new account |
| `ActivateAccount / DeactivateAccount / SuspendAccount / UnlockAccount` | Account lifecycle management |
| `GrantSuperAdmin / RevokeSuperAdmin` | Super-admin privilege management |
| `AssignRolesToAccount / RemoveRolesFromAccount` | Role assignment |
| `CreateRole / UpdateRole / DeleteRole` | Role management |
| `RevokeSession / RevokeAllSessionsExceptCurrent` | Session management |
| `EnableTwoFactor / DisableTwoFactor` | 2FA management |

### Queries

| Query | Description |
|---|---|
| `GetAccountById / GetAccounts` | Account lookup (paginated) |
| `GetRoleById / GetRoles` | Role lookup |
| `GetAccountRoles` | Roles assigned to a specific account |
| `GetActiveSessions` | Active refresh token sessions for current user |
| `GetSystemProfile / GetEmployeeProfile` | Profile details |
| `GetPermissionCatalog` | All permissions from all modules (for role management UI) |

### API Routes

```
POST /api/identity/accounts/register
POST /api/identity/accounts/login
POST /api/identity/accounts/logout
POST /api/identity/accounts/refresh
GET  /api/identity/accounts
GET  /api/identity/accounts/{id}
PUT  /api/identity/accounts/{id}/password (reset)
...
GET  /api/identity/roles
POST /api/identity/roles
GET  /api/identity/permissions/catalog
```

---

## Organization Module

**Purpose**: Multi-tenant hierarchy: Tenants → Companies → Departments → Positions.

**DB Schema**: `Organization.*`

### Entities

| Entity | Description |
|---|---|
| `Tenant` | Root of multi-tenancy: name, status (Active/Suspended/Deactivated) |
| `Company` | Business company within a tenant: name, code, IsActive |
| `Department` | Org unit within a company: name, parentDepartmentId (tree structure), managerId |
| `Position` | Job position: title, companyId, departmentId, IsActive/Closed status |

### Commands

| Area | Commands |
|---|---|
| Tenant | CreateTenant, UpdateTenant, ActivateTenant, SuspendTenant, DeactivateTenant |
| Company | CreateCompany, UpdateCompany, ActivateCompany, DeactivateCompany |
| Department | CreateDepartment, UpdateDepartment, ActivateDepartment, DeactivateDepartment, MoveDepartment, AssignDepartmentManager, RemoveDepartmentManager |
| Position | CreatePosition, UpdatePosition, ActivatePosition, DeactivatePosition, ClosePosition, MovePositionToDepartment |

### Queries

| Query | Description |
|---|---|
| `GetTenants / GetTenantById` | Tenant listing/detail |
| `GetCompanies / GetCompanyById` | Company listing/detail |
| `GetDepartmentsByCompany / GetDepartmentById` | Department tree for a company |
| `GetPositionsByCompany / GetPositionsByDepartment / GetPositionById` | Position listing/detail |

### API Routes

```
GET  /api/tenants, POST /api/tenants, GET /api/tenants/{id}
GET  /api/companies, POST /api/companies, PUT /api/companies/{id}
GET  /api/departments, POST /api/departments, PUT /api/departments/{id}
GET  /api/positions, POST /api/positions, PUT /api/positions/{id}
```

---

## Personnel Module

**Purpose**: Employee master data, work assignments, reporting hierarchy, and data scope resolution.

**DB Schema**: `Personnel.*`

### Entities

| Entity | Description |
|---|---|
| `Employee` | Person: firstName, lastName, email, phone, dateOfBirth, status (Active/Terminated), userId (links to Identity.Account) |
| `EmployeeAssignment` | Employee ↔ Company/Department/Position: isPrimary, startDate, endDate (history-aware) |

### Commands

| Command | Description |
|---|---|
| `CreateEmployee` | Onboard new employee (creates Personnel.Employee + optionally links Identity.Account) |
| `UpdateEmployee` | Update personal details |
| `TerminateEmployee` | Mark employee as terminated, end all active assignments |
| `AddAssignment` | Assign employee to company/department/position |
| `SetPrimaryAssignment` | Mark one assignment as primary |
| `EndAssignment` | Close an active assignment with end date |
| `AssignManager` | Set reporting manager for an employee |
| `RemoveManager` | Remove reporting manager |

### Queries

| Query | Description |
|---|---|
| `GetEmployees` | Paginated employee list (with DataScope filter applied) |
| `GetEmployeeById` | Single employee detail (scope-checked) |
| `GetEmployeeAssignments` | All assignments for an employee (history) |
| `GetDirectReports` | Employees that report to a given manager |

### Special: Data Scope Implementation

The Personnel module provides two critical cross-module services:

1. **`IDataScopeService`** — `DataScopeService` implementation resolves a `DataScopeRule` for the current user and permission by querying the user's scope grants from the Identity database.

2. **`IPersonnelQuery`** — Provides `GetEmployeeCompanyIdsAsync(employeeId)` used by Attendance and other modules to get company context without cross-module DB joins.

### API Routes

```
GET  /api/employees, POST /api/employees
GET  /api/employees/{id}
PUT  /api/employees/{id}
POST /api/employees/{id}/assignments
GET  /api/employees/{id}/assignments
```

---

## Attendance Module

**Purpose**: Daily check-in/check-out records with multi-scope visibility.

**DB Schema**: `Attendance.*`

### Entities

```csharp
public class AttendanceRecord : AuditableEntity, IScopedEntity, ITenantEntity
{
    public Guid TenantId { get; }
    public Guid EmployeeId { get; }          // owner (IScopedEntity.OwnerId)
    public DateOnly Date { get; }            // derived from CheckInTimeUtc (UTC date)
    public DateTime CheckInTimeUtc { get; }  // full UTC timestamp (DATETIME2 in DB)
    public DateTime? CheckOutTimeUtc { get; }
    public AttendanceStatus Status { get; }  // CheckedIn = 1, CheckedOut = 2
    public bool IsManualEntry { get; }       // true = created by HR
    public Guid? CompanyId { get; }          // denormalized for Company-scope filtering
    public string? Notes { get; }
}
```

**DB Constraints**:
- Unique index `(EmployeeId, Date)` — prevents duplicate check-in per day; also catches race conditions
- Composite index `(EmployeeId, Date, Status)` — optimizes `GetActiveCheckIn` query

### Commands

| Command | Description |
|---|---|
| `CheckIn` | Employee self check-in (employeeId resolved from DataScopeRule, not request body) |
| `CheckOut` | Employee self check-out (finds active check-in for today, calls `record.CheckOut()`) |
| `RecordManualAttendance` | HR creates a complete record (check-in + check-out) for any employee |

### Queries

| Query | Scope |
|---|---|
| `GetMyAttendance` | Always Self — employee sees only their own records |
| `GetEmployeeAttendance` | Scope-filtered — manager/HR scoped to their allowed employees |
| `GetAttendanceById` | Scope-checked — verifies caller has access to this record |

### Permissions

| Permission | Default Scope | Allowed Scopes |
|---|---|---|
| `Attendance.Record.CheckIn` | Self | Self only |
| `Attendance.Record.CheckOut` | Self | Self only |
| `Attendance.Record.View` | Self | Self, EmployeeSet, Company, Global |
| `Attendance.Record.ManualRecord` | Company | Company, Global |

### Domain Events → Integration Events

```
AttendanceCheckedInDomainEvent  →  AttendanceCheckedInIntegrationEvent  → OutboxMessages
AttendanceCheckedOutDomainEvent →  (no integration event currently)
```

### API Routes

```
POST /api/attendance/check-in
POST /api/attendance/check-out
GET  /api/attendance/me                          # self history
GET  /api/attendance/employees/{employeeId}      # scoped employee view
GET  /api/attendance/records/{id}                # single record
POST /api/attendance/records                     # manual entry (HR)
```

---

## HRM.Api — Backend Entry Point

`HRM.Api` is the single deployable ASP.NET Core host that combines all modules.

### Module Registration Order

Modules must be registered in dependency order:

```csharp
// src/Apps/HRM.Api/DependencyInjection/ModuleExtensions.cs

services.AddIdentityApplication();
services.AddIdentityModule(configuration);       // registers IPermissionService

services.AddOrganizationApplication();
services.AddOrganizationModule(configuration);   // registers Company/Dept/Position DbContexts

services.AddPersonnelApplication();
services.AddPersonnelModule(configuration);      // registers IDataScopeService, IPersonnelQuery

services.AddAttendanceApplication();
services.AddAttendanceModule(configuration);     // depends on IDataScopeService (Personnel)
```

### Middleware Pipeline

```csharp
// src/Apps/HRM.Api/Program.cs

app.UseAuthentication();         // validate JWT, populate ClaimsPrincipal
app.UseAuthorization();          // ASP.NET Core policy engine
app.UseRoutePermissions();       // RoutePermissionMiddleware (check permission per route)
app.MapModuleEndpoints();        // register all module endpoint groups under /api/...
```

### Route Security Architecture

Each module declares protected routes via an embedded XML resource:

```xml
<!-- HRM.Modules.Attendance.Infrastructure/Security/RouteSecurityMap.xml -->
<ProtectedRoutes>
  <Route Method="POST" Path="/api/attendance/check-in"             Permission="Attendance.Record.CheckIn" />
  <Route Method="GET"  Path="/api/attendance/me"                   Permission="Attendance.Record.View" />
  <Route Method="POST" Path="/api/attendance/records"              Permission="Attendance.Record.ManualRecord" />
</ProtectedRoutes>
```

### Permission Catalog Architecture

Each module declares its permissions via another embedded XML:

```xml
<!-- HRM.Modules.Attendance.Application/Resources/PermissionCatalog.xml -->
<PermissionCatalog Module="Attendance">
  <Permissions>
    <Module name="Attendance" displayName="Chấm công">
      <Entity name="Record">
        <Action name="View" defaultScope="Self">
          <Scopes>
            <Scope value="Self" /><Scope value="EmployeeSet" /><Scope value="Company" /><Scope value="Global" />
          </Scopes>
        </Action>
      </Entity>
    </Module>
  </Permissions>
</PermissionCatalog>
```

---

## HRM.Web — MVC Frontend

`HRM.Web` is an ASP.NET Core MVC application that acts as a BFF (Backend for Frontend), calling `HRM.Api` via typed HTTP clients.

### Architecture

```
Browser
  ↕ (Cookie-based session)
HRM.Web (ASP.NET Core MVC)
  ↕ (HTTP + JWT Bearer, via typed HttpClients)
HRM.Api (Minimal API)
  ↕ (Entity Framework Core)
SQL Server
```

### Typed HTTP Clients

| Interface | Implementation | Purpose |
|---|---|---|
| `IIdentityApiClient` | `AccountApiClient` | Auth, accounts, roles |
| `IOrganizationApiClient` | `OrganizationApiClient` | Companies, departments, positions |
| `IPersonnelApiClient` | `PersonnelApiClient` | Employees, assignments |
| `IAttendanceApiClient` | `AttendanceApiClient` | Attendance records |

All clients are registered with `AddHttpClient<I, T>(...).AddHttpMessageHandler<AuthTokenHandler>()`.

### AuthTokenHandler

```csharp
// DelegatingHandler injected into all typed HTTP clients
// 1. Read JWT access token from "HRM.AccessToken" cookie
// 2. Attach as "Authorization: Bearer <token>" header
// 3. If token expires in < 2 minutes: proactively call /api/identity/accounts/refresh
//    → update cookie → continue with new token
```

### Controllers

| Controller | Views / Actions |
|---|---|
| `AuthController` | Login, Logout |
| `AccountController` | Index, Create, Edit, Sessions, AssignRoles |
| `TenantController` | Index, Create, Edit |
| `CompanyController` | Index, Create, Edit, Activate/Deactivate |
| `DepartmentController` | Index, Create, Edit, AssignManager |
| `PositionController` | Index, Create, Edit |
| `EmployeeController` | Index, Create, Edit, Terminate, AddAssignment |
| `AttendanceController` | MyAttendance, CheckIn, CheckOut, Index, Details, RecordManual |
| `RoleController` | Index, Create, Edit, Permissions |

### Layout Features

- Responsive Bootstrap 5.3 nav with Bootstrap Icons
- Company switcher ViewComponent (reads/writes `HRM.SelectedCompanyId` cookie; sends `X-Company-Id` header to API)
- TempData-based flash messages (success/error)
- Authenticated/anonymous nav branches

---

## Key Design Patterns

### 1. Result Pattern (No Exceptions for Business Logic)

```csharp
// Handler never throws for business errors:
var employee = await _repo.GetByIdAsync(command.EmployeeId);
if (employee is null)
    return Result.Failure(PersonnelErrors.EmployeeNotFound(command.EmployeeId));  // → 404

// Caller pattern-matches on success/failure:
return result.ToHttpResult(id => Results.Created($"/api/employees/{id}", id));
```

### 2. Module Isolation (Weak References)

Modules reference other modules' data only by `Guid` (no EF navigation properties across schemas):

```csharp
// Personnel.EmployeeAssignment
public Guid CompanyId { get; }     // refers to Organization.Company — no FK constraint
public Guid DepartmentId { get; }  // refers to Organization.Department — no FK constraint

// Attendance.AttendanceRecord
public Guid EmployeeId { get; }    // refers to Personnel.Employee — no FK constraint
public Guid? CompanyId { get; }    // denormalized from Personnel at check-in time
```

### 3. Transactional Outbox (Reliable Messaging)

Domain events → integration events → `OutboxMessages` table are all committed in the same database transaction. Background `OutboxProcessor` delivers them asynchronously. No message is lost even if the process crashes immediately after the business transaction commits.

### 4. Global Query Filters (Automatic Tenant + Soft Delete Isolation)

```csharp
// Applied in ModuleDbContext.OnModelCreating() for all entities:
modelBuilder.Entity<AttendanceRecord>().HasQueryFilter(e => e.TenantId == _tenantId && !e.IsDeleted);
// Handler code never needs to filter by TenantId — it's always automatic.
```

### 5. Data Scope (Fine-Grained Data Access Control)

A two-layer security model:
- **Layer 1 (Route)**: Can this user perform this action at all? (`RoutePermissionMiddleware` → binary yes/no)
- **Layer 2 (Data)**: What subset of data can they see? (`IDataScopeService` → `DataScopeRule` applied in query handlers)

```csharp
// Example: manager can see direct reports' attendance
var rule = await _dataScopeService.GetScopeRuleAsync(userId, AttendancePermissions.Record.View, ct);
// rule.Level = EmployeeSet, rule.EmployeeIds = {subordinate1, subordinate2}

query = query.Where(r => rule.EmployeeIds.Contains(r.EmployeeId));
```
