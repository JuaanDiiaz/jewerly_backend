using System.Text.Json;
using Serilog;

namespace J_A_Jewelry.Middleware;

/// <summary>
/// Catches unhandled exceptions on API routes, logs them at Error with the
/// request's traceId (pushed by TraceMiddleware), and answers with a small
/// JSON body that carries the traceId — so a support session can grep the log
/// store with it. Stack traces never leave the server. Kept separate from the
/// non-Development UseExceptionHandler("/Error") block, which remains for
/// Razor pages paths.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var traceId = context.Response.Headers.TryGetValue("X-TraceId", out var header)
                ? header.ToString()
                : context.TraceIdentifier;

            Log.Error(exception, "Unhandled exception {Method} {Path} TraceId={TraceId}",
                context.Request.Method, context.Request.Path, traceId);

            if (context.Response.HasStarted) throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = 500,
                title = "Internal server error",
                traceId
            }, JsonOptions));
        }
    }
}