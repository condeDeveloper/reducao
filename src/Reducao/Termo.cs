namespace Conde.Reducao;

/// <summary>
/// Um TERMO do calculo lambda, que tem tres formas e nada mais.
///
/// Uma variavel, uma funcao de um argumento e uma aplicacao. Nao ha numero, nao
/// ha logico, nao ha condicional e nao ha recursao: tudo isso e construido com
/// essas tres formas, e construir e metade da graca.
///
/// O que falta de proposito e o que torna o assunto interessante: com tao pouco,
/// a unica coisa que se pode fazer e SUBSTITUIR, e a ordem em que as
/// substituicoes acontecem muda se o programa termina.
/// </summary>
public abstract record Termo
{
    /// <summary>O tamanho em nos, para as medidas de custo.</summary>
    public int Tamanho() => this switch
    {
        Funcao f => 1 + f.Corpo.Tamanho(),
        Aplicacao a => 1 + a.Alvo.Tamanho() + a.Argumento.Tamanho(),
        _ => 1,
    };

    /// <summary>As variaveis LIVRES, que sao as que nao estao ligadas por nenhum lambda.</summary>
    public HashSet<string> Livres()
    {
        var livres = new HashSet<string>(StringComparer.Ordinal);
        Juntar(this, livres);
        return livres;
    }

    private static void Juntar(Termo termo, HashSet<string> livres)
    {
        switch (termo)
        {
            case Variavel v:
                livres.Add(v.Nome);
                break;

            case Funcao f:
                var dentro = new HashSet<string>(StringComparer.Ordinal);
                Juntar(f.Corpo, dentro);
                dentro.Remove(f.Parametro);
                livres.UnionWith(dentro);
                break;

            case Aplicacao a:
                Juntar(a.Alvo, livres);
                Juntar(a.Argumento, livres);
                break;
        }
    }

    /// <summary>Se o termo e FECHADO: sem variavel livre nenhuma.</summary>
    public bool Fechado() => Livres().Count == 0;

    /// <summary>
    /// A PROFUNDIDADE de aninhamento, que importa porque a reducao e recursiva e
    /// a pilha e finita.
    /// </summary>
    public int Profundidade() => this switch
    {
        Funcao f => 1 + f.Corpo.Profundidade(),
        Aplicacao a => 1 + Math.Max(a.Alvo.Profundidade(), a.Argumento.Profundidade()),
        _ => 1,
    };
}

public sealed record Variavel(string Nome) : Termo
{
    public override string ToString() => Nome;
}

public sealed record Funcao(string Parametro, Termo Corpo) : Termo
{
    public override string ToString() => $"\\{Parametro}.{Corpo}";
}

public sealed record Aplicacao(Termo Alvo, Termo Argumento) : Termo
{
    // Os parenteses sao postos onde a leitura exige: a aplicacao associa a
    // esquerda, entao "f g h" e "(f g) h" e nao precisa de parenteses; um
    // lambda no lugar do alvo precisa.
    public override string ToString()
    {
        var alvo = Alvo is Funcao ? $"({Alvo})" : Alvo.ToString();
        var argumento = Argumento is Variavel ? Argumento.ToString() : $"({Argumento})";

        return $"{alvo} {argumento}";
    }
}

/// <summary>Os atalhos para escrever termo sem afogar em construtores.</summary>
public static class Escrever
{
    public static Termo V(string nome) => new Variavel(nome);

    public static Termo L(string parametro, Termo corpo) => new Funcao(parametro, corpo);

    /// <summary>Varios parametros de uma vez: L("x", "y", corpo) e \x.\y.corpo.</summary>
    public static Termo L(string primeiro, string segundo, Termo corpo) =>
        new Funcao(primeiro, new Funcao(segundo, corpo));

    public static Termo L(string primeiro, string segundo, string terceiro, Termo corpo) =>
        new Funcao(primeiro, new Funcao(segundo, new Funcao(terceiro, corpo)));

    /// <summary>A aplicacao em cadeia, que associa a esquerda.</summary>
    public static Termo A(Termo alvo, params Termo[] argumentos)
    {
        ArgumentNullException.ThrowIfNull(argumentos);
        return argumentos.Aggregate(alvo, (atual, argumento) => new Aplicacao(atual, argumento));
    }
}
