# HRM Module Flows

This document describes the end-to-end data flows through the HRM system: how requests are processed, how modules interact, and how the key architectural patterns operate at runtime.

---

## Table of Contents

1. [End-to-End HTTP Request Flow](#1-end-to-end-http-request-flow)
2. [Command Flow — CheckIn (Detailed Example)](#2-command-flow--checkin-detailed-example)
3. [Query Flow — GetMyAttendance (Detailed Example)](#3-query-flow--getmyattendance-detailed-example)
4. [Domain Event → Integration Event → Outbox Flow](#4-domain-event--integration-event--outbox-flow)
5. [Multi-Tenancy Flow](#5-multi-tenancy-flow)
6. [Two-Layer Security Flow](#6-two-layer-security-flow)
7. [Authentication Flow (Login)](#7-authentication-flow-login)
8. [Token Refresh Flow (Proactive)](#8-token-refresh-flow-proactive)
9. [HRM.Web → HRM.Api Client Flow](#9-hrmweb--hrmapi-client-flow)
10. [Module Registration Order & Dependency Chain](#10-module-registration-order--dependency-chain)
11. [MediatR Pipeline Behavior Execution](#11-mediatr-pipeline-behavior-execution)

---

## 1. End-to-End HTTP Request Flow

Every API request to `HRM.Api` passes through this middleware pipeline:

```
Browser / HRM.Web
      |
      | HTTP Request
      ↓
ASP.NET Core Kestrel
      |
      ↓ UseAuthentication
      |   JWT Bearer validation
      |   → if valid: populate ClaimsPrincipal (userId, tenantId, role, email claims)
      |   → if invalid/missing: principal = anonymous (not rejected here)
      |
      ↓ UseAuthorization
      |   ASP.NET Core policy engine
      |   → endpoint tagged with .RequireAuthorization() → check authenticated
      |   → 401 if unauthenticated
      |
      ↓ UseRoutePermissions  [RoutePermissionMiddleware]
      |   1. Match request (Method + Path) to RouteSecurityMap.xml entries
      |      (all modules register their XML at startup via IRouteSecurityService)
      |   2. If route is protected:
      |      IPermissionService.HasPermissionAsync(userId, permissionDescriptor)
      |      → query Identity DB: user → roles → role permissions → check match
      |   3. If no permission: return 403 Forbidden
      |   4. If permitted: store PermissionDescriptor in HttpContext.Items["CurrentPermission"]
      |      (handlers can retrieve this for data scope resolution)
      |
      ↓ Minimal API Endpoint Handler
      |   1. Bind and validate HTTP request → Command / Query record
      |   2. ISender.Send(command) — enters MediatR pipeline
      |   3. result.ToHttpResult(...) → HTTP response
      |
      ↓ HTTP Response
      |
Browser / HRM.Web
```

---

## 2. Command Flow — CheckIn (Detailed Example)

```
POST /api/attendance/check-in
Body: { "checkInTimeUtc": null, "notes": "Early today" }
Headers: Authorization: Bearer <jwt>
```

### Step 1 — Middleware

```
UseAuthentication   → extract userId, tenantId from JWT claims
UseAuthorization    → endpoint has .RequireAuthorization() → pass
RoutePermissionMiddleware:
  → match "POST /api/attendance/check-in" → Permission "Attendance.Record.CheckIn"
  → IPermissionService.HasPermissionAsync(userId, CheckIn)
     → SELECT EXISTS(
           SELECT 1 FROM Identity.AccountRoles ar
           JOIN Identity.RolePermissions rp ON ar.RoleId = rp.RoleId
           WHERE ar.AccountId = @userId AND rp.Permission = 'Attendance.Record.CheckIn'
       )
  → if false: 403 Forbidden (stop)
  → if true: store PermissionDescriptor in HttpContext.Items
```

### Step 2 — Endpoint Handler

```csharp
// AttendanceEndpoints.cs
group.MapPost("/check-in", async (
    CheckInRequest request,
    ISender sender,
    CancellationToken ct) =>
{
    var command = new CheckInCommand(request.CheckInTimeUtc, request.Notes);
    var result = await sender.Send(command, ct);
    return result.ToHttpResult(id => Results.Created($"/api/attendance/records/{id}", id));
});
```

### Step 3 — MediatR Pipeline

```
ISender.Send(new CheckInCommand(...))
   │
   ▼ LoggingBehavior
   │  → _logger.LogInformation("Executing CheckInCommand...")
   │  → Stopwatch.Start()
   │
   ▼ AuditBehavior
   │  → CheckInCommand does NOT implement IAuditableCommand → no-op, pass through
   │
   ▼ ValidationBehavior
   │  → Discover: CheckInCommandValidator : AbstractValidator<CheckInCommand>
   │  → Run validator (e.g., validate CheckInTimeUtc if provided is not in future)
   │  → If errors: return Result.Failure(new ValidationError("...", details))
   │     → endpoint: return 400 Bad Request { code, message, details }
   │
   ▼ CheckInCommandHandler.Handle()
   │  1. Get PermissionDescriptor from HttpContext.Items["CurrentPermission"]
   │  2. IDataScopeService.GetScopeRuleAsync(userId, AttendancePermissions.Record.CheckIn, ct)
   │     → DataScopeService (Personnel module):
   │       a. Load user's ScopeGrant for "Attendance.Record.CheckIn" from DB
   │       b. Level = Self (only allowed scope for CheckIn)
   │       c. Load employeeId linked to this userId from Personnel.Employees
   │       d. return DataScopeRule.Self(employeeId)
   │     → If ScopeRule.SelfEmployeeId is null: user has no employee profile
   │       return Result.Failure(AttendanceErrors.EmployeeNotResolved())  → 400
   │  3. checkInTime = request.CheckInTimeUtc ?? DateTime.UtcNow
   │  4. IPersonnelQuery.GetEmployeeCompanyIdsAsync(employeeId, ct)
   │     → SELECT CompanyId FROM Personnel.EmployeeAssignments
   │       WHERE EmployeeId = @employeeId AND IsActive = 1
   │     → companyId = result.FirstOrDefault()  (null if unassigned)
   │  5. var record = AttendanceRecord.Create(tenantId, employeeId, companyId, checkInTime, notes)
   │     → internally: record.AddDomainEvent(new AttendanceCheckedInDomainEvent(...))
   │  6. _repository.Add(record)
   │     → _dbContext.AttendanceRecords.Add(record)  (not saved yet)
   │  7. return Result.Success(record.Id)
   │
   ▼ UnitOfWorkBehavior
   │  → command.ModuleName == "Attendance"
   │  → resolve IModuleUnitOfWork where uow.ModuleName == "Attendance"
   │     (== AttendanceDbContext, registered as IModuleUnitOfWork in DI)
   │  → await uow.CommitAsync(ct)
   │     ↓
   │     ModuleDbContext.CommitAsync():
   │       a. Collect domain events: [AttendanceCheckedInDomainEvent]
   │       b. MediatR.Publish(AttendanceCheckedInDomainEvent):
   │            AttendanceCheckedInDomainEventHandler.Handle():
   │              → _dbContext.AddIntegrationEvent(new AttendanceCheckedInIntegrationEvent(...))
   │              → Serialize → new OutboxMessage { TypeName, Payload (JSON), CreatedAtUtc }
   │              → _dbContext.OutboxMessages.Add(outboxMsg)
   │       c. AuditInterceptor.SavingChangesAsync():
   │            → set CreatedAtUtc, CreatedById on new AttendanceRecord
   │       d. DbContext.SaveChangesAsync()
   │            → INSERT INTO Attendance.AttendanceRecords (...)
   │            → INSERT INTO Attendance.OutboxMessages (...)
   │            → both in ONE SQL transaction
   │            → if UNIQUE constraint violation (EmployeeId, Date):
   │                DbUpdateException thrown → catch in endpoint handler
   │                → return 409 Conflict { code: "Attendance.AlreadyCheckedIn", message: "..." }
   │
   ▼ LoggingBehavior (after handler)
   │  → _logger.LogInformation("CheckInCommand completed in {ElapsedMs}ms")
   │
   ▼ Return Result.Success(record.Id)

→ endpoint: result.ToHttpResult(id => Results.Created(..., id))
→ HTTP 201 Created  { body: recordId (Guid) }
```

---

## 3. Query Flow — GetMyAttendance (Detailed Example)

```
GET /api/attendance/me?fromDate=2026-01-01&toDate=2026-01-31&pageNumber=1&pageSize=20
Headers: Authorization: Bearer <jwt>
```

### Key Difference from Commands

Queries do **not** use `UnitOfWorkBehavior` — they only read data and never commit. They also typically return data directly (not wrapped in `Result<T>`).

### Execution

```
RoutePermissionMiddleware:
  → "GET /api/attendance/me" → Permission "Attendance.Record.View"
  → HasPermissionAsync(userId, View) → 403 if no

Endpoint Handler:
  var query = new GetMyAttendanceQuery(fromDate, toDate, pageNumber, pageSize);
  var result = await sender.Send(query, ct);
  return Results.Ok(result);

MediatR Pipeline:
  │
  ▼ LoggingBehavior → log + timer
  ▼ AuditBehavior   → no-op (query, not IAuditableCommand)
  ▼ ValidationBehavior → validate date range (if provided)
  ▼ GetMyAttendanceQueryHandler.Handle():
  │
  │  1. IDataScopeService.GetScopeRuleAsync(userId, AttendancePermissions.Record.View, ct)
  │     → DataScopeRule{Level=Self, SelfEmployeeId=employeeId}
  │     (even if user has Company-scope for View, GetMyAttendance always forces Self)
  │
  │  2. IAttendanceQueryContext.AttendanceRecords  (no-tracking IQueryable)
  │       .Where(r => r.EmployeeId == selfEmployeeId)          // scope: Self
  │       .Where(r => r.Date >= fromDate)                       // date filter
  │       .Where(r => r.Date <= toDate)
  │       [+ automatic global filter: r.TenantId == currentTenantId]
  │       [+ automatic global filter: !r.IsDeleted (SoftDeletable)]
  │       .OrderByDescending(r => r.Date)
  │       .Skip((pageNumber - 1) * pageSize)
  │       .Take(pageSize)
  │
  │  3. Execute: Count query + Data query (2 DB round-trips)
  │     → return new PagedResult<AttendanceSummaryDto>(items, totalCount, pageNumber, pageSize)
  │
  ▼ LoggingBehavior → log elapsed
  ▼ Return PagedResult<AttendanceSummaryDto>

→ HTTP 200 OK { items: [...], totalCount: 15, pageNumber: 1, pageSize: 20 }
```

---

## 4. Domain Event → Integration Event → Outbox Flow

This flow ensures that cross-module notifications are delivered reliably even if the process crashes.

```
┌─────────────────────────────────────────────────────────────────────┐
│  SYNCHRONOUS (inside HTTP request transaction)                      │
│                                                                     │
│  AttendanceRecord.Create(...)                                       │
│    → entity.AddDomainEvent(new AttendanceCheckedInDomainEvent(...)) │
│                                                                     │
│  UnitOfWorkBehavior → CommitAsync():                                │
│    1. Collect domain events from all tracked entities               │
│    2. MediatR.Publish(AttendanceCheckedInDomainEvent):              │
│         AttendanceCheckedInDomainEventHandler.Handle():             │
│           → new AttendanceCheckedInIntegrationEvent { ... }         │
│           → Serialize to JSON                                        │
│           → new OutboxMessage {                                     │
│               TypeName = "AttendanceCheckedInIntegrationEvent",     │
│               Payload  = "{...json...}",                            │
│               CreatedAtUtc = UtcNow,                                │
│               ProcessedAtUtc = null                                 │
│             }                                                       │
│           → _dbContext.OutboxMessages.Add(outboxMsg)                │
│    3. DbContext.SaveChangesAsync()                                  │
│         INSERT INTO Attendance.AttendanceRecords (...)              │
│         INSERT INTO Attendance.OutboxMessages (...)                 │
│         ← COMMIT (both in one SQL transaction)                      │
└─────────────────────────────────────────────────────────────────────┘
                              │
                              │ (OutboxMessage is now durable in DB)
                              │
┌─────────────────────────────────────────────────────────────────────┐
│  ASYNCHRONOUS (AttendanceOutboxProcessor background service)        │
│                                                                     │
│  Every ~5 seconds:                                                  │
│    1. SELECT TOP 20 FROM Attendance.OutboxMessages                  │
│       WHERE ProcessedAtUtc IS NULL                                  │
│       ORDER BY CreatedAtUtc ASC                                     │
│                                                                     │
│    2. For each OutboxMessage:                                       │
│       a. type = Type.GetType(message.TypeName)                      │
│       b. @event = JsonSerializer.Deserialize(message.Payload, type) │
│       c. handler = sp.GetRequiredService<                           │
│              IIntegrationEventHandler<AttendanceCheckedInIntegrationEvent>>() │
│       d. await handler.HandleAsync(@event)                          │
│          (e.g., notify another module, update a read model, etc.)  │
│       e. UPDATE Attendance.OutboxMessages                           │
│          SET ProcessedAtUtc = UtcNow                                │
│          WHERE Id = @id                                             │
│                                                                     │
│    3. On handler failure: leave ProcessedAtUtc = null               │
│       → retry on next poll cycle (at-least-once delivery)           │
└─────────────────────────────────────────────────────────────────────┘
```

**Why this matters**: Even if the server crashes after committing the main transaction but before any in-process notification, the `OutboxMessage` remains in the database and will be picked up on the next startup.

---

## 5. Multi-Tenancy Flow

### Setup (at startup)

```csharp
// Each module DbContext inherits ModuleDbContext:
public class AttendanceDbContext : ModuleDbContext
{
    private readonly IExecutionContext _executionContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);  // applies global filters

        // base.OnModelCreating iterates all entity types:
        // For ITenantEntity → HasQueryFilter(e => e.TenantId == _executionContext.TenantId)
        // For ISoftDeletable → HasQueryFilter(e => !e.IsDeleted)
    }
}
```

### Runtime (per request)

```
JWT token contains claim: "tenantId" = "3fa85f64-..."

UseAuthentication → parse JWT → ClaimsPrincipal{tenantId claim}

IExecutionContext (scoped per request):
  → _httpContextAccessor.HttpContext.User.FindFirst("tenantId")
  → returns Guid "3fa85f64-..."

All EF Core queries on AttendanceDbContext:
  SELECT * FROM Attendance.AttendanceRecords
  -- EF Core automatically adds:
  WHERE TenantId = '3fa85f64-...'
    AND IsDeleted = 0
  -- (from global query filters)

Handler code:
  _context.AttendanceRecords.Where(r => r.Date > fromDate)
  -- No need to filter by TenantId — it's always implicit
```

**Result**: Cross-tenant data access is architecturally impossible without explicitly bypassing EF Core query filters (which is disallowed in the codebase).

---

## 6. Two-Layer Security Flow

### Layer 1 — Route Permission (Binary: Can this user perform this action?)

```
HTTP Request: POST /api/attendance/records (ManualRecord)

RoutePermissionMiddleware:
  1. Load all RouteSecurityMap.xml entries (from all modules, cached at startup)
  2. Match: Method="POST", Path="/api/attendance/records"
     → Permission="Attendance.Record.ManualRecord"
  3. IPermissionService.HasPermissionAsync(userId, "Attendance.Record.ManualRecord"):
     → SELECT COUNT(*) FROM Identity.AccountRoles ar
       JOIN Identity.RolePermissions rp ON ar.RoleId = rp.RoleId
       WHERE ar.AccountId = @userId
         AND rp.Module = 'Attendance' AND rp.Entity = 'Record' AND rp.Action = 'ManualRecord'
  4. Count = 0 → 403 Forbidden { "You don't have permission to access this resource." }
  5. Count > 0 → store PermissionDescriptor in HttpContext.Items → continue
```

### Layer 2 — Data Scope (What subset of data can they access?)

```
Inside RecordManualAttendanceCommandHandler (or any scoped query handler):

IDataScopeService.GetScopeRuleAsync(userId, ManualRecord, ct):
  → DataScopeService (Personnel module):
    1. SELECT ScopeGrant FROM Identity.RoleScopeGrants rsg
       JOIN Identity.AccountRoles ar ON ar.RoleId = rsg.RoleId
       WHERE ar.AccountId = @userId
         AND rsg.Permission = 'Attendance.Record.ManualRecord'
       ORDER BY rsg.ScopeLevel DESC  -- take highest (most permissive) scope
    2. scopeLevel = Company (5)
    3. Resolve dimension IDs for Company scope:
       → SELECT DISTINCT CompanyId FROM Personnel.EmployeeAssignments
         WHERE EmployeeId = @selfEmployeeId
       → companyIds = [companyId1, companyId2]
    4. return DataScopeRule.Company([companyId1, companyId2])

Handler applies scope to the operation:
  → If command.EmployeeId's company is NOT in rule.DimensionIds:
     return Result.Failure(new ForbiddenError("..."))  → 403

Query handlers apply scope as WHERE clause:
  → DataScopeLevel.Self:        WHERE EmployeeId = @selfId
  → DataScopeLevel.EmployeeSet: WHERE EmployeeId IN (@id1, @id2, ...)
  → DataScopeLevel.Company:     WHERE CompanyId IN (@companyId1, @companyId2)
  → DataScopeLevel.Global:      (no filter — see all)
  → DataScopeLevel.None:        WHERE 1=0  (empty result set)
```

### Summary

```
Request
  │
  ├─ RoutePermissionMiddleware ──────────────────── "Can they do THIS ACTION?"
  │    PermissionDescriptor stored in HttpContext      → 403 if NO (route rejected)
  │
  └─ Handler: IDataScopeService ────────────────── "What DATA can they see?"
       DataScopeRule applied to queries                → empty/forbidden result if None
```

---

## 7. Authentication Flow (Login)

```
┌──────────────────────────────────────────────────────────────────────┐
│  HRM.Web Browser                                                     │
│                                                                      │
│  User submits: POST /Auth/Login { email, password }                  │
└─────────────────────────────┬────────────────────────────────────────┘
                              │ MVC form POST
                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│  AuthController.Login(POST) [HRM.Web]                                │
│                                                                      │
│  var response = await _identityApiClient.LoginAsync(email, password) │
└─────────────────────────────┬────────────────────────────────────────┘
                              │ HTTP POST /api/identity/accounts/login
                              │ (via AuthTokenHandler — no JWT needed for login)
                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│  LoginCommandHandler [HRM.Api / Identity module]                     │
│                                                                      │
│  1. Load Account by email                                            │
│  2. PasswordHasher.Verify(password, account.PasswordHash)           │
│     → if wrong: return Result.Failure(UnauthorizedError)            │
│  3. Check account status (Active required)                           │
│  4. Generate JWT:                                                    │
│       claims: { sub: userId, tenantId, role, email, fullName }       │
│       expiry: DateTime.UtcNow + JwtSettings.ExpiryMinutes           │
│       signed: HMAC-SHA256 with JwtSettings.SecretKey                │
│  5. Generate RefreshToken:                                           │
│       tokenValue = CryptoRandom.Generate()                           │
│       hash = SHA256(tokenValue)                                      │
│       store: new RefreshToken { Hash, UserId, ExpiresAt, DeviceInfo }│
│  6. return Result.Success({ AccessToken, RefreshToken, ExpiresAt })  │
└─────────────────────────────┬────────────────────────────────────────┘
                              │ HTTP 200 OK { accessToken, refreshToken, expiresAt }
                              ▼
┌──────────────────────────────────────────────────────────────────────┐
│  AuthController.Login(POST) [HRM.Web] — continues                   │
│                                                                      │
│  1. Parse JWT claims → create ClaimsPrincipal                        │
│  2. HttpContext.SignInAsync(CookieAuthenticationDefaults, principal) │
│     → sets encrypted cookie "HRM.Auth"                              │
│  3. Response.Cookies.Append("HRM.AccessToken", accessToken, HttpOnly)│
│  4. Response.Cookies.Append("HRM.RefreshToken", refreshToken, HttpOnly)│
│  5. Redirect to Home/Index                                           │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 8. Token Refresh Flow (Proactive)

`AuthTokenHandler` (a `DelegatingHandler`) is injected into all typed HTTP clients:

```
Every API call from HRM.Web goes through AuthTokenHandler.SendAsync():

  1. Read "HRM.AccessToken" cookie → accessToken
  2. Decode JWT payload (no signature check) → ExpiresAt
  3. if (ExpiresAt - UtcNow) < 2 minutes:
       // Proactive refresh before token actually expires
       POST /api/identity/accounts/refresh
         { refreshToken: cookieValue("HRM.RefreshToken") }
       → RefreshTokenCommandHandler [HRM.Api]:
           a. Hash incoming refreshToken value → look up RefreshToken in DB
           b. Verify not expired, not revoked
           c. Issue new JWT + new RefreshToken
           d. Mark old RefreshToken as revoked
           e. Return { newAccessToken, newRefreshToken, newExpiresAt }
       → Update cookies: HRM.AccessToken, HRM.RefreshToken
       → Use new accessToken for this request
  4. Add header: "Authorization: Bearer <accessToken>"
  5. Execute the actual API call
```

---

## 9. HRM.Web → HRM.Api Client Flow

```
User clicks "Check In" button in browser
      │
      ▼
AttendanceController.CheckIn(GET)
  → return View(new CheckInFormModel())  →  render CheckIn.cshtml

User submits form
      │
      ▼
AttendanceController.CheckIn(POST)
  → IAttendanceApiClient.CheckInAsync(new CheckInFormModel { Notes = "..." })

AttendanceApiClient.CheckInAsync():
  → var request = new CheckInRequest(CheckInTimeUtc: null, Notes: formModel.Notes)
  → await _httpClient.PostAsJsonAsync("/api/attendance/check-in", request, ct)
      │
      │ (AuthTokenHandler middleware intercepts here):
      │   → attach Bearer token → POST to HRM.Api
      │
  → response = await response.EnsureSuccessStatusCode() / ReadAsAsync()

On 201 Created:
  → TempData["SuccessMessage"] = "Checked in successfully!"
  → return RedirectToAction("MyAttendance")

On 409 Conflict:
  → TempData["ErrorMessage"] = "You are already checked in today."
  → return RedirectToAction("MyAttendance")

On 403 Forbidden:
  → TempData["ErrorMessage"] = "You don't have permission to check in."
  → return RedirectToAction("MyAttendance")

AttendanceController.MyAttendance(GET)
  → IAttendanceApiClient.GetMyAttendanceAsync(fromDate, toDate, pageNumber, pageSize)
  → return View(new MyAttendanceViewModel { Records = pagedResult, ... })
      │
      ▼
Browser renders MyAttendance.cshtml with Bootstrap 5.3 table
```

---

## 10. Module Registration Order & Dependency Chain

```
HRM.Api startup (Program.cs → ModuleExtensions.AddModules):

① Identity Module
   AddIdentityApplication()     → MediatR handlers, FluentValidation validators
   AddIdentityModule(config)    → IdentityDbContext, IPermissionService ← registered here
                                   JWT auth config, PasswordHasher

② Organization Module
   AddOrganizationApplication() → MediatR handlers, validators
   AddOrganizationModule(config)→ OrganizationDbContext, IOrganizationQuery ← registered

③ Personnel Module
   AddPersonnelApplication()    → MediatR handlers, validators
   AddPersonnelModule(config)   → PersonnelDbContext
                                   IDataScopeService ← registered here (needs Org + Identity)
                                   IPersonnelQuery ← registered here

④ Attendance Module
   AddAttendanceApplication()   → MediatR handlers, validators
   AddAttendanceModule(config)  → AttendanceDbContext
                                   AttendanceOutboxProcessor
                                   ⚠ Does NOT register IDataScopeService (already by Personnel)
                                   ⚠ Does NOT register IPersonnelQuery (already by Personnel)

DI resolution order matters:
  AttendanceModule depends on → IDataScopeService (from Personnel)
  AttendanceModule depends on → IPersonnelQuery (from Personnel)
  PersonnelModule depends on  → IOrganizationQuery (from Organization)
  PersonnelModule depends on  → IPermissionService (from Identity)
```

---

## 11. MediatR Pipeline Behavior Execution

All 5 behaviors are registered in `ApplicationServiceExtensions.cs` in order:

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
// Handler runs here (no explicit registration)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
```

MediatR executes behaviors in a nested "Russian doll" pattern:

```
LoggingBehavior.Handle()
  → AuditBehavior.Handle()
       → ValidationBehavior.Handle()
            → UnitOfWorkBehavior.Handle()
                 → actual Handler.Handle()
                 ← returns Result<T>
            ← if IsSuccess: CommitAsync()  [UnitOfWorkBehavior]
            ← pass through result
       ← pass through result  [ValidationBehavior]
  ← log elapsed ms  [LoggingBehavior]
← return result
```

### Short-Circuit Paths

```
ValidationBehavior: if validators find errors
  → return Result.Failure(ValidationError) immediately
  → UnitOfWorkBehavior never runs → CommitAsync never called → no DB write

UnitOfWorkBehavior: if handler returns Result.Failure(...)
  → skip CommitAsync → no DB write
  → return Result.Failure to endpoint handler
  → endpoint: result.ToHttpResult() → appropriate 4xx/5xx response

DbUpdateException (unique index violation during CommitAsync):
  → propagates up through UnitOfWorkBehavior
  → caught in Minimal API endpoint handler (AttendanceEndpoints.cs has try/catch)
  → mapped to 409 Conflict
```

---

## Flow Summary Diagram

```
Browser
  │
  ▼ HTTP
HRM.Web (MVC)
  │ Typed HttpClient
  │ + AuthTokenHandler (JWT from cookie)
  ▼ HTTP
HRM.Api
  │ Middleware:
  │  → JWT validation → Route permission check (403 or pass)
  │
  ▼ Minimal API Endpoint
     → build Command/Query
     │
     ▼ MediatR Pipeline
        LoggingBehavior → AuditBehavior → ValidationBehavior → Handler → UnitOfWorkBehavior
                                                                 │              │
                                          DataScopeService ──────┤              │
                                          PersonnelQuery ─────────┤              │
                                          Repository ─────────────┤              │
                                          DomainEvents ───────────┘              │
                                                                                 │
                                          DbContext.SaveChangesAsync() ──────────┘
                                            ├── INSERT business records
                                            └── INSERT OutboxMessages (one TX)
                                                       │
                                                       ▼ (async, background)
                                               OutboxProcessor
                                                 → IIntegrationEventHandler
                                                 → notify other modules / services
```
