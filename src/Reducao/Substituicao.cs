namespace Conde.Reducao;

/// <summary>
/// A SUBSTITUICAO, que e a unica operacao do calculo lambda, e a armadilha dela.
///
/// Trocar uma variavel pelo que ela vale parece a coisa mais simples do mundo, e
/// tem um buraco com nome: a CAPTURA. Se o termo que entra tem uma variavel
/// livre com o mesmo nome de um parametro que esta no caminho, essa variavel
/// vira ligada e passa a significar outra coisa.
///
/// O exemplo cabe numa linha. Substituir y por x em \x.y deveria dar \x'.x, uma
/// funcao que joga o argumento fora e devolve o x de fora. Sem renomear, da
/// \x.x, que e a identidade: uma funcao completamente diferente.
///
/// Esta classe tem as DUAS versoes, e a medida mostra o estrago da errada. Nao e
/// uma diferenca sutil: ela troca a identidade pela constante.
/// </summary>
public static class Substituicao
{
    /// <summary>Quantas vezes a ultima substituicao precisou renomear.</summary>
    public sealed record Resultado(Termo Termo, int Renomeacoes);

    /// <summary>
    /// A substituicao CORRETA, que renomeia o parametro quando ele capturaria.
    /// </summary>
    public static Resultado Trocar(Termo onde, string variavel, Termo por)
    {
        ArgumentNullException.ThrowIfNull(onde);
        ArgumentNullException.ThrowIfNull(por);

        var renomeacoes = 0;
        var saida = Aplicar(onde, variavel, por, ref renomeacoes, true);

        return new Resultado(saida, renomeacoes);
    }

    /// <summary>
    /// A versao INGENUA, sem renomear nada.
    ///
    /// Ela existe para a medida mostrar o que acontece. Ela nao quebra, nao
    /// avisa e nao parece errada: ela devolve outro termo, calmamente.
    /// </summary>
    public static Termo TrocarIngenuamente(Termo onde, string variavel, Termo por)
    {
        var renomeacoes = 0;
        return Aplicar(onde, variavel, por, ref renomeacoes, false);
    }

    private static Termo Aplicar(Termo onde, string variavel, Termo por, ref int renomeacoes, bool cuidadosa)
    {
        switch (onde)
        {
            case Variavel v:
                return v.Nome == variavel ? por : v;

            case Aplicacao a:
                return new Aplicacao(Aplicar(a.Alvo, variavel, por, ref renomeacoes, cuidadosa),
                                     Aplicar(a.Argumento, variavel, por, ref renomeacoes, cuidadosa));

            case Funcao f:
                // O parametro esconde a variavel: dentro dele, o nome significa
                // outra coisa, e nao ha nada para trocar.
                if (f.Parametro == variavel) return f;

                if (!cuidadosa)
                    return new Funcao(f.Parametro, Aplicar(f.Corpo, variavel, por, ref renomeacoes, false));

                // A CAPTURA: o termo que entra tem o nome do parametro livre, e
                // entrar aqui dentro o tornaria ligado.
                if (por.Livres().Contains(f.Parametro))
                {
                    renomeacoes++;

                    // O nome fresco precisa evitar TRES conjuntos, e esquecer
                    // qualquer um deles da um erro silencioso:
                    //
                    // 1. as variaveis LIVRES do termo que entra, que e a razao
                    //    de renomear;
                    // 2. TODOS os nomes do corpo, livres ou ligados, porque a
                    //    renomeacao e feita sem cuidado e um nome ligado la
                    //    dentro captura o nome novo;
                    // 3. a propria VARIAVEL que esta sendo substituida.
                    //
                    // O terceiro foi o que me pegou. Substituindo x' numa funcao
                    // "\x.x", o nome fresco escolhido era exatamente x', e a
                    // renomeacao transformava "\x.x" em "\x'.x'", que a
                    // substituicao seguinte trocava pelo termo que entrava: a
                    // identidade virava uma constante.
                    //
                    // Isso dava nove discordancias em cinquenta mil termos
                    // sorteados, com o teorema de Church-Rosser parecendo
                    // falhar. Quem falhava era a substituicao.
                    var novo = NomeNovo(f.Parametro, por.Livres(), TodosOsNomes(f.Corpo), new[] { variavel });
                    var zero = 0;
                    var corpoRenomeado = Aplicar(f.Corpo, f.Parametro, new Variavel(novo), ref zero, false);

                    return new Funcao(novo, Aplicar(corpoRenomeado, variavel, por, ref renomeacoes, true));
                }

                return new Funcao(f.Parametro, Aplicar(f.Corpo, variavel, por, ref renomeacoes, true));

            default:
                return onde;
        }
    }

    /// <summary>
    /// Um nome que nao colide com nada do que esta em jogo.
    ///
    /// A linha de apostrofos e proposital: ela deixa o renomeado legivel na
    /// saida, e e assim que a diferenca entre \x.y e \x'.x aparece na tabela em
    /// vez de ficar escondida.
    /// </summary>
    public static string NomeNovo(string original, params IReadOnlyCollection<string>[] ocupados)
    {
        ArgumentNullException.ThrowIfNull(ocupados);

        var candidato = original;

        while (ocupados.Any(c => c.Contains(candidato))) candidato += "'";

        return candidato;
    }

    /// <summary>
    /// TODOS os nomes que aparecem num termo, livres ou ligados.
    ///
    /// E este o conjunto que o nome fresco precisa evitar, e nao so o dos
    /// livres.
    /// </summary>
    public static HashSet<string> TodosOsNomes(Termo termo)
    {
        ArgumentNullException.ThrowIfNull(termo);

        var nomes = new HashSet<string>(StringComparer.Ordinal);
        Recolher(termo, nomes);

        return nomes;
    }

    private static void Recolher(Termo termo, HashSet<string> nomes)
    {
        switch (termo)
        {
            case Variavel v:
                nomes.Add(v.Nome);
                break;

            case Funcao f:
                nomes.Add(f.Parametro);
                Recolher(f.Corpo, nomes);
                break;

            case Aplicacao a:
                Recolher(a.Alvo, nomes);
                Recolher(a.Argumento, nomes);
                break;
        }
    }

    /// <summary>
    /// Se dois termos sao IGUAIS a menos de renomear parametros, que e a
    /// igualdade que vale no calculo lambda.
    ///
    /// \x.x e \y.y sao a mesma funcao escrita com outra letra, e qualquer
    /// comparacao que diga que nao sao esta comparando a escrita, nao o termo.
    /// </summary>
    public static bool Iguais(Termo primeiro, Termo segundo)
    {
        ArgumentNullException.ThrowIfNull(primeiro);
        ArgumentNullException.ThrowIfNull(segundo);

        return Comparar(primeiro, segundo, [], [], 0);
    }

    /// <param name="nivel">
    /// A profundidade, contada a parte e nao pelo TAMANHO do mapa.
    ///
    /// Esse detalhe me custou nove discordancias em cinquenta mil termos
    /// sorteados, com o teorema de Church-Rosser parecendo falhar. Com
    /// sombreamento, num termo da forma "x, x de novo, y", o segundo x
    /// sobrescreve o primeiro no mapa e o tamanho dele NAO cresce: o y seguinte
    /// recebia o mesmo nivel do x, e duas variaveis diferentes passavam a ser
    /// consideradas iguais.
    ///
    /// Com o nivel contado a parte, cada lambda ganha um numero novo, haja
    /// sombreamento ou nao.
    /// </param>
    private static bool Comparar(Termo a, Termo b, Dictionary<string, int> deA, Dictionary<string, int> deB, int nivel)
    {
        switch (a, b)
        {
            case (Variavel va, Variavel vb):
                var temA = deA.TryGetValue(va.Nome, out var nivelA);
                var temB = deB.TryGetValue(vb.Nome, out var nivelB);

                // Duas variaveis ligadas sao iguais quando estao ligadas no
                // MESMO nivel; duas livres, quando tem o mesmo nome.
                if (temA != temB) return false;
                return temA ? nivelA == nivelB : va.Nome == vb.Nome;

            case (Funcao fa, Funcao fb):
                var dentroA = new Dictionary<string, int>(deA, StringComparer.Ordinal) { [fa.Parametro] = nivel };
                var dentroB = new Dictionary<string, int>(deB, StringComparer.Ordinal) { [fb.Parametro] = nivel };

                return Comparar(fa.Corpo, fb.Corpo, dentroA, dentroB, nivel + 1);

            case (Aplicacao aa, Aplicacao ab):
                return Comparar(aa.Alvo, ab.Alvo, deA, deB, nivel) &&
                       Comparar(aa.Argumento, ab.Argumento, deA, deB, nivel);

            default:
                return false;
        }
    }
}
