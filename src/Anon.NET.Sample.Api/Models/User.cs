using Anon.NET.Anonimization.Attributes;

namespace Anon.NET.Sample.Api.Models;

[KAnonymity(3)]
public class User
{
    public int Id { get; set; }

    // Nome: Randomização - gera nomes aleatórios
    [Anonymize(PropertyAnonymizationMethod.Randomize)]
    public string Name { get; set; } = string.Empty;

    // Email: Hash - transforma em hash irreversível
    [Anonymize(PropertyAnonymizationMethod.Hash)]
    public string Email { get; set; } = string.Empty;

    // CPF: Mascaramento personalizado - mostra apenas os 3 primeiros dígitos
    [Anonymize(PropertyAnonymizationMethod.Mask, 3, 11)]
    public string CPF { get; set; } = string.Empty;

    // Idade: Randomização - mantém faixa etária similar
    [Anonymize(PropertyAnonymizationMethod.Randomize)]
    public int Idade { get; set; }

    // Salário: Tokenização - permite reversibilidade se necessário
    [Anonymize(PropertyAnonymizationMethod.Tokenize)]
    public int Salario { get; set; }

    // CEP: Anulação - remove completamente o dado
    [Anonymize(PropertyAnonymizationMethod.Nullify)]
    public string? CEP { get; set; } = string.Empty;
}
