using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;

namespace Anon.NET.Anonimization.Methods;

/// <summary>
/// Método de anonimização que mascara parte do valor com caracteres especiais
/// Útil para CPF, telefones, emails, etc.
/// </summary>
public class MaskMethod : IPropertyAnonymizationMethod
{
    public object? Process(object value, Type targetType, AnonymizeAttribute? attribute)
    {
        if (value == null) return null;

        string stringValue = value.ToString() ?? "";
        if (string.IsNullOrEmpty(stringValue))
            return value;

        string maskedValue;

        if (attribute != null && (attribute.MaskInitPosition > 0 || attribute.MaskEndPosition > 0))
        {
            maskedValue = ApplyCustomMask(stringValue, attribute.MaskInitPosition, attribute.MaskEndPosition);
        }
        else
        {
            maskedValue = ApplyDefaultMask(stringValue);
        }

        return ConvertToTargetType(maskedValue, targetType);
    }

    /// <summary>
    /// Aplica máscara personalizada baseada nas posições especificadas
    /// </summary>
    private string ApplyCustomMask(string value, int startPosition, int endPosition)
    {
        if (value.Length <= 2) return new string('*', value.Length);

        int start = Math.Max(0, startPosition);
        int end = endPosition > 0 ? Math.Min(value.Length, endPosition) : value.Length;

        if (start >= end) return new string('*', value.Length);

        var chars = value.ToCharArray();
        for (int i = start; i < end && i < chars.Length; i++)
        {
            if (char.IsLetterOrDigit(chars[i]))
                chars[i] = '*';
        }

        return new string(chars);
    }

    /// <summary>
    /// Aplica máscara padrão baseada no padrão comum do valor
    /// </summary>
    private string ApplyDefaultMask(string value)
    {
        // Remove espaços e caracteres especiais para análise
        string cleanValue = new string(value.Where(c => char.IsLetterOrDigit(c)).ToArray());

        // CPF: 123.456.789-01 -> 123.***.***-**
        if (IsCpfPattern(value))
        {
            return MaskCpf(value);
        }

        // CNPJ: 12.345.678/0001-90 -> 12.345.**/****-**
        if (IsCnpjPattern(value))
        {
            return MaskCnpj(value);
        }

        // Email: usuario@dominio.com -> us***@dom***.com
        if (IsEmailPattern(value))
        {
            return MaskEmail(value);
        }

        // Telefone: (11) 99999-9999 -> (11) 9****-****
        if (IsPhonePattern(value))
        {
            return MaskPhone(value);
        }

        // Cartão de crédito: 1234 5678 9012 3456 -> **** **** **** 3456
        if (IsCreditCardPattern(cleanValue))
        {
            return MaskCreditCard(value);
        }

        return MaskGeneric(value);
    }

    #region Pattern Detection

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

    private bool IsEmailPattern(string value)
    {
        return value.Contains('@') && value.Contains('.');
    }

    private bool IsPhonePattern(string value)
    {
        string digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length >= 10 && digits.Length <= 11;
    }

    private bool IsCreditCardPattern(string value)
    {
        return value.Length >= 13 && value.Length <= 19 && value.All(char.IsDigit);
    }

    #endregion

    #region Specific Masking Methods

    private string MaskCpf(string cpf)
    {
        // 123.456.789-01 -> 123.***.***-**
        if (cpf.Length == 14)
        {
            return cpf.Substring(0, 4) + "***.***.***-**";
        }
        else // Sem formatação: 12345678901 -> 123***
        {
            return cpf.Substring(0, 3) + new string('*', cpf.Length - 3);
        }
    }

    private string MaskCnpj(string cnpj)
    {
        // 12.345.678/0001-90 -> 12.345.***/****-**
        if (cnpj.Length == 18)
        {
            return cnpj.Substring(0, 7) + "***/****-**";
        }
        else // Sem formatação
        {
            return cnpj.Substring(0, 6) + new string('*', cnpj.Length - 6);
        }
    }

    private string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return new string('*', email.Length);

        string username = parts[0];
        string domain = parts[1];

        string maskedUsername = username.Length <= 2
            ? new string('*', username.Length)
            : username[0] + new string('*', Math.Max(1, username.Length - 2)) + username[^1];

        var domainParts = domain.Split('.');
        if (domainParts.Length >= 2)
        {
            string domainName = domainParts[0];
            string maskedDomain = domainName.Length <= 2
                ? new string('*', domainName.Length)
                : domainName[0] + new string('*', Math.Max(1, domainName.Length - 2)) + domainName[^1];

            domain = maskedDomain + "." + string.Join(".", domainParts.Skip(1));
        }

        return maskedUsername + "@" + domain;
    }

    private string MaskPhone(string phone)
    {
        // (11) 99999-9999 -> (11) 9****-****
        string digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length == 11)
        {
            return $"({digits.Substring(0, 2)}) {digits[2]}****-****";
        }
        else if (digits.Length == 10)
        {
            return $"({digits.Substring(0, 2)}) ****-****";
        }

        // Formato genérico
        return phone.Substring(0, Math.Min(4, phone.Length)) + new string('*', Math.Max(0, phone.Length - 4));
    }

    private string MaskCreditCard(string card)
    {
        // 1234567890123456 -> ************3456
        if (card.Length >= 4)
        {
            return new string('*', card.Length - 4) + card.Substring(card.Length - 4);
        }
        return new string('*', card.Length);
    }

    private string MaskGeneric(string value)
    {
        if (value.Length <= 2)
            return new string('*', value.Length);

        if (value.Length <= 4)
            return value[0] + new string('*', value.Length - 2) + value[^1];

        // Para strings maiores, mostra 2 caracteres no início e 2 no final
        int visibleChars = Math.Min(4, value.Length / 3);
        int startChars = visibleChars / 2;
        int endChars = visibleChars - startChars;

        return value.Substring(0, startChars) +
               new string('*', value.Length - visibleChars) +
               value.Substring(value.Length - endChars);
    }

    #endregion

    /// <summary>
    /// Converte o valor mascarado de volta para o tipo de destino
    /// </summary>
    private object ConvertToTargetType(string maskedValue, Type targetType)
    {
        if (targetType == typeof(string))
            return maskedValue;

        return maskedValue;
    }
}
