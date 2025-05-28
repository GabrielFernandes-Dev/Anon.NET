namespace Anon.NET.SqlInterception.Models;

public class SqlInjectionRegexPatterns
{
    public required string Pattern { get; set; }
    public SqlInjectionSeverity Severity { get; set; }

    public SqlInjectionRegexPatterns(string pattern, SqlInjectionSeverity severity)
    {
        Pattern = pattern;
        Severity = severity;
    }
}
