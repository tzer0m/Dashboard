using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Dashboard.Auth;

/// <summary>
/// Validates the X-Api-Key header on incoming requests against the configured dashboard status API key.
/// </summary>
public sealed partial class ApiKeyAuthFilter(IConfiguration configuration, ILogger<ApiKeyAuthFilter> logger) : IAsyncActionFilter
{
    /// <summary>
    /// The name of the header that should contain the API key.
    /// </summary>
    private const string ApiKeyHeaderName = "X-Api-Key";

    /// <summary>
    /// The expected API key value, read from configuration.
    /// </summary>
    private readonly IConfiguration Configuration = configuration;
    private readonly ILogger<ApiKeyAuthFilter> Logger = logger;

    /// <summary>
    /// Short-circuits the request with 401 Unauthorized if the API key header is missing or does not match the configured key.
    /// </summary>
    /// <param name="context">The action executing context for the current request.</param>
    /// <param name="next">The delegate to invoke the next filter or action.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Check if the X-Api-Key header is present
        string remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out StringValues providedKey))
        {
            LogMissingApiKey(remoteIp);
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if the provided API key matches the expected value from configuration
        string? expectedKey = Configuration["Api:DashboardStatusKey"];
        if (string.IsNullOrEmpty(expectedKey) || !string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal))
        {
            LogInvalidApiKey(remoteIp);
            context.Result = new UnauthorizedResult();
            return;
        }

        // If the API key is valid, proceed to the next filter or action
        await next();
    }

    /// <summary>
    /// Logs a warning when the API key header is missing from the request.
    /// </summary>
    /// <param name="remoteIp">The IP address of the client making the request.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Status API request from {RemoteIp} was missing the X-Api-Key header.")]
    private partial void LogMissingApiKey(string remoteIp);

    /// <summary>
    /// Logs a warning when the provided API key does not match the expected value.
    /// </summary>
    /// <param name="remoteIp">The IP address of the client making the request.</param>
    [LoggerMessage(Level = LogLevel.Warning, Message = "Status API request from {RemoteIp} used an invalid API key.")]
    private partial void LogInvalidApiKey(string remoteIp);
}