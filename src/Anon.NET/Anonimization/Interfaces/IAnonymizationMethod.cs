using Anon.NET.Anonimization.Attributes;

namespace Anon.NET.Anonimization.Interfaces;

public interface IAnonymizationMethod
{
    object? Process(object value, Type targetType, AnonymizeAttribute? attribute);
}
