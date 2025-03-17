namespace Anon.NET.SqlInterception.Models;

/// <summary>
/// Representa uma query SQL interceptada
/// </summary>
public class SqlQuery
{
    /// <summary>
    /// Identificador único da query
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Texto da query SQL
    /// </summary>
    public string CommandText { get; set; } = string.Empty;

    /// <summary>
    /// Parâmetros da query
    /// </summary>
    public Dictionary<string, object?> Parameters { get; set; } = new();

    /// <summary>
    /// Timestamp de quando a query foi executada
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duração da execução em milissegundos
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Origem da query (controller, método, etc)
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// ID da transação HTTP relacionada
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Tipo da query (SELECT, INSERT, UPDATE, DELETE, etc)
    /// </summary>
    public string? QueryType { get; set; }

    /// <summary>
    /// Informações adicionais sobre a query
    /// </summary>
    public Dictionary<string, object?> AdditionalInfo { get; set; } = new();
}
