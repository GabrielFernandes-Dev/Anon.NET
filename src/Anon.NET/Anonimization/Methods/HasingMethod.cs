using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;
using System.Diagnostics;

namespace Anon.NET.Anonimization.Methods;

public class HasingMethod : IAnonymizationMethod
{
    public object? Process(object value, Type targetType, AnonymizeAttribute? attribute)
    {
        if (value == null) return null;

        string stringValue = value.ToString() ?? "";
        if (string.IsNullOrEmpty(stringValue))
            return value;

        // Calcula o hash SHA256 do valor
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(stringValue);
        var hashBytes = sha256.ComputeHash(bytes);
        var hash = BitConverter.ToString(hashBytes).Replace("-", "");

        if (targetType == typeof(string))
            return hash;

        try
        {
            if (targetType == typeof(int) || targetType == typeof(int?))
                return BitConverter.ToInt32(hashBytes, 0);

            if (targetType == typeof(long) || targetType == typeof(long?))
                return BitConverter.ToInt64(hashBytes, 0);

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                int intVal = BitConverter.ToInt32(hashBytes, 0);
                return (decimal)intVal / 100;
            }

            // Para outros tipos, retornamos o valor padrão
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
        catch
        {
            // Se falhar na conversão, retornamos o valor padrão do tipo
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }
}
