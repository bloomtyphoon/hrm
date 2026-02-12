namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Response DTO for enabling two-factor authentication.
/// Contains the generated TOTP secret key for authenticator app setup.
/// </summary>
public sealed record EnableTwoFactorResponse(
    string SecretKey
);
