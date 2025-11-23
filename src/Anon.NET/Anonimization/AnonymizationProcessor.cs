using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;
using Anon.NET.Anonimization.Methods;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace Anon.NET.Anonimization;

public class AnonymizationProcessor : IAnonymizationProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAnonymizationMethodRegistry _methodResgistry;

    public AnonymizationProcessor(IServiceProvider serviceProvider, IAnonymizationMethodRegistry methodResgistry)
    {
        _serviceProvider = serviceProvider;
        _methodResgistry = methodResgistry;
    }

    public object? ProcessEntity(object entity)
    {
        if (entity == null) return null;

        if (AnonymizationContext.IsUpdating)
            return entity;

        var entityType = entity.GetType();
        var properties = entityType.GetProperties().Where(p => p.GetCustomAttributes(typeof(AnonymizeAttribute), true).Any());

        var classAttributes = entityType.GetCustomAttributes(true);
        foreach (var attribute in classAttributes.Where(at => typeof(IAnonymizationAttribute).IsAssignableFrom(at.GetType())))
        {
            if (attribute is KAnonymityAttribute kAnonAttr)
            {
                int k = kAnonAttr.KValue;
                List<PropertyInfo> quasiIdentifierProperty = entityType.GetProperties()
                    .Where(p => p.GetCustomAttributes(typeof(QuasiIdentifierAttribute), true).Any())
                    .ToList();

                var processor = _methodResgistry.GetKAnonymityProcessor();

                ConfigurarCallbackDeAtualizacao(processor, entityType);

                processor.Process(entity, quasiIdentifierProperty, k);
                return entity;
            }
        }

        ProcessPropertyAttributes(entity, properties);

        return entity;
    }

    private void ConfigurarCallbackDeAtualizacao(IKAnonymityMethod processor, Type entityType)
    {
        if (processor is KAnonymityMethod kAnonymityProcessor)
        {
            kAnonymityProcessor.AtualizarEntidadesNoBanco = (entidades) =>
            {
                AnonymizationContext.ExecuteUpdate(() =>
                {
                    try
                    {
                        bool flowControl = AtualizarEntidadesNoBanco(entityType, entidades);
                        if (!flowControl)
                        {
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AnonymizationProcessor] ERRO ao atualizar entidades no banco: {ex.Message}");
                        Console.WriteLine($"StackTrace: {ex.StackTrace}");
                        throw;
                    }
                });
            };
        }
    }

    private bool AtualizarEntidadesNoBanco(Type entityType, List<object> entidades)
    {
        var dbContext = ObterDbContext(entityType);

        if (dbContext == null)
        {
            Console.WriteLine("[AnonymizationProcessor] ERRO: Não foi possível obter o DbContext");
            return false;
        }

        Console.WriteLine($"[AnonymizationProcessor] Atualizando {entidades.Count} entidades do tipo {entityType.Name}...");

        var idProperty = ObterPropriedadeId(entityType);
        if (idProperty == null)
        {
            Console.WriteLine("[AnonymizationProcessor] ERRO: Não foi possível encontrar propriedade ID");
            return false;
        }

        RecarregarEntidades(entityType, entidades, dbContext, idProperty);

        int registrosAtualizados = dbContext.SaveChanges();

        Console.WriteLine($"[AnonymizationProcessor] {registrosAtualizados} registros atualizados com sucesso no banco de dados!");
        return true;
    }

    private void RecarregarEntidades(Type entityType, List<object> entidades, DbContext dbContext, PropertyInfo idProperty)
    {
        foreach (var entidade in entidades)
        {
            var idValue = idProperty.GetValue(entidade);


            var entidadeRastreada = dbContext.Find(entityType, idValue);

            if (entidadeRastreada == null)
            {
                Console.WriteLine($"[AnonymizationProcessor] AVISO: Entidade com ID {idValue} não encontrada no banco");
                continue;
            }

            foreach (var prop in entityType.GetProperties())
            {
                if (prop.CanWrite && prop.Name != idProperty.Name)
                {
                    var valorAnonimizado = prop.GetValue(entidade);
                    prop.SetValue(entidadeRastreada, valorAnonimizado);
                }
            }
        }
    }

    private PropertyInfo? ObterPropriedadeId(Type entityType)
    {
        // Busca pela propriedade chamada "Id"
        var idProperty = entityType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (idProperty != null) return idProperty;

        // Busca por TipoId (ex: UserId)
        idProperty = entityType.GetProperty($"{entityType.Name}Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (idProperty != null) return idProperty;

        // Busca por atributo [Key]
        foreach (var prop in entityType.GetProperties())
        {
            var keyAttr = prop.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute));
            if (keyAttr != null) return prop;
        }

        return null;
    }

    /// <summary>
    /// Obtém o DbContext apropriado para o tipo de entidade
    /// </summary>
    private DbContext? ObterDbContext(Type entityType)
    {
        try
        {
            // 1. Encontra o tipo do DbContext
            var dbContextType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(DbContext)) && !t.IsAbstract)
                .FirstOrDefault(contextType =>
                {
                    return contextType.GetProperties()
                        .Any(p => p.PropertyType.IsGenericType &&
                                 p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                 p.PropertyType.GetGenericArguments()[0] == entityType);
                });

            if (dbContextType == null)
            {
                Console.WriteLine($"[AnonymizationProcessor] AVISO: Não foi encontrado um DbContext para o tipo {entityType.Name}");
                return null;
            }

            var dbContext = _serviceProvider.GetService(dbContextType) as DbContext;

            if (dbContext == null)
            {
                Console.WriteLine($"[AnonymizationProcessor] AVISO: DbContext do tipo {dbContextType.Name} não está registrado no DI");
                return null;
            }

            return dbContext;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AnonymizationProcessor] ERRO ao obter DbContext: {ex.Message}");
            return null;
        }
    }

    private void ProcessPropertyAttributes(object entity, IEnumerable<System.Reflection.PropertyInfo> properties)
    {
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttributes(typeof(AnonymizeAttribute), true).FirstOrDefault() as AnonymizeAttribute;

            if (attribute == null) continue;

            var currentValue = property.GetValue(entity);
            if (currentValue == null) continue;

            var processor = _methodResgistry.GetProcessor(attribute.Method);
            if (processor == null) continue;

            var anonymizedValue = processor.Process(currentValue, property.PropertyType, attribute);

            property.SetValue(entity, anonymizedValue);
        }
    }


    public T? ProcessEntity<T>(T entity) where T : class
    {
        return (T?)ProcessEntity((object)entity);
    }
}
