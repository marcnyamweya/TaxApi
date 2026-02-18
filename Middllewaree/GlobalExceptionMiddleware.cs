using System.Net;
using System.Text.Json;
using TaxApi.Models;
using TaxApi.Services;

namespace TaxApi.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService audit)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            // Best-effort audit log (don't throw if audit itself fails)
            try
            {
                await audit.LogAsync(
                    AuditEventType.SystemError,
                    "UnhandledException",
                    performedBy: "System",
                    details: $"{ex.GetType().Name}: {ex.Message} | Path: {context.Request.Path}");
            }
            catch { /* swallow */ }

            context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new
            {
                error     = "An unexpected error occurred.",
                requestId = context.TraceIdentifier
            });

            await context.Response.WriteAsync(payload);
        }
    }
}
