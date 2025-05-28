using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Reflection;
using System.Text.Json;
using System.IO;
using Anon.NET.SqlInterception.Models;
using System.Collections.Concurrent;
using Anon.NET.Dashboard;

namespace Anon.NET.Core.Extensions;

public static class AnonDashboardExtensions
{
    private static readonly ConcurrentQueue<SqlQuery> _recentQueries = new ConcurrentQueue<SqlQuery>();
    private static readonly int _maxQueueSize = 100; // Número máximo de queries para armazenar em memória

    /// <summary>
    /// Adiciona o middleware para o dashboard do Anon.NET
    /// </summary>
    public static IServiceCollection AddAnonDashboard(this IServiceCollection services)
    {
        services.AddSingleton<IAnonQueryStore>(new InMemoryQueryStore(_maxQueueSize));

        return services;
    }

    /// <summary>
    /// Mapeia os endpoints do dashboard de anonimização
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="dashboardPath">Caminho base do dashboard</param>
    public static IApplicationBuilder UseAnonDashboard(this IApplicationBuilder app, string dashboardPath = "/anon-dashboard")
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Endpoint para obter as queries recentes como JSON
        app.Map($"{dashboardPath}/api/queries", queriesApp =>
        {
            queriesApp.Run(async context =>
            {
                var queryStore = context.RequestServices.GetRequiredService<IAnonQueryStore>();
                var queries = queryStore.GetAllQueries();

                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, queries, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            });
        });

        // Endpoint para arquivos estáticos (CSS, JS)
        app.Map($"{dashboardPath}/{{filename}}", fileApp =>
        {
            fileApp.Run(async context =>
            {
                var filename = context.Request.Path.Value?.Split('/').LastOrDefault();
                if (string.IsNullOrEmpty(filename))
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                // Obter todos os recursos embutidos para debug
                var allResources = assembly.GetManifestResourceNames();
                var resourceName = allResources.FirstOrDefault(r => r.EndsWith(filename));

                // Fallback para o caminho original caso não encontre pelo nome do arquivo
                if (string.IsNullOrEmpty(resourceName))
                {
                    resourceName = $"Anon.NET.Dashboard.Resources.{filename}";
                }

                var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                string contentType = "application/octet-stream";
                if (filename.EndsWith(".css")) contentType = "text/css";
                else if (filename.EndsWith(".js")) contentType = "application/javascript";

                context.Response.ContentType = contentType;
                await stream.CopyToAsync(context.Response.Body);
            });
        });

        // Endpoint para a página HTML do dashboard
        app.Map(dashboardPath, dashboardApp =>
        {
            dashboardApp.Run(async context =>
            {
                context.Response.ContentType = "text/html";
                var stream = assembly.GetManifestResourceStream("Anon.NET.Dashboard.Resources.index.html");

                if (stream != null)
                {
                    await stream.CopyToAsync(context.Response.Body);
                }
                else
                {
                    await context.Response.WriteAsync("Dashboard resource not found. Make sure the resources are embedded correctly.");
                }
            });
        });

        return app;
    }

    /// <summary>
    /// Registra uma query SQL no store para exibição no dashboard
    /// </summary>
    /// <param name="query">Query SQL a ser registrada</param>
    /// <param name="serviceProvider">Provedor de serviços</param>
    public static void LogQueryForDashboard(SqlQuery query, IServiceProvider serviceProvider)
    {
        var queryStore = serviceProvider.GetService<IAnonQueryStore>();
        if (queryStore != null)
        {
            queryStore.AddQuery(query);
        }
    }
}
