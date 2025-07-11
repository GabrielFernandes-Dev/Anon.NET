using Anon.NET.Core.Extensions;
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
    private readonly IServiceProvider? _serviceProvider;

    public SqlInterceptor(ILogger logger, IServiceProvider? serviceProvider = null)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void InterceptQuery(SqlQuery query)
    {
        var injectionResult = DetectSqlInjection(query.CommandText, query.Parameters);

        if (injectionResult.IsDetected)
        {
            _logger.Warning(
                "POTENTIAL SQL INJECTION DETECTED: {QueryId} | Pattern: {Pattern} | Severity: {Severity}",
                query.Id,
                injectionResult.Pattern,
                injectionResult.Severity
            );


            query.AdditionalInfo["SqlInjectionDetected"] = true;
            query.AdditionalInfo["SqlInjectionPattern"] = injectionResult.Pattern;
            query.AdditionalInfo["SqlInjectionSeverity"] = injectionResult.Severity.ToString();
            if (injectionResult.Parameter != null)
            {
                query.AdditionalInfo["SqlInjectionParameter"] = injectionResult.Parameter;
            }
        }

        _logger.Information(
            "SQL Query Executed: {QueryId} | Type: {QueryType} | Duration: {DurationMs}ms | Source: {Source}",
            query.Id,
            query.QueryType,
            query.DurationMs,
            query.Source
        );

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

        if (_serviceProvider != null)
        {
            AnonDashboardExtensions.LogQueryForDashboard(query, _serviceProvider);
        }
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
    /// Detecta possíveis tentativas de SQL Injection
    /// </summary>
    /// <param name="commandText">Texto do comando SQL</param>
    /// <param name="parameters">Parâmetros do comando</param>
    /// <returns>Resultado da detecção</returns>
    public SqlInjectionResult DetectSqlInjection(string commandText, Dictionary<string, object?> parameters)
    {
        var result = new SqlInjectionResult { IsDetected = false };

        // Padrões comuns de SQL Injection
        var patterns = new List<SqlInjectionRegexPatterns>
        {
            new(@"'.*--", SqlInjectionSeverity.Low) { Pattern = @"'.*--" },
            new(@"'.*OR.*'.*'.*'", SqlInjectionSeverity.High) { Pattern = @"'.*OR.*'.*'.*'" },
            new(@"'.*AND.*'.*'.*'", SqlInjectionSeverity.Medium) { Pattern = @"'.*AND.*'.*'.*'"},
            new(@".*UNION.*SELECT.*", SqlInjectionSeverity.Medium) { Pattern = @".*UNION.*SELECT.*" },
            new(@".*DROP.*TABLE.*", SqlInjectionSeverity.High) { Pattern = @".*DROP.*TABLE.*" },
            new(@".*EXEC.*sp_.*", SqlInjectionSeverity.Critical) { Pattern = @".*EXEC.*sp_.*" },
            new(@".*xp_cmdshell.*", SqlInjectionSeverity.Critical) { Pattern = @".*xp_cmdshell.*" }
        };

        // Verifica se o comando SQL contém algum dos padrões
        foreach (var pattern in patterns.OrderByDescending(p => p.Severity))
        {
            if (Regex.IsMatch(commandText, pattern.Pattern, RegexOptions.IgnoreCase))
            {
                result.IsDetected = true;
                result.Pattern = pattern.Pattern;
                result.Severity = pattern.Severity;
                break;
            }
        }

        // Verifica os parâmetros em busca de padrões suspeitos
        if (!result.IsDetected && parameters.Any())
        {
            foreach (var param in parameters)
            {
                if (param.Value is string paramValue && !string.IsNullOrEmpty(paramValue))
                {
                    foreach (var pattern in patterns)
                    {
                        if (Regex.IsMatch(paramValue, pattern.Pattern, RegexOptions.IgnoreCase))
                        {
                            result.IsDetected = true;
                            result.Pattern = pattern.Pattern;
                            result.Severity = pattern.Severity;
                            result.Parameter = param.Key;
                            break;
                        }
                    }
                }

                if (result.IsDetected) break;
            }
        }

        return result;
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

            return $"{method.Module} {declaringType.FullName}.{method.Name}";
        }

        return "Unknown Source";
    }
}
