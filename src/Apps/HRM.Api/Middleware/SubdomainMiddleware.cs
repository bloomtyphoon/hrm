namespace HRM.Api.Middleware;

/// <summary>
/// Extracts the subdomain label from the request Host header and stores it in
/// HttpContext.Items["TenantSubdomain"] for downstream use.
///
/// Example: request Host = "acme.hrm.example.com"
///   → Items["TenantSubdomain"] = "acme"
///
/// Single-label hosts (localhost, plain IP) and the www label are ignored.
/// </summary>
public class SubdomainMiddleware
{
    private readonly RequestDelegate _next;

    public SubdomainMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host; // e.g. "acme.hrm.example.com"

        var subdomain = ExtractSubdomain(host);
        if (subdomain is not null)
        {
            context.Items["TenantSubdomain"] = subdomain;
        }

        return _next(context);
    }

    private static string? ExtractSubdomain(string host)
    {
        // Strip port if present
        var dotIndex = host.IndexOf('.');
        if (dotIndex <= 0) return null; // single label (localhost, IP, etc.)

        var label = host[..dotIndex].ToLowerInvariant();

        // Ignore common non-tenant labels
        if (label is "www" or "api" or "app") return null;

        return label;
    }
}

public static class SubdomainMiddlewareExtensions
{
    public static IApplicationBuilder UseSubdomainResolution(this IApplicationBuilder app)
        => app.UseMiddleware<SubdomainMiddleware>();
}
