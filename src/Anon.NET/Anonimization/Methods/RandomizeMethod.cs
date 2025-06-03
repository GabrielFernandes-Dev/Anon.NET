using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;

namespace Anon.NET.Anonimization.Methods;

/// <summary>
/// Método de anonimização que gera valores aleatórios do mesmo tipo
/// Mantém a estrutura dos dados mas altera o conteúdo
/// </summary>
public class RandomizeMethod : IAnonymizationMethod
{
    private static readonly Random _random = new Random();
    private static readonly string[] _firstNames = { "João", "Maria", "Pedro", "Ana", "Carlos", "Fernanda", "Ricardo", "Juliana", "Marcos", "Patrícia" };
    private static readonly string[] _lastNames = { "Silva", "Santos", "Oliveira", "Souza", "Lima", "Pereira", "Costa", "Rodrigues", "Almeida", "Nascimento" };
    private static readonly string[] _domains = { "email.com", "teste.com", "exemplo.org", "demo.net", "sample.com" };

    public object? Process(object value, Type targetType, AnonymizeAttribute? attribute)
    {
        if (value == null) return null;

        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return underlyingType.Name switch
        {
            nameof(String) => RandomizeString(value.ToString()!),
            nameof(Int32) => RandomizeInt((int)value),
            nameof(Int64) => RandomizeLong((long)value),
            nameof(Decimal) => RandomizeDecimal((decimal)value),
            nameof(Double) => RandomizeDouble((double)value),
            nameof(DateTime) => RandomizeDateTime((DateTime)value),
            nameof(Boolean) => RandomizeBool(),
            _ => RandomizeGeneric(value, underlyingType)
        };
    }

    #region String Randomization

    private string RandomizeString(string original)
    {
        if (IsEmailPattern(original))
            return RandomizeEmail();

        if (IsCpfPattern(original))
            return RandomizeCpf();

        if (IsCnpjPattern(original))
            return RandomizeCnpj();

        if (IsPhonePattern(original))
            return RandomizePhone();

        if (IsNamePattern(original))
            return RandomizeName();

        return RandomizeGenericString(original);
    }

    private string RandomizeEmail()
    {
        string username = GenerateRandomString(5, 10, true);
        string domain = _domains[_random.Next(_domains.Length)];
        return $"{username}@{domain}";
    }

    private string RandomizeCpf()
    {
        // Gera CPF aleatório (formato apenas, não válido)
        return $"{_random.Next(100, 999)}.{_random.Next(100, 999)}.{_random.Next(100, 999)}-{_random.Next(10, 99)}";
    }

    private string RandomizeCnpj()
    {
        // Gera CNPJ aleatório (formato apenas, não válido)
        return $"{_random.Next(10, 99)}.{_random.Next(100, 999)}.{_random.Next(100, 999)}/{_random.Next(1000, 9999)}-{_random.Next(10, 99)}";
    }

    private string RandomizePhone()
    {
        // Gera telefone brasileiro aleatório
        int ddd = _random.Next(11, 99);
        if (_random.NextDouble() > 0.5) // Celular
        {
            return $"({ddd}) 9{_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}";
        }
        else // Fixo
        {
            return $"({ddd}) {_random.Next(1000, 9999)}-{_random.Next(1000, 9999)}";
        }
    }

    private string RandomizeName()
    {
        string firstName = _firstNames[_random.Next(_firstNames.Length)];
        string lastName = _lastNames[_random.Next(_lastNames.Length)];
        return $"{firstName} {lastName}";
    }

    private string RandomizeGenericString(string original)
    {
        var result = new char[original.Length];

        for (int i = 0; i < original.Length; i++)
        {
            char c = original[i];

            if (char.IsLetter(c))
            {
                if (char.IsUpper(c))
                    result[i] = (char)_random.Next('A', 'Z' + 1);
                else
                    result[i] = (char)_random.Next('a', 'z' + 1);
            }
            else if (char.IsDigit(c))
            {
                result[i] = (char)_random.Next('0', '9' + 1);
            }
            else
            {
                result[i] = c;
            }
        }

        return new string(result);
    }

    private string GenerateRandomString(int minLength, int maxLength, bool alphanumeric = true)
    {
        int length = _random.Next(minLength, maxLength + 1);
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        const string numbers = "0123456789";
        string charset = alphanumeric ? chars + numbers : chars;

        return new string(Enumerable.Repeat(charset, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    #endregion

    #region Numeric Randomization

    private int RandomizeInt(int original)
    {
        int magnitude = GetMagnitude(Math.Abs(original));
        int min = (int)Math.Pow(10, magnitude - 1);
        int max = (int)Math.Pow(10, magnitude) - 1;

        int randomValue = _random.Next(min, max + 1);
        return original < 0 ? -randomValue : randomValue;
    }

    private long RandomizeLong(long original)
    {
        int magnitude = GetMagnitude(Math.Abs(original));
        long min = (long)Math.Pow(10, magnitude - 1);
        long max = (long)Math.Pow(10, magnitude) - 1;

        long randomValue = _random.NextInt64(min, max + 1);
        return original < 0 ? -randomValue : randomValue;
    }

    private decimal RandomizeDecimal(decimal original)
    {
        string originalStr = original.ToString();
        int decimalPlaces = originalStr.Contains('.')
            ? originalStr.Split('.')[1].Length
            : 0;

        double randomDouble = _random.NextDouble() * (double)Math.Abs(original) * 2;
        decimal randomDecimal = Math.Round((decimal)randomDouble, decimalPlaces);

        return original < 0 ? -randomDecimal : randomDecimal;
    }

    private double RandomizeDouble(double original)
    {
        double randomValue = _random.NextDouble() * Math.Abs(original) * 2;
        return original < 0 ? -randomValue : randomValue;
    }

    private float RandomizeFloat(float original)
    {
        float randomValue = (float)_random.NextDouble() * Math.Abs(original) * 2;
        return original < 0 ? -randomValue : randomValue;
    }

    #endregion

    #region Other Types

    private DateTime RandomizeDateTime(DateTime original)
    {
        // Randomiza dentro de ±1 ano da data original
        int daysOffset = _random.Next(-365, 365);
        return original.AddDays(daysOffset);
    }

    private bool RandomizeBool()
    {
        return _random.NextDouble() > 0.5;
    }

    private object RandomizeGeneric(object value, Type type)
    {
        try
        {
            return Activator.CreateInstance(type) ?? value;
        }
        catch
        {
            return value;
        }
    }

    #endregion

    #region Helper Methods

    private int GetMagnitude(long number)
    {
        if (number == 0) return 1;
        return (int)Math.Floor(Math.Log10(Math.Abs(number))) + 1;
    }

    private bool IsEmailPattern(string value) => value.Contains('@') && value.Contains('.');
    private bool IsCpfPattern(string value) => value.Replace(".", "").Replace("-", "").Length == 11 && value.Any(char.IsDigit);
    private bool IsCnpjPattern(string value) => value.Replace(".", "").Replace("/", "").Replace("-", "").Length == 14 && value.Any(char.IsDigit);
    private bool IsPhonePattern(string value) => value.Count(char.IsDigit) >= 10 && value.Count(char.IsDigit) <= 11;
    private bool IsNamePattern(string value) => value.Contains(' ') && value.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));

    #endregion
}
