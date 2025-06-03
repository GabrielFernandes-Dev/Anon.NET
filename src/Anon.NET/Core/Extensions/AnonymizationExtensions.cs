using Anon.NET.Anonimization;
using Anon.NET.Anonimization.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Anon.NET.Core.Extensions;
public static class AnonymizationExtensions
{
    public static IServiceCollection AddAnonymization(this IServiceCollection services)
    {
        // Registra os serviços necessários
        services.AddSingleton<IAnonymizationMethodRegistry, AnonymizationMethodRegistry>();
        services.AddScoped<IAnonymizationProcessor, AnonymizationProcessor>();

        services.AddScoped<AnonymizationInterceptor>();
        services.AddScoped<AnonymizationSaveInterceptor>();

        return services;
    }

    public static DbContextOptionsBuilder UseAnonymization(
        this DbContextOptionsBuilder optionsBuilder,
        AnonymizationInterceptor materializationInterceptor,
        AnonymizationSaveInterceptor saveInterceptor)
    {
        optionsBuilder.AddInterceptors(materializationInterceptor, saveInterceptor);
        return optionsBuilder;
    }
}
