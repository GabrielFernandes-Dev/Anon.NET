using Anon.NET.SqlInterception;
using Anon.NET.SqlInterception.EntityFramework;
using Anon.NET.SqlInterception.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;

namespace Anon.NET.Core.Extensions;

/// <summary>
/// Extensões para configuração dos serviços de interceptação de SQL
/// </summary>
public static class SqlInterceptionExtensions
{
    /// <summary>
    /// Adiciona serviços de interceptação de SQL à coleção de serviços
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="configuration">Configuração da aplicação</param>
    /// <returns>Coleção de serviços atualizada</returns>
    public static IServiceCollection AddSqlInterception(this IServiceCollection services, IConfiguration configuration)
    {
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        // Registra o logger como um singleton
        services.AddSingleton<ILogger>(logger);

        // Registra o interceptor como um singleton
        services.AddSingleton<ISqlInterceptor, SqlInterceptor>();

        // Registra o interceptor do EF Core como um singleton
        services.AddSingleton<AnonDbCommandInterceptor>();

        // Garante que o HttpContextAccessor esteja registrado
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Configura o DbContext para usar o interceptor de SQL
    /// </summary>
    /// <param name="optionsBuilder">Builder de opções do DbContext</param>
    /// <param name="interceptor">Interceptor de comandos</param>
    /// <returns>Builder de opções atualizado</returns>
    public static DbContextOptionsBuilder UseAnonSqlInterception(
        this DbContextOptionsBuilder optionsBuilder,
        AnonDbCommandInterceptor interceptor)
    {
        optionsBuilder.AddInterceptors(interceptor);
        return optionsBuilder;
    }
}
