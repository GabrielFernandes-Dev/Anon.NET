using Anon.NET.SqlInterception.Models;
using System.Data.Common;

namespace Anon.NET.SqlInterception.Interfaces;

public interface ISqlInterceptor
{
    /// <summary>
    /// Intercepta e registra uma query SQL
    /// </summary>
    /// <param name="query">Informações da query</param>
    void InterceptQuery(SqlQuery query);

    /// <summary>
    /// Extrai metadados de um comando para criar um objeto SqlQuery
    /// </summary>
    /// <param name="command">Comando a ser analisado</param>
    /// <param name="transactionId">ID opcional da transação</param>
    /// <returns>Objeto SqlQuery com os metadados extraídos</returns>
    SqlQuery ExtractQueryFromCommand(DbCommand command, string? transactionId = null);
}
