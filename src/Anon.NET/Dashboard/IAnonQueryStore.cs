using Anon.NET.SqlInterception.Models;

namespace Anon.NET.Dashboard;

public interface IAnonQueryStore
{
    /// <summary>
    /// Adiciona uma query ao armazenamento
    /// </summary>
    /// <param name="query">A query SQL interceptada</param>
    void AddQuery(SqlQuery query);

    /// <summary>
    /// Obtém todas as queries armazenadas
    /// </summary>
    /// <returns>Uma coleção de queries SQL</returns>
    IEnumerable<SqlQuery> GetAllQueries();
}
