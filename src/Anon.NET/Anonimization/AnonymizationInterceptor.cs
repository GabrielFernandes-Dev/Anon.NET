using Anon.NET.Anonimization.Interfaces;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Anon.NET.Anonimization;

public class AnonymizationInterceptor : IMaterializationInterceptor
{
    private readonly IAnonymizationProcessor _anonymizer;

    public AnonymizationInterceptor(IAnonymizationProcessor anonymizer)
    {
        _anonymizer = anonymizer;
    }

    public object InitializedInstance(MaterializationInterceptionData materializationData,
                                     object instance)
    {
        // Aplica anonimização após a instância ser criada pelo EF Core
        return _anonymizer.ProcessEntity(instance)!;
    }
}
