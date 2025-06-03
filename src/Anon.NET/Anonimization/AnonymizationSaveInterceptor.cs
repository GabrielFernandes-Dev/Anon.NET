using Anon.NET.Anonimization.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Anon.NET.Anonimization;

/// <summary>
/// Interceptor que aplica anonimização antes de salvar dados no banco
/// </summary>
public class AnonymizationSaveInterceptor : SaveChangesInterceptor
{
    private readonly IAnonymizationProcessor _anonymizer;

    public AnonymizationSaveInterceptor(IAnonymizationProcessor anonymizer)
    {
        _anonymizer = anonymizer;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ProcessEntitiesBeforeSave(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ProcessEntitiesBeforeSave(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ProcessEntitiesBeforeSave(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in entries)
        {
            _anonymizer.ProcessEntity(entry.Entity);
        }
    }
}
