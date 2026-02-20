using System.Security.Claims;
using System.Text.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Authentication controller for login/logout operations
/// </summary>
public class AuthController : Controller
{
    private readonly IIdentityApiClient _identityClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IIdentityApiClient identityClient,
        ILogger<AuthController> logger)
    {
        _identityClient = identityClient;
        _logger = logger;
    }

    /// <summary>
    /// Display login form
    /// GET: /Auth/Login
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already authenticated, redirect to return URL or home
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Process login form submission
    /// POST: /Auth/Login
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginRequest request,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var result = await _identityClient.LoginAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            // Display error message
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid login attempt");

            // Log validation errors if any
            if (result.ValidationErrors != null)
            {
                foreach (var error in result.ValidationErrors)
                {
                    foreach (var errorMessage in error.Value)
                    {
                        ModelState.AddModelError(error.Key, errorMessage);
                    }
                }
            }

            return View(request);
        }

        // Login successful - extract AccountType from JWT payload
        var accountType = ExtractClaimFromJwt(result.Data!.AccessToken, "AccountType") ?? "Employee";

        // Create authentication cookie
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, result.Data.User.Id.ToString()),
            new Claim(ClaimTypes.Name, result.Data.User.Username),
            new Claim(ClaimTypes.Email, result.Data.User.Email),
            new Claim("FullName", result.Data.User.FullName),
            new Claim("AccountType", accountType),
            new Claim("AccessToken", result.Data.AccessToken),
            new Claim("RefreshToken", result.Data.RefreshToken),
            new Claim("AccessTokenExpiry", result.Data.AccessTokenExpiry.ToString("O")),
            new Claim("RefreshTokenExpiry", result.Data.RefreshTokenExpiry.ToString("O"))
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = request.RememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(request.RememberMe ? 30 : 7),
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        _logger.LogInformation("User {Username} logged in successfully", result.Data.User.Username);

        TempData["SuccessMessage"] = $"Welcome back, {result.Data.User.FullName}!";

        return RedirectToLocal(returnUrl);
    }

    /// <summary>
    /// Logout current user
    /// POST: /Auth/Logout
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken = default)
    {
        var username = User.Identity?.Name ?? "Unknown";

        // Call API logout to revoke refresh token
        try
        {
            await _identityClient.LogoutAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call API logout for user {Username}", username);
            // Continue with local logout even if API call fails
        }

        // Clear company context cookie
        HttpContext.Response.Cookies.Delete("HRM.SelectedCompanyId");

        // Sign out from cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        _logger.LogInformation("User {Username} logged out successfully", username);

        TempData["SuccessMessage"] = "You have been logged out successfully.";

        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Redirect to local URL or home page
    /// Prevents open redirect vulnerability
    /// </summary>
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Extract a claim value from a JWT access token without full validation.
    /// Only used to read AccountType at login time.
    /// </summary>
    private static string? ExtractClaimFromJwt(string token, string claimName)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return null;

            var payload = parts[1];
            // Fix base64url padding
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var json = Convert.FromBase64String(payload);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
