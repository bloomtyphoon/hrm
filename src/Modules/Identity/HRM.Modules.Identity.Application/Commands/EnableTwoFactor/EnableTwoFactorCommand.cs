using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.EnableTwoFactor;

/// <summary>
/// Command to enable two-factor authentication for an account.
/// Generates a TOTP secret key and returns it for the user to configure their authenticator app.
/// </summary>
public sealed record EnableTwoFactorCommand(
    Guid AccountId
) : IModuleCommand<EnableTwoFactorResponse>
{
    public string ModuleName => "Identity";
}

/// <summary>
/// Response containing the generated TOTP secret key for authenticator app setup.
/// </summary>
public sealed record EnableTwoFactorResponse(
    string SecretKey
);
