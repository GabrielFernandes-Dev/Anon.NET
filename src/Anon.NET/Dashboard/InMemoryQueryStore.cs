using Anon.NET.SqlInterception.Models;
using System.Collections.Concurrent;

namespace Anon.NET.Dashboard;

public class InMemoryQueryStore : IAnonQueryStore
{
    private readonly ConcurrentQueue<SqlQuery> _queries = new();
    private readonly int _maxSize;

    /// <summary>
    /// Cria uma nova instância do armazenamento em memória
    /// </summary>
    /// <param name="maxSize">Número máximo de queries a serem armazenadas</param>
    public InMemoryQueryStore(int maxSize)
    {
        _maxSize = maxSize;
    }

    /// <inheritdoc />
    public void AddQuery(SqlQuery query)
    {
        _queries.Enqueue(query);
        while (_queries.Count > _maxSize && _queries.TryDequeue(out _)) { }
    }

    /// <inheritdoc />
    public IEnumerable<SqlQuery> GetAllQueries()
    {
        return _queries.ToArray();
    }
}
