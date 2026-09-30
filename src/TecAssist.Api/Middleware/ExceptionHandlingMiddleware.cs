using Microsoft.AspNetCore.Mvc;
using TecAssist.Application.Common;

namespace TecAssist.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppValidationException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, exception.Message);
        }
        catch (ResourceNotFoundException)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, "The requested resource was not found.");
        }
        catch (UnauthorizedAccessException)
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Authentication is required to access this resource.");
        }
        catch (ChatGenerationException exception)
        {
            logger.LogError(exception, "AI generation failed while handling {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status502BadGateway, "The AI service failed to generate a response. Please try again.");
        }
        catch (ServiceUnavailableException exception)
        {
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, exception.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while handling {Path}", context.Request.Path);
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = statusCode, Title = title },
            options: null,
            contentType: "application/problem+json");
    }
}
