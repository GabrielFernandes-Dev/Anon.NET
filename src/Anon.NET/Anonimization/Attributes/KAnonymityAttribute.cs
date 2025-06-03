using Anon.NET.Anonimization.Interfaces;

namespace Anon.NET.Anonimization.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KAnonymityAttribute : Attribute, IAnonymizationAttribute
{
    public int KValue { get; set; }

    public KAnonymityAttribute(int k)
    {
        KValue = k;
    }
}
