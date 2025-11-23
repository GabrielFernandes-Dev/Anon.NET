using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;

namespace Anon.NET.Anonimization.Methods;

/// <summary>
/// Método de anonimização que substitui o valor por null ou valor padrão
/// Útil para dados muito sensíveis que não devem ser visíveis
/// </summary>
public class NullifyMethod : IPropertyAnonymizationMethod
{
    public object? Process(object value, Type targetType, AnonymizeAttribute? attribute)
    {
        if (value == null) return null;

        if (IsNullableType(targetType))
        {
            return null;
        }

        return GetDefaultValue(targetType);
    }

    /// <summary>
    /// Verifica se o tipo pode aceitar valores nulos
    /// </summary>
    private bool IsNullableType(Type type)
    {
        if (!type.IsValueType)
            return true;

        if (Nullable.GetUnderlyingType(type) != null)
            return true;

        return false;
    }

    /// <summary>
    /// Retorna o valor padrão para o tipo especificado
    /// </summary>
    private object? GetDefaultValue(Type type)
    {
        if (Nullable.GetUnderlyingType(type) != null)
            return null;

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return null;
    }
}
