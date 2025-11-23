using Anon.NET.Anonimization.Attributes;
using System.Reflection;

namespace Anon.NET.Anonimization.Interfaces;

public interface IKAnonymityMethod
{
    /// <summary>
    /// Armazena os agrupamentos não finalizados para cada tipo de entidade
    /// </summary>
    Dictionary<string, Dictionary<int, object>>? AgrupamentosNaoFinalizados { get; set; }

    /// <summary>
    /// Armazena os IDs das entidades que foram salvas no banco mas ainda não foram anonimizadas
    /// </summary>
    Dictionary<string, List<object>>? EntidadesPendentesDeAtualizacao { get; set; }

    /// <summary>
    /// Ação de callback para atualizar entidades no banco de dados
    /// </summary>
    Action<List<object>>? AtualizarEntidadesNoBanco { get; set; }

    void Process(object value, List<PropertyInfo>? quasiIdentifierAttribute, int K);
}
