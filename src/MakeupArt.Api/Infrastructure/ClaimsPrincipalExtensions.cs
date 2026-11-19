using System.Diagnostics;
using System.Security.Claims;

namespace MakeupArt.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal?.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string? GetEmail(this ClaimsPrincipal? principal)
        => principal?.FindFirst(ClaimTypes.Email)?.Value
           ?? principal?.FindFirst("email")?.Value;

    public static bool IsStaff(this ClaimsPrincipal? principal)
        => principal?.IsInRole("Admin") == true
           || principal?.IsInRole("Manager") == true
           || principal?.IsInRole("Support") == true;
}

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            if (!context.Request.Path.StartsWithSegments("/media") && !context.Request.Path.StartsWithSegments("/swagger"))
            {
                logger.LogInformation(
                    "HTTP {Method} {Path}{Query} responded {StatusCode} in {Elapsed} ms (user {User}, ip {Ip})",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    context.User.GetUserId()?.ToString() ?? "anonymous",
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            }
        }
    }
}
