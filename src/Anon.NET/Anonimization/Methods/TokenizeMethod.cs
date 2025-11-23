using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Anon.NET.Anonimization.Methods;

/// <summary>
/// Método de anonimização que substitui valores por tokens únicos
/// Permite reversibilidade e mantém relacionamentos entre dados
/// </summary>
public class TokenizeMethod : IPropertyAnonymizationMethod
{
    private static readonly ConcurrentDictionary<string, string> _tokenMap = new();
    private static readonly ConcurrentDictionary<string, string> _reverseTokenMap = new();

    private static readonly byte[] _secretKey = Encoding.UTF8.GetBytes("AnonymizationKey123456"); // 128-bit key

    private static readonly Random _random = new Random();

    public object? Process(object value, Type targetType, AnonymizeAttribute? attribute)
    {
        if (value == null) return null;

        string stringValue = value.ToString()!;
        if (string.IsNullOrEmpty(stringValue))
            return value;

        string token = GetOrCreateToken(stringValue);

        return ConvertTokenToTargetType(token, targetType, stringValue);
    }

    /// <summary>
    /// Obtém um token existente ou cria um novo para o valor
    /// </summary>
    private string GetOrCreateToken(string originalValue)
    {
        if (_reverseTokenMap.TryGetValue(originalValue, out string? existingToken))
        {
            return existingToken;
        }

        string newToken = GenerateToken(originalValue);

        _tokenMap.TryAdd(newToken, originalValue);
        _reverseTokenMap.TryAdd(originalValue, newToken);

        return newToken;
    }

    /// <summary>
    /// Gera um token único baseado no valor original
    /// </summary>
    private string GenerateToken(string originalValue)
    {
        using var hmac = new HMACSHA256(_secretKey);
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(originalValue));

        string hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        string baseToken = hashHex.Substring(0, Math.Min(16, hashHex.Length));

        string prefix = DetectDataTypePrefix(originalValue);

        string finalToken = prefix + baseToken;
        int counter = 1;
        while (_tokenMap.ContainsKey(finalToken))
        {
            finalToken = prefix + baseToken + counter.ToString("x2");
            counter++;
        }

        return finalToken;
    }

    /// <summary>
    /// Detecta o tipo de dado e retorna um prefixo apropriado
    /// </summary>
    private string DetectDataTypePrefix(string value)
    {
        if (IsEmailPattern(value)) return "em_";
        if (IsCpfPattern(value)) return "cp_";
        if (IsCnpjPattern(value)) return "cn_";
        if (IsPhonePattern(value)) return "ph_";
        if (IsCreditCardPattern(value)) return "cc_";
        if (IsNamePattern(value)) return "nm_";
        if (value.All(char.IsDigit)) return "id_";

        return "tk_";
    }

    /// <summary>
    /// Converte o token de volta para o tipo de destino
    /// </summary>
    private object ConvertTokenToTargetType(string token, Type targetType, string originalValue)
    {
        if (targetType == typeof(string))
            return token;

        try
        {
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                int hashCode = token.GetHashCode();
                return Math.Abs(hashCode % 1000000); // Limita a 6 dígitos
            }

            if (targetType == typeof(long) || targetType == typeof(long?))
            {
                long hashCode = token.GetHashCode();
                return Math.Abs(hashCode % 10000000000L); // Limita a 10 dígitos
            }

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
            {
                int hashCode = token.GetHashCode();
                return Math.Abs(hashCode % 100000) / 100.0m;
            }

            return token;
        }
        catch
        {
            return token;
        }
    }

    #region Pattern Detection

    private bool IsEmailPattern(string value) => value.Contains('@') && value.Contains('.');

    private bool IsCpfPattern(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 11;
    }

    private bool IsCnpjPattern(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 14;
    }

    private bool IsPhonePattern(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length >= 10 && digits.Length <= 11;
    }

    private bool IsCreditCardPattern(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length >= 13 && digits.Length <= 19;
    }

    private bool IsNamePattern(string value)
    {
        return value.Contains(' ') && value.All(c => char.IsLetter(c) || char.IsWhiteSpace(c));
    }

    #endregion

    #region Token Management (Métodos utilitários para gerenciamento)

    /// <summary>
    /// Obtém o valor original a partir de um token (processo reverso)
    /// </summary>
    public static string? GetOriginalValue(string token)
    {
        return _tokenMap.TryGetValue(token, out string? originalValue) ? originalValue : null;
    }

    /// <summary>
    /// Obtém o token a partir do valor original
    /// </summary>
    public static string? GetToken(string originalValue)
    {
        return _reverseTokenMap.TryGetValue(originalValue, out string? token) ? token : null;
    }

    /// <summary>
    /// Limpa o cache de tokens (útil para testes)
    /// </summary>
    public static void ClearTokenCache()
    {
        _tokenMap.Clear();
        _reverseTokenMap.Clear();
    }

    /// <summary>
    /// Obtém estatísticas do cache de tokens
    /// </summary>
    public static (int TokenCount, int TotalMemoryUsage) GetTokenStats()
    {
        int tokenCount = _tokenMap.Count;
        int memoryUsage = _tokenMap.Sum(kvp => kvp.Key.Length + kvp.Value.Length) * sizeof(char);
        return (tokenCount, memoryUsage);
    }

    #endregion
}
