using Anon.NET.SqlInterception.Interfaces;
using Anon.NET.SqlInterception.Models;
using Serilog;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Anon.NET.SqlInterception;

public class SqlInterceptor : ISqlInterceptor
{
    private readonly ILogger _logger;

    public SqlInterceptor(ILogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void InterceptQuery(SqlQuery query)
    {
        _logger.Information(
            "SQL Query Executed: {QueryId} | Type: {QueryType} | Duration: {DurationMs}ms | Source: {Source}",
            query.Id,
            query.QueryType,
            query.DurationMs,
            query.Source
        );

        // Log da query com todas as informações para análise detalhada
        _logger.Debug(
            "SQL Details: {QueryDetails}",
            new
            {
                query.Id,
                query.CommandText,
                query.Parameters,
                query.Timestamp,
                query.DurationMs,
                query.Source,
                query.TransactionId,
                query.QueryType,
                query.AdditionalInfo
            }
        );
    }

    /// <inheritdoc />
    public SqlQuery ExtractQueryFromCommand(DbCommand command, string? transactionId = null)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        var match = Regex.Match(command.CommandText, @"/\* QueryId: ([0-9a-fA-F-]+) \*/");
        string commandText;
        if (match.Success && match.Groups.Count > 1)
            commandText = Regex.Replace(command.CommandText, @"/\* QueryId: [0-9a-fA-F-]+ \*/\r?\n", "");
        else
            commandText = command.CommandText;

        var queryType = DetermineQueryType(commandText);
        var parameters = ExtractParameters(command);

        return new SqlQuery
        {
            CommandText = command.CommandText,
            Parameters = parameters,
            QueryType = queryType,
            TransactionId = transactionId,
            Source = GetCallerInfo()
        };
    }

    /// <summary>
    /// Determina o tipo de query (SELECT, INSERT, etc.) a partir do texto SQL
    /// </summary>
    private static string DetermineQueryType(string commandText)
    {
        var trimmedCommand = commandText.TrimStart();

        if (trimmedCommand.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            return "SELECT";

        if (trimmedCommand.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            return "INSERT";

        if (trimmedCommand.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            return "UPDATE";

        if (trimmedCommand.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            return "DELETE";

        if (trimmedCommand.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase))
            return "CREATE";

        if (trimmedCommand.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase))
            return "ALTER";

        if (trimmedCommand.StartsWith("DROP", StringComparison.OrdinalIgnoreCase))
            return "DROP";

        return "UNKNOWN";
    }

    /// <summary>
    /// Extrai os parâmetros do comando SQL
    /// </summary>
    private static Dictionary<string, object?> ExtractParameters(DbCommand command)
    {
        var parameters = new Dictionary<string, object?>();

        foreach (DbParameter parameter in command.Parameters)
        {
            object? value = parameter.Value == DBNull.Value ? null : parameter.Value;
            parameters[parameter.ParameterName] = value;
        }

        return parameters;
    }

    /// <summary>
    /// Obtém informações sobre o método que chamou a query
    /// </summary>
    private static string GetCallerInfo()
    {
        // Usa stack trace para obter informações do chamador
        var stackTrace = new StackTrace(true);

        // Pula os primeiros frames que são do próprio interceptor
        for (int i = 0; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            var method = frame?.GetMethod();

            if (method == null) continue;

            var declaringType = method.DeclaringType;
            if (declaringType == null) continue;

            // Ignora frames do Sistema ou da implementação do interceptor
            if (declaringType.Namespace?.StartsWith("System") == true ||
                declaringType.Namespace?.StartsWith("Microsoft") == true ||
                declaringType.Namespace?.StartsWith("Anon.NET.SqlInterception") == true)
                continue;

            return $"{declaringType.FullName}.{method.Name}";
        }

        return "Unknown Source";
    }
}
