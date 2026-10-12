using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using web.Services.AiGateway;
using web.Services.Ollama;

namespace web.Infrastructure.ErrorHandling;

/// <summary>
/// The only place unhandled exceptions are logged. Requests from scripts (fetch/AJAX) get a
/// ProblemDetails body; browser page requests are left to the error page (/Home/Error), which
/// UseExceptionHandler re-executes when this handler returns false.
/// </summary>
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, detail) = exception switch
        {
            AiGatewayException aiGatewayException => ((int)aiGatewayException.StatusCode, aiGatewayException.Message),
            OllamaException ollamaException => ((int)ollamaException.StatusCode, ollamaException.Message),
            _ => (StatusCodes.Status500InternalServerError, environment.IsDevelopment() ? exception.Message : null)
        };

        logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        if (IsPageRequest(httpContext.Request))
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Detail = detail
            }
        });
    }

    private static bool IsPageRequest(HttpRequest request) =>
        HttpMethods.IsGet(request.Method)
        && request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)
        && request.Headers.XRequestedWith != "XMLHttpRequest";
}
