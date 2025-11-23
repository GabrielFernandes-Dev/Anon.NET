using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;
using Anon.NET.Anonimization.Methods;

namespace Anon.NET.Anonimization;

public class AnonymizationMethodRegistry : IAnonymizationMethodRegistry
{
    private readonly Dictionary<PropertyAnonymizationMethod, IPropertyAnonymizationMethod> _propertieProcessors = new();
    private readonly IKAnonymityMethod _kAnonymityProcessor;

    public AnonymizationMethodRegistry()
    {
        RegisterProcessor(PropertyAnonymizationMethod.Hash, new HasingMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Mask, new MaskMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Nullify, new NullifyMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Randomize, new RandomizeMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Tokenize, new TokenizeMethod());

        _kAnonymityProcessor = new KAnonymityMethod();
    }

    public IPropertyAnonymizationMethod? GetProcessor(PropertyAnonymizationMethod method)
    {
        return _propertieProcessors.TryGetValue(method, out var processor) ? processor : null;
    }

    public IKAnonymityMethod GetKAnonymityProcessor()
    {
        return _kAnonymityProcessor;
    }

    public void RegisterProcessor(PropertyAnonymizationMethod method, IPropertyAnonymizationMethod processor)
    {
        _propertieProcessors[method] = processor;
    }
}
