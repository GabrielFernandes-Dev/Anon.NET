using Anon.NET.Anonimization.Attributes;
using Anon.NET.Anonimization.Interfaces;
using System.Reflection;

namespace Anon.NET.Anonimization.Methods;

internal class KAnonymityMethod : IKAnonymityMethod
{
    public Dictionary<string, Dictionary<int, object>>? AgrupamentosNaoFinalizados { get; set; }

    public Dictionary<string, List<object>>? EntidadesPendentesDeAtualizacao { get; set; }

    public Action<List<object>>? AtualizarEntidadesNoBanco { get; set; }

    public void Process(object value, List<PropertyInfo>? quasiIdentifierAttribute, int K)
    {
        string tipoEntidade = value.GetType().Name;

        if (AgrupamentosNaoFinalizados == null)
        {
            AgrupamentosNaoFinalizados = new Dictionary<string, Dictionary<int, object>>
            {
                { nameof(value), new Dictionary<int, object> { { 0, value } } }
            };
            return;
        }

        if (EntidadesPendentesDeAtualizacao == null)
        {
            EntidadesPendentesDeAtualizacao = new Dictionary<string, List<object>>();
        }
        if (!EntidadesPendentesDeAtualizacao.ContainsKey(tipoEntidade))
        {
            EntidadesPendentesDeAtualizacao[tipoEntidade] = new List<object>();
        }

        Dictionary<int, object> agrupamentoEntidadeAtual = AgrupamentosNaoFinalizados[nameof(value)];
        List<object> entidadesPendentes = EntidadesPendentesDeAtualizacao[tipoEntidade];

        if (agrupamentoEntidadeAtual == null)
        {
            agrupamentoEntidadeAtual = new Dictionary<int, object>
            {
                { 0, value }
            };
        }

        agrupamentoEntidadeAtual.Add(agrupamentoEntidadeAtual.Count, value);

        var idProperty = ObterPropriedadeId(value.GetType());
        if (idProperty != null)
        {
            var idValue = idProperty.GetValue(value);
            if (idValue != null && !entidadesPendentes.Contains(idValue))
            {
                entidadesPendentes.Add(idValue);
                Console.WriteLine($"[K-Anonymity] ID {idValue} adicionado à lista de pendentes para {tipoEntidade}");
            }
        }

        int limiteMinimo = K * 3;

        if (agrupamentoEntidadeAtual.Count < limiteMinimo)
            return;

        var valoresQuasiIdentificadores = new List<Dictionary<string, object?>>();

        foreach (var item in agrupamentoEntidadeAtual)
        {
            var valoresEntidade = new Dictionary<string, object?>();

            if (quasiIdentifierAttribute != null)
            {
                foreach (var propertyInfo in quasiIdentifierAttribute)
                {
                    object? valorPropriedade = propertyInfo.GetValue(item.Value);
                    if (!TryConvertToNumeric(valorPropriedade, out double valorPropriedadeConvertido))
                        throw new InvalidOperationException("Os Quasi Identificadores devem ser apenas números ou conversíveis para números");

                    Console.WriteLine($"Propriedade: {propertyInfo.Name}, Valor: {valorPropriedadeConvertido}");
                    valoresEntidade[propertyInfo.Name] = valorPropriedadeConvertido;
                }
            }
            valoresQuasiIdentificadores.Add(valoresEntidade);
        }

        var grupos = CalcularMDAV(valoresQuasiIdentificadores, K);

        AplicarGeneralizacao(grupos, agrupamentoEntidadeAtual, quasiIdentifierAttribute);
        if (AtualizarEntidadesNoBanco != null && entidadesPendentes.Count > 0)
        {
            Console.WriteLine($"[K-Anonymity] Atualizando {agrupamentoEntidadeAtual.Count} entidades no banco de dados...");

            var entidadesParaAtualizar = agrupamentoEntidadeAtual.Values.ToList();
            AtualizarEntidadesNoBanco(entidadesParaAtualizar);

            Console.WriteLine($"[K-Anonymity] Entidades atualizadas com sucesso!");
        }
        else
        {
            Console.WriteLine("[K-Anonymity] AVISO: Nenhum callback de atualização configurado ou não há entidades pendentes!");
        }

        agrupamentoEntidadeAtual.Clear();
        entidadesPendentes.Clear();
        return;
    }

    private PropertyInfo? ObterPropriedadeId(Type tipoEntidade)
    {
        var idProperty = tipoEntidade.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (idProperty != null)
            return idProperty;

        idProperty = tipoEntidade.GetProperty($"{tipoEntidade.Name}Id", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (idProperty != null)
            return idProperty;

        foreach (var property in tipoEntidade.GetProperties())
        {
            var keyAttribute = property.GetCustomAttribute(typeof(System.ComponentModel.DataAnnotations.KeyAttribute));
            if (keyAttribute != null)
                return property;
        }

        Console.WriteLine($"[K-Anonymity] AVISO: Nenhuma propriedade ID encontrada para o tipo {tipoEntidade.Name}");
        return null;
    }

    private List<List<Dictionary<string, object?>>> CalcularMDAV(
        List<Dictionary<string, object?>> valoresQuasiIdentificadores,
        int K)
    {
        var grupos = new List<List<Dictionary<string, object?>>>();
        var pontosRestantes = new List<Dictionary<string, object?>>(valoresQuasiIdentificadores);

        while (pontosRestantes.Count >= 2 * K)
        {
            var centroide = CalcularCentroide(pontosRestantes);

            var pontoR = EncontrarPontoMaisDistante(pontosRestantes, centroide);

            var pontoS = EncontrarPontoMaisDistanteDoRegistro(pontosRestantes, pontoR);

            var (grupoR, grupoS) = CriarDoisGrupos(pontosRestantes, pontoR, pontoS, K);

            grupos.Add(grupoR);
            grupos.Add(grupoS);

            foreach (var ponto in grupoR.Concat(grupoS))
            {
                pontosRestantes.Remove(ponto);
            }
        }

        if (pontosRestantes.Count >= K)
        {
            grupos.Add(pontosRestantes);
        }
        else if (pontosRestantes.Count > 0)
        {
            DistribuirPontosRestantes(grupos, pontosRestantes);
        }

        Console.WriteLine($"MDAV: Criados {grupos.Count} grupos com K={K}");
        return grupos;
    }

    private Dictionary<string, object?> EncontrarPontoMaisDistante(
        List<Dictionary<string, object?>> pontos,
        Dictionary<string, double> centroide)
    {
        if (pontos.Count == 0)
            throw new InvalidOperationException("Lista de pontos vazia");

        Dictionary<string, object?>? pontoMaisDistante = null;
        double maiorDistancia = double.MinValue;

        foreach (var ponto in pontos)
        {
            double distancia = CalcularDistancia(ponto, centroide);

            if (distancia > maiorDistancia)
            {
                maiorDistancia = distancia;
                pontoMaisDistante = ponto;
            }
        }

        if (pontoMaisDistante == null)
            throw new InvalidOperationException("Não foi possível encontrar o ponto mais distante");

        Console.WriteLine($"Ponto mais distante encontrado com distância: {maiorDistancia:F2}");
        return pontoMaisDistante;
    }

    private Dictionary<string, object?> EncontrarPontoMaisDistanteDoRegistro(
        List<Dictionary<string, object?>> pontos,
        Dictionary<string, object?> registroReferencia)
    {
        if (pontos.Count == 0)
            throw new InvalidOperationException("Lista de pontos vazia");

        Dictionary<string, object?>? pontoMaisDistante = null;
        double maiorDistancia = double.MinValue;

        foreach (var ponto in pontos)
        {
            if (ponto == registroReferencia)
                continue;

            double distancia = CalcularDistanciaEntrePontos(ponto, registroReferencia);

            if (distancia > maiorDistancia)
            {
                maiorDistancia = distancia;
                pontoMaisDistante = ponto;
            }
        }

        if (pontoMaisDistante == null)
            throw new InvalidOperationException("Não foi possível encontrar o ponto mais distante");

        Console.WriteLine($"Ponto mais distante do registro encontrado com distância: {maiorDistancia:F2}");
        return pontoMaisDistante;
    }

    private (List<Dictionary<string, object?>>, List<Dictionary<string, object?>>) CriarDoisGrupos(
        List<Dictionary<string, object?>> pontos,
        Dictionary<string, object?> pontoR,
        Dictionary<string, object?> pontoS,
        int K)
    {
        var distanciasParaR = new Dictionary<Dictionary<string, object?>, double>();
        var distanciasParaS = new Dictionary<Dictionary<string, object?>, double>();

        foreach (var ponto in pontos)
        {
            distanciasParaR[ponto] = CalcularDistanciaEntrePontos(ponto, pontoR);
            distanciasParaS[ponto] = CalcularDistanciaEntrePontos(ponto, pontoS);
        }

        var pontosOrdenadosR = distanciasParaR
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        var pontosOrdenadosS = distanciasParaS
            .OrderBy(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        var grupoR = new List<Dictionary<string, object?>>();
        var pontosUsados = new HashSet<Dictionary<string, object?>>();

        foreach (var ponto in pontosOrdenadosR)
        {
            if (grupoR.Count >= K) break;

            grupoR.Add(ponto);
            pontosUsados.Add(ponto);
        }

        var grupoS = new List<Dictionary<string, object?>>();

        foreach (var ponto in pontosOrdenadosS)
        {
            if (grupoS.Count >= K) break;

            if (!pontosUsados.Contains(ponto))
            {
                grupoS.Add(ponto);
            }
        }

        Console.WriteLine($"Grupo R criado com {grupoR.Count} pontos");
        Console.WriteLine($"Grupo S criado com {grupoS.Count} pontos");

        return (grupoR, grupoS);
    }

    private void DistribuirPontosRestantes(
        List<List<Dictionary<string, object?>>> grupos,
        List<Dictionary<string, object?>> pontosRestantes)
    {
        if (grupos.Count == 0)
        {
            Console.WriteLine("Aviso: Não há grupos para distribuir os pontos restantes");
            return;
        }

        Console.WriteLine($"Distribuindo {pontosRestantes.Count} pontos restantes em {grupos.Count} grupos");

        foreach (var ponto in pontosRestantes)
        {
            int indiceGrupoMaisProximo = 0;
            double menorDistancia = double.MaxValue;

            for (int i = 0; i < grupos.Count; i++)
            {
                var centroideGrupo = CalcularCentroide(grupos[i]);
                double distancia = CalcularDistancia(ponto, centroideGrupo);

                if (distancia < menorDistancia)
                {
                    menorDistancia = distancia;
                    indiceGrupoMaisProximo = i;
                }
            }

            grupos[indiceGrupoMaisProximo].Add(ponto);
            Console.WriteLine($"Ponto adicionado ao grupo {indiceGrupoMaisProximo} (distância: {menorDistancia:F2})");
        }
    }

    private double CalcularDistanciaEntrePontos(
        Dictionary<string, object?> ponto1,
        Dictionary<string, object?> ponto2)
    {
        double distancia = 0;

        foreach (var propriedade in ponto1.Keys)
        {
            if (ponto2.ContainsKey(propriedade))
            {
                if (double.TryParse(ponto1[propriedade]?.ToString(), out double valor1) &&
                    double.TryParse(ponto2[propriedade]?.ToString(), out double valor2))
                {
                    double diff = valor1 - valor2;
                    distancia += diff * diff;
                }
            }
        }

        return Math.Sqrt(distancia);
    }

    private void AplicarGeneralizacao(
        List<List<Dictionary<string, object?>>> grupos,
        Dictionary<int, object> entidadesOriginais,
        List<PropertyInfo>? quasiIdentifierAttribute)
    {
        if (quasiIdentifierAttribute == null) return;

        Console.WriteLine($"\nAplicando generalização a {grupos.Count} grupos...");

        foreach (var grupo in grupos)
        {
            var centroideGrupo = CalcularCentroide(grupo);

            Console.WriteLine($"\nGrupo com {grupo.Count} registros:");
            foreach (var propriedade in centroideGrupo.Keys)
            {
                Console.WriteLine($"  {propriedade}: {centroideGrupo[propriedade]:F2}");
            }

            foreach (var valoresQuasi in grupo)
            {
                var entidadeOriginal = EncontrarEntidadeOriginal(valoresQuasi, entidadesOriginais, quasiIdentifierAttribute);

                if (entidadeOriginal != null)
                {
                    foreach (var prop in quasiIdentifierAttribute)
                    {
                        if (centroideGrupo.ContainsKey(prop.Name))
                        {
                            object valorGeneralizado = ConverterParaTipoPropriedade(
                                centroideGrupo[prop.Name],
                                prop.PropertyType);

                            prop.SetValue(entidadeOriginal, valorGeneralizado);
                        }
                    }
                }
            }
        }

        Console.WriteLine("\nGeneralização aplicada com sucesso!");
    }

    private object? EncontrarEntidadeOriginal(
        Dictionary<string, object?> valoresQuasi,
        Dictionary<int, object> entidadesOriginais,
        List<PropertyInfo> quasiIdentifierAttribute)
    {
        foreach (var entidade in entidadesOriginais.Values)
        {
            bool match = true;

            foreach (var prop in quasiIdentifierAttribute)
            {
                var valorEntidade = prop.GetValue(entidade);
                var valorQuasi = valoresQuasi[prop.Name];

                // Compara os valores
                if (!ValoresIguais(valorEntidade, valorQuasi))
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return entidade;
        }

        return null;
    }

    private bool ValoresIguais(object? valor1, object? valor2)
    {
        if (valor1 == null && valor2 == null) return true;
        if (valor1 == null || valor2 == null) return false;

        if (double.TryParse(valor1.ToString(), out double d1) &&
            double.TryParse(valor2.ToString(), out double d2))
        {
            return Math.Abs(d1 - d2) < 0.0001; // Tolerância para comparação de doubles
        }

        return valor1.Equals(valor2);
    }

    private object ConverterParaTipoPropriedade(double valor, Type tipoPropriedade)
    {
        // Remove Nullable se existir
        Type tipoBase = Nullable.GetUnderlyingType(tipoPropriedade) ?? tipoPropriedade;

        if (tipoBase == typeof(int))
            return (int)Math.Round(valor);

        if (tipoBase == typeof(long))
            return (long)Math.Round(valor);

        if (tipoBase == typeof(short))
            return (short)Math.Round(valor);

        if (tipoBase == typeof(byte))
            return (byte)Math.Round(valor);

        if (tipoBase == typeof(decimal))
            return (decimal)valor;

        if (tipoBase == typeof(float))
            return (float)valor;

        if (tipoBase == typeof(double))
            return valor;
        if (tipoBase == typeof(uint))
            return (uint)Math.Round(Math.Abs(valor));

        if (tipoBase == typeof(ulong))
            return (ulong)Math.Round(Math.Abs(valor));

        if (tipoBase == typeof(ushort))
            return (ushort)Math.Round(Math.Abs(valor));

        return valor;
    }

    private Dictionary<string, double> CalcularCentroide(List<Dictionary<string, object?>> valores)
    {
        var centroide = new Dictionary<string, double>();

        if (valores.Count == 0) return centroide;

        foreach (var propriedade in valores[0].Keys)
        {
            double soma = 0;
            int count = 0;

            foreach (var registro in valores)
            {
                if (registro[propriedade] != null)
                {
                    if (double.TryParse(registro[propriedade]?.ToString(), out double valor))
                    {
                        soma += valor;
                        count++;
                    }
                }
            }

            centroide[propriedade] = count > 0 ? soma / count : 0;
        }

        return centroide;
    }

    private double CalcularDistancia(Dictionary<string, object?> ponto, Dictionary<string, double> centroide)
    {
        double distancia = 0;

        foreach (var propriedade in centroide.Keys)
        {
            if (ponto.ContainsKey(propriedade) && ponto[propriedade] != null)
            {
                if (double.TryParse(ponto[propriedade]?.ToString(), out double valor))
                {
                    double diff = valor - centroide[propriedade];
                    distancia += diff * diff;
                }
            }
        }

        return Math.Sqrt(distancia);
    }

    private bool TryConvertToNumeric(object? value, out double numericValue)
    {
        numericValue = 0;

        if (value == null)
            return false;

        try
        {
            numericValue = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
