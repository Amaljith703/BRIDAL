using MakeupArt.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MakeupArt.Api.Infrastructure;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await WriteProblem(context, ex.StatusCode, ex.Message, ex.Code);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entries = string.Join(", ", ex.Entries.Select(e => $"{e.Metadata.ClrType.Name}:{e.State}"));
            logger.LogWarning(ex, "Concurrency conflict on {Path}. Entities: {Entities}", context.Request.Path, entries);
            await WriteProblem(context, StatusCodes.Status409Conflict, "This record changed while you were editing it. Reload and try again.", "concurrency_conflict");
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database update failed on {Path}", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status409Conflict, "The change could not be saved. It may conflict with existing data.", "database_conflict");
        }
        catch (BadHttpRequestException ex)
        {
            await WriteProblem(context, ex.StatusCode, "The request could not be read.", "bad_request");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; nothing useful to write.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            var detail = environment.IsDevelopment()
                ? ex.Message
                : "Something went wrong on our side. Please try again, or contact support if it persists.";
            await WriteProblem(context, StatusCodes.Status500InternalServerError, detail, "server_error");
        }
    }

    private static async Task WriteProblem(HttpContext context, int statusCode, string detail, string? code)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                400 => "Bad request",
                401 => "Authentication required",
                403 => "Permission denied",
                404 => "Not found",
                409 => "Conflict",
                422 => "Validation failed",
                429 => "Too many requests",
                _ => "Request failed"
            },
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
