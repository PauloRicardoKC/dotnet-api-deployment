namespace MinimalApi.Api.Middlewares;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        logger.LogInformation("Handling {Method} {Path}", context.Request.Method, context.Request.Path);
        await next(context);
        logger.LogInformation("Finished {Method} {Path} with status {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
    }
}
