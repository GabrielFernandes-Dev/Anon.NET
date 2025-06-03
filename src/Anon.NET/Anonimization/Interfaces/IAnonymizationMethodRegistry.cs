using Anon.NET.Anonimization.Attributes;

namespace Anon.NET.Anonimization.Interfaces;

public interface IAnonymizationMethodRegistry
{
    IAnonymizationMethod? GetProcessor(PropertyAnonymizationMethod method);
    void RegisterProcessor(PropertyAnonymizationMethod method, IAnonymizationMethod processor);
}
