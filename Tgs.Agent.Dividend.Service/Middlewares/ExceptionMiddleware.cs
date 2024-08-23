using System.Net;
using System.Text.Json;

namespace Tgs.PlayerService.Middlewares;

public class ExceptionMiddleware(IConfiguration configuration) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureMessage($"{configuration.GetValue<string>("Sentry:Tag")}: {ex.Message} \n {ex.StackTrace}", scope =>
            {
                scope.Level = SentryLevel.Error;
            });
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                StatusCode = context.Response.StatusCode,
                Message = ex.Message
            };

            await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse);
        }
    }
}
