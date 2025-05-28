using Anon.NET.Anonimization.Attributes;

namespace Anon.NET.Sample.Api.Models;

[KAnonymity(3)]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [Anonymize(PropertyAnonymizationMethod.Hash)]
    public string Email { get; set; } = string.Empty;
    [QuasiIdentifier]
    public string CPF { get; set; } = string.Empty;
    public int Idade { get; set; }
    public int Salario { get; set; }
    public string? CEP { get; set; } = string.Empty;
}
