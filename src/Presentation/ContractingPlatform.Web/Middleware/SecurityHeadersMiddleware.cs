namespace ContractingPlatform.Web.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Anti-Clickjacking: Disallow framing entirely
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // 2. Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // 3. Referrer Policy
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // 4. Legacy XSS Filter block
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // 5. Content Security Policy (CSP): Whitelist trusted CDNs only
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self' ws: wss:; " +
            "frame-ancestors 'none';";

        // 6. Restrict hardware permissions
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";

        // 7. Prevent server reconnaissance / fingerprinting
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
