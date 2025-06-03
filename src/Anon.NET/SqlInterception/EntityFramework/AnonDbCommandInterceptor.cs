using Anon.NET.Anonimization.Interfaces;
using Anon.NET.SqlInterception.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Anon.NET.SqlInterception.EntityFramework;

public class AnonDbCommandInterceptor : DbCommandInterceptor, IMaterializationInterceptor
{
    private readonly ISqlInterceptor _sqlInterceptor;
    private readonly IHttpContextAccessor? _httpContextAccessor;


    public AnonDbCommandInterceptor(ISqlInterceptor sqlInterceptor, IHttpContextAccessor? httpContextAccessor = null)
    {
        _sqlInterceptor = sqlInterceptor ?? throw new ArgumentNullException(nameof(sqlInterceptor));
        _httpContextAccessor = httpContextAccessor;
    }

    // Intercepta execuções síncronas de comandos que retornam um reader
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        InterceptBeforeExecution(command);
        return result;
    }

    // Intercepta execuções assíncronas de comandos que retornam um reader
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        InterceptBeforeExecution(command);
        return ValueTask.FromResult(result);
    }

    // Intercepta execuções síncronas de comandos que não retornam valores
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        InterceptBeforeExecution(command);
        return result;
    }

    // Intercepta execuções assíncronas de comandos que não retornam valores
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        InterceptBeforeExecution(command);
        return ValueTask.FromResult(result);
    }

    // Intercepta execuções síncronas de comandos que retornam um escalar
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        InterceptBeforeExecution(command);
        return result;
    }

    // Intercepta execuções assíncronas de comandos que retornam um escalar
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        InterceptBeforeExecution(command);
        return ValueTask.FromResult(result);
    }

    // Intercepta após a execução para registrar o tempo de execução
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        InterceptAfterExecution(command, eventData.Duration.TotalMilliseconds);
        return result;
    }

    // Intercepta após a execução assíncrona para registrar o tempo de execução
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        InterceptAfterExecution(command, eventData.Duration.TotalMilliseconds);
        return ValueTask.FromResult(result);
    }

    // Intercepta após a execução de comandos que não retornam valores
    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        InterceptAfterExecution(command, eventData.Duration.TotalMilliseconds);
        return result;
    }

    // Intercepta após a execução assíncrona de comandos que não retornam valores
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        InterceptAfterExecution(command, eventData.Duration.TotalMilliseconds);
        return ValueTask.FromResult(result);
    }

    // Intercepta após a execução de comandos que retornam um escalar
    public override object ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        InterceptAfterExecution(command, eventData.Duration.TotalMilliseconds);
        return ValueTask.FromResult(result);
    }

    // Intercepta após a execução assíncrona de comandos que retornam um escalar
    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        // Captura e intercepta a query após a execução
        InterceptAfterExecution(command, eventData.Duration.TotalMilliseconds);
        return ValueTask.FromResult(result);
    }

    // Método auxiliar para interceptar antes da execução
    private void InterceptBeforeExecution(DbCommand command)
    {
        // Extrai o ID de transação do contexto HTTP, se disponível
        string? transactionId = null;
        if (_httpContextAccessor?.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Transaction-Id", out var transactionIdValues);
            transactionId = transactionIdValues.FirstOrDefault();
        }

        // Extrai informações da query
        var sqlQuery = _sqlInterceptor.ExtractQueryFromCommand(command, transactionId);

        // Guarda o ID da query como informação de rastreamento no DbCommand
        command.CommandText = $"/* QueryId: {sqlQuery.Id} */" + Environment.NewLine + command.CommandText;
    }

    // Método auxiliar para interceptar após a execução
    private void InterceptAfterExecution(DbCommand command, double durationMs)
    {
        string? queryIdStr = null;
        var match = Regex.Match(command.CommandText, @"/\* QueryId: ([0-9a-fA-F-]+) \*/");
        if (match.Success && match.Groups.Count > 1)
            queryIdStr = match.Groups[1].Value;

        if (!Guid.TryParse(queryIdStr, out var queryId))
            return;

        string? transactionId = null;
        if (_httpContextAccessor?.HttpContext != null)
        {
            _httpContextAccessor.HttpContext.Request.Headers.TryGetValue("X-Transaction-Id", out var transactionIdValues);
            transactionId = transactionIdValues.FirstOrDefault();
        }

        // Cria um objeto SqlQuery com as informações pós-execução
        var sqlQuery = _sqlInterceptor.ExtractQueryFromCommand(command, transactionId);

        // Ajusta as propriedades com os valores específicos dessa execução
        sqlQuery.Id = queryId;
        if (match.Success && match.Groups.Count > 1)
            sqlQuery.CommandText = Regex.Replace(command.CommandText, @"/\* QueryId: [0-9a-fA-F-]+ \*/\r?\n", "");
        sqlQuery.DurationMs = (long)durationMs;

        _sqlInterceptor.InterceptQuery(sqlQuery);
    }
}
