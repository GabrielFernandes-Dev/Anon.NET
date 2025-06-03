using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;
using Anon.NET.Anonimization.Methods;

namespace Anon.NET.Anonimization;

public class AnonymizationMethodRegistry : IAnonymizationMethodRegistry
{
    private readonly Dictionary<PropertyAnonymizationMethod, IAnonymizationMethod> _processors =
            new Dictionary<PropertyAnonymizationMethod, IAnonymizationMethod>();

    public AnonymizationMethodRegistry()
    {
        RegisterProcessor(PropertyAnonymizationMethod.Hash, new HasingMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Mask, new MaskMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Nullify, new NullifyMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Randomize, new RandomizeMethod());
        RegisterProcessor(PropertyAnonymizationMethod.Tokenize, new TokenizeMethod());
    }

    public IAnonymizationMethod? GetProcessor(PropertyAnonymizationMethod method)
    {
        return _processors.TryGetValue(method, out var processor) ? processor : null;
    }

    public void RegisterProcessor(PropertyAnonymizationMethod method, IAnonymizationMethod processor)
    {
        _processors[method] = processor;
    }
}
