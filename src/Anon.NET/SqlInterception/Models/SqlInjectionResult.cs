namespace Anon.NET.SqlInterception.Models;

public class SqlInjectionResult
{
    public bool IsDetected { get; set; }
    public string? Pattern { get; set; }
    public SqlInjectionSeverity Severity { get; set; }
    public string? Parameter { get; set; }
}

public enum SqlInjectionSeverity
{
    Low,
    Medium,
    High
}
