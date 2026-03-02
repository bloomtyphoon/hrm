using HRM.BuildingBlocks.Application.Abstractions.EventBus;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Infrastructure.Persistence;
using HRM.Modules.Personnel.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.IntegrationEventHandlers;

/// <summary>
/// Handles EmployeeCreatedIntegrationEvent from Personnel module.
/// Auto-creates an employee Account and EmployeeProfile in Identity module
/// when a new employee is created in Personnel.
///
/// Idempotency: Uses Inbox pattern to prevent duplicate account creation.
/// </summary>
internal sealed class EmployeeCreatedIntegrationEventHandler
    : IIntegrationEventHandler<EmployeeCreatedIntegrationEvent>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IAccountRepository _accountRepository;
    private readonly IEmployeeProfileRepository _employeeProfileRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<EmployeeCreatedIntegrationEventHandler> _logger;

    public EmployeeCreatedIntegrationEventHandler(
        IdentityDbContext dbContext,
        IAccountRepository accountRepository,
        IEmployeeProfileRepository employeeProfileRepository,
        IPasswordHasher passwordHasher,
        ILogger<EmployeeCreatedIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _accountRepository = accountRepository;
        _employeeProfileRepository = employeeProfileRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task Handle(
        EmployeeCreatedIntegrationEvent notification,
        CancellationToken cancellationToken)
    {
        var handlerName = nameof(EmployeeCreatedIntegrationEventHandler);

        try
        {
            // Inbox check: skip if already processed
            if (await _dbContext.IsEventProcessedAsync(notification.Id, handlerName, cancellationToken))
            {
                _logger.LogDebug(
                    "Event {EventId} already processed by {HandlerName}, skipping",
                    notification.Id, handlerName);
                return;
            }

            // Idempotency check: skip if account already exists for this email
            if (await _accountRepository.ExistsByEmailAsync(notification.Email, cancellationToken))
            {
                _logger.LogInformation(
                    "Account already exists for email {Email} (EmployeeId={EmployeeId}), skipping",
                    notification.Email, notification.EmployeeId);

                // Still mark as processed to prevent re-processing
                _dbContext.MarkEventAsProcessed(
                    notification.Id,
                    nameof(EmployeeCreatedIntegrationEvent),
                    handlerName);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            // Generate username from employee code (unique, uppercase)
            var username = notification.EmployeeCode.ToLowerInvariant();

            // Check username uniqueness, append suffix if needed
            if (await _accountRepository.ExistsByUsernameAsync(username, cancellationToken))
            {
                username = $"{username}.{notification.EmployeeId.ToString()[..8]}";
            }

            // Generate a temporary password (employee must change on first login)
            var temporaryPassword = GenerateTemporaryPassword();
            var passwordHash = _passwordHasher.HashPassword(temporaryPassword);

            // Create employee account
            var account = Account.CreateEmployeeAccount(
                tenantId: notification.TenantId,
                username: username,
                email: notification.Email,
                passwordHash: passwordHash,
                fullName: $"{notification.FirstName} {notification.LastName}".Trim(),
                phoneNumber: notification.Phone);

            _accountRepository.Add(account);

            // Activate account immediately
            account.Activate();

            // Create employee profile linking account to employee
            var profile = EmployeeProfile.Create(
                tenantId: notification.TenantId,
                accountId: account.Id,
                employeeId: notification.EmployeeId);

            _employeeProfileRepository.Add(profile);

            // Mark event as processed in inbox (same transaction)
            _dbContext.MarkEventAsProcessed(
                notification.Id,
                nameof(EmployeeCreatedIntegrationEvent),
                handlerName);

            // Save everything atomically (Account + EmployeeProfile + InboxMessage + domain events)
            await _dbContext.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Created Account (Id={AccountId}, Username={Username}) and EmployeeProfile for EmployeeId={EmployeeId}",
                account.Id, username, notification.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create Account for EmployeeId={EmployeeId} from {EventType}",
                notification.EmployeeId,
                nameof(EmployeeCreatedIntegrationEvent));
            throw;
        }
    }

    /// <summary>
    /// Generate a temporary password for the new employee account.
    /// The employee must change this on first login.
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        // Generate a random 16-character password with mixed case, digits, and special chars
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        var random = Random.Shared;
        var password = new char[16];

        // Ensure at least one of each category
        password[0] = upper[random.Next(upper.Length)];
        password[1] = lower[random.Next(lower.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];

        // Fill the rest randomly
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = all[random.Next(all.Length)];
        }

        // Shuffle
        random.Shuffle(password);

        return new string(password);
    }
}
