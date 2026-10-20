using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Serilog.Context;

namespace J_A_Jewelry.Middleware;

/// <summary>
/// Correlation + log attribution, FIRST in the pipeline so the traceId wraps
/// error paths too: resolves the W3C traceId and (a) echoes it back to the
/// client as an X-TraceId response header (the Flutter app attaches it to
/// its own logs, so one issue greps across both repos), (b) pushes it into
/// the Serilog LogContext so every log line inside the request carries it,
/// and (c) when a JWT Authorization header is present, resolves the user
/// (AuthenticateAsync — UseAuthentication runs later in the pipeline) and
/// pushes UserId/UserName so the log trails are attributable per person.
/// </summary>
public class TraceMiddleware
{
    private const string HeaderName = "X-TraceId";
    private readonly RequestDelegate _next;

    public TraceMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        context.Response.Headers[HeaderName] = traceId;

        ClaimsPrincipal? principal = null;
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            var authenticate = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
            if (authenticate.Succeeded) principal = authenticate.Principal;
        }

        // using var with a nullable target: a null IDisposable is legal (no
        // dispose happens) — gives the "only push user props when a token
        // resolved" behavior without a fake disposable.
        using var tracePush = LogContext.PushProperty("TraceId", traceId);
        using var userPush = principal != null
            ? LogContext.PushProperty("UserId",
                principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
                principal.FindFirstValue(ClaimTypes.NameIdentifier))
            : null;
        using var namePush = principal != null
            ? LogContext.PushProperty("UserName",
                principal.FindFirstValue(JwtRegisteredClaimNames.Name) ?? "")
            : null;

        await _next(context);
    }
}