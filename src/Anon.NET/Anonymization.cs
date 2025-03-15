using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Anon.NET;

public class Anonymization
{
    private readonly RequestDelegate _next;

    public Anonymization(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("X-Transaction-Id", Guid.NewGuid().ToString());

        await _next(context);
    }

}
public static class AnonimizationExtension
{
    public static IApplicationBuilder UseAnonymization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<Anonymization>();
    }
}
