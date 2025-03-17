using Anon.NET.SqlInterception.Interfaces;
using Anon.NET.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Anon.NET.Middleware;

/// <summary>
/// Middleware de anonimização e interceptação
/// </summary>
public class Anonymization
{
    private readonly RequestDelegate _next;
    private readonly ISqlInterceptor? _sqlInterceptor;

    public Anonymization(RequestDelegate next, ISqlInterceptor? sqlInterceptor = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _sqlInterceptor = sqlInterceptor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Gera e adiciona um ID de transação para rastreamento
        var transactionId = Guid.NewGuid().ToString();
        context.Response.Headers.Append("X-Transaction-Id", transactionId);

        // Se a requisição tiver um corpo, adiciona o transactionId também aos headers da requisição
        // para que possa ser capturado pelos interceptores
        if (context.Request.Headers != null)
        {
            context.Request.Headers.Append("X-Transaction-Id", transactionId);
        }

        // Continua o pipeline
        await _next(context);
    }
}

/// <summary>
/// Extensões para o middleware de anonimização
/// </summary>
public static class AnonymizationExtensions
{
    /// <summary>
    /// Adiciona o middleware de anonimização ao pipeline da aplicação
    /// </summary>
    public static IApplicationBuilder UseAnonymization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<Anonymization>();
    }

    /// <summary>
    /// Adiciona serviços necessários para anonimização e interceptação SQL
    /// </summary>
    public static IServiceCollection AddAnonymization(this IServiceCollection services, IConfiguration configuration)
    {
        // Adiciona serviços de interceptação SQL
        services.AddSqlInterception(configuration);

        // Retorna a coleção de serviços para permitir encadeamento
        return services;
    }
}
