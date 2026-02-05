using System.Security.Claims;
using HRM.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HRM.Web.Services;

/// <summary>
/// HTTP message handler that automatically attaches Bearer token to outgoing API requests.
/// If the access token is expired or near expiry, it uses the refresh token to obtain
/// a new access token before sending the request — as long as the refresh token is still valid.
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthTokenHandler> _logger;

    /// <summary>
    /// Buffer time before actual expiry to trigger a proactive refresh.
    /// </summary>
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Prevents concurrent refresh attempts within the same request pipeline.
    /// </summary>
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public AuthTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("User is not authenticated, skipping token attachment for: {RequestUri}",
                request.RequestUri);
            return await base.SendAsync(request, cancellationToken);
        }

        // Skip token refresh for the refresh endpoint itself to avoid infinite recursion
        var isRefreshRequest = request.RequestUri?.AbsolutePath
            .EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase) == true;

        var accessToken = httpContext.User.FindFirst("AccessToken")?.Value;

        if (!isRefreshRequest && !string.IsNullOrWhiteSpace(accessToken))
        {
            // Check if access token needs refreshing
            var accessTokenExpiryStr = httpContext.User.FindFirst("AccessTokenExpiry")?.Value;

            if (DateTime.TryParse(accessTokenExpiryStr, out var accessTokenExpiry)
                && accessTokenExpiry.ToUniversalTime() <= DateTime.UtcNow.Add(RefreshBuffer))
            {
                // Token is expired or about to expire — try to refresh
                accessToken = await TryRefreshTokenAsync(httpContext, cancellationToken);
            }
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            _logger.LogDebug("Added Bearer token to request: {RequestUri}", request.RequestUri);
        }
        else
        {
            _logger.LogWarning("User is authenticated but no valid AccessToken available");
        }

        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Attempt to refresh the access token using the stored refresh token.
    /// Updates the authentication cookie with new claims on success.
    /// Returns the new access token, or null if refresh failed.
    /// </summary>
    private async Task<string?> TryRefreshTokenAsync(
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var refreshToken = httpContext.User.FindFirst("RefreshToken")?.Value;

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Cannot refresh: RefreshToken claim is missing");
            return null;
        }

        // Check if refresh token itself has expired
        var refreshTokenExpiryStr = httpContext.User.FindFirst("RefreshTokenExpiry")?.Value;
        if (DateTime.TryParse(refreshTokenExpiryStr, out var refreshTokenExpiry)
            && refreshTokenExpiry.ToUniversalTime() <= DateTime.UtcNow)
        {
            _logger.LogWarning("Cannot refresh: RefreshToken has expired");
            return null;
        }

        // Prevent concurrent refresh attempts
        if (!await RefreshLock.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken))
        {
            _logger.LogWarning("Timeout waiting for refresh lock — using existing token");
            return httpContext.User.FindFirst("AccessToken")?.Value;
        }

        try
        {
            // Double-check after acquiring lock: another thread may have already refreshed
            var currentExpiry = httpContext.User.FindFirst("AccessTokenExpiry")?.Value;
            if (DateTime.TryParse(currentExpiry, out var currentExpiryDt)
                && currentExpiryDt.ToUniversalTime() > DateTime.UtcNow.Add(RefreshBuffer))
            {
                _logger.LogDebug("Token was already refreshed by another request");
                return httpContext.User.FindFirst("AccessToken")?.Value;
            }

            _logger.LogInformation("Access token expired or near expiry, refreshing...");

            // Call the refresh endpoint directly via HttpClient to avoid recursion
            var httpClientFactory = httpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("HRM.Api");

            var payload = new { refreshToken };
            var response = await httpClient.PostAsJsonAsync(
                "/api/identity/auth/refresh",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token refresh failed with status {StatusCode}", (int)response.StatusCode);
                return null;
            }

            var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var tokenResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions, cancellationToken);

            if (tokenResponse == null)
            {
                _logger.LogWarning("Token refresh returned null response");
                return null;
            }

            // Build new claims with updated tokens
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, tokenResponse.User.Id.ToString()),
                new(ClaimTypes.Name, tokenResponse.User.Username),
                new(ClaimTypes.Email, tokenResponse.User.Email),
                new("FullName", tokenResponse.User.FullName),
                new("AccessToken", tokenResponse.AccessToken),
                new("RefreshToken", tokenResponse.RefreshToken),
                new("AccessTokenExpiry", tokenResponse.AccessTokenExpiry.ToString("O")),
                new("RefreshTokenExpiry", tokenResponse.RefreshTokenExpiry.ToString("O"))
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            // Preserve the existing authentication properties (IsPersistent, etc.)
            var authResult = await httpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = authResult.Properties ?? new AuthenticationProperties
            {
                IsPersistent = true,
                AllowRefresh = true
            };

            // Re-issue the authentication cookie with new claims
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("Access token refreshed successfully for user {Username}",
                tokenResponse.User.Username);

            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh access token");
            return null;
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}
