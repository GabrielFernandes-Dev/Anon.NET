using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;
using System;

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

        var entityType = entity.GetType();
        var properties = entityType.GetProperties().Where(p => p.GetCustomAttributes(typeof(AnonymizeAttribute), true).Any());

        var classAttributes = entityType.GetCustomAttributes(true);
        foreach (var attribute in classAttributes.Where(at => typeof(IAnonymizationAttribute).IsAssignableFrom(at.GetType())))
        {
            if (attribute is KAnonymityAttribute kAnonAttr)
            {
                int k = kAnonAttr.KValue;
                
            }
            else
            {
            }
        }

        ProcessPropertyAttributes(entity, properties);

        return entity;
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
