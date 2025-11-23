using Anon.NET.Anonimization.Attributes;

namespace Anon.NET.Anonimization.Interfaces;

public interface IAnonymizationMethodRegistry
{
    IPropertyAnonymizationMethod? GetProcessor(PropertyAnonymizationMethod method);
    IKAnonymityMethod GetKAnonymityProcessor();
    void RegisterProcessor(PropertyAnonymizationMethod method, IPropertyAnonymizationMethod processor);
}
