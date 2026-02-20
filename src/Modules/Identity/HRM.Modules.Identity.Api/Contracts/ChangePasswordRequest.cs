namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for a user changing their own password.
/// Requires current password verification.
/// </summary>
public sealed record ChangeMyPasswordRequest(
    string CurrentPassword,
    string NewPassword
);

/// <summary>
/// Request DTO for an admin resetting another account's password.
/// Does not require the current password.
/// </summary>
public sealed record ResetAccountPasswordRequest(
    string NewPassword
);
