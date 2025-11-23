using Anon.NET.Anonimization.Attributes;

namespace Anon.NET.Anonimization.Interfaces;

public interface IPropertyAnonymizationMethod
{
    object? Process(object value, Type targetType, AnonymizeAttribute? attribute);
}
