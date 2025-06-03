using Anon.NET.Anonimization.Interfaces;

namespace Anon.NET.Anonimization.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class AnonymizeAttribute : Attribute, IAnonymizationAttribute
{
    public PropertyAnonymizationMethod Method { get; }
    public string Format { get; set; } = "";
    public int MaskInitPosition { get; set; }
    public int MaskEndPosition { get; set; }

    public AnonymizeAttribute(PropertyAnonymizationMethod method)
    {
        Method = method;
    }

    public AnonymizeAttribute(PropertyAnonymizationMethod method, int maskInitPosition, int maskEndPosition)
    {
        Method = method;
        MaskInitPosition = maskInitPosition;
        MaskEndPosition = maskEndPosition;
    }
}

public enum PropertyAnonymizationMethod
{
    Hash,           // Hash criptográfico
    Mask,           // Mascara parcial (ex: ****)
    Tokenize,       // Substituição por token
    Nullify,        // Substitui por null
    Randomize,      // Valor aleatório do mesmo tipo
}
