using static Conde.Reducao.Escrever;

namespace Conde.Reducao;

/// <summary>
/// Os NUMERAIS DE IGREJA, que mostram que a linguagem de tres formas basta.
///
/// Nao ha numero no calculo lambda, e nao falta: o numero tres vira "a funcao
/// que aplica alguma coisa tres vezes". Com essa ideia saem a soma, o produto, a
/// potencia e o teste de zero, e todos eles sao termos, nao primitivas.
///
/// Eles existem aqui por um motivo pratico: eles dao CONTAS DE VERDADE para
/// medir. Reduzir "dois mais tres" e ver sair "cinco" e uma conferencia que nao
/// depende de confiar em redutor nenhum, porque a resposta e conhecida de fora.
/// </summary>
public static class Igreja
{
    /// <summary>O numeral n: a funcao que aplica f n vezes a x.</summary>
    public static Termo Numero(int n)
    {
        Termo corpo = V("x");
        for (var i = 0; i < n; i++) corpo = A(V("f"), corpo);

        return L("f", "x", corpo);
    }

    /// <summary>Le um numeral de volta, contando quantas vezes f aparece.</summary>
    public static int? Ler(Termo termo)
    {
        ArgumentNullException.ThrowIfNull(termo);

        if (termo is not Funcao { Corpo: Funcao dentro } fora) return null;

        var f = fora.Parametro;
        var x = dentro.Parametro;
        var atual = dentro.Corpo;
        var quantos = 0;

        while (atual is Aplicacao a)
        {
            if (a.Alvo is not Variavel va || va.Nome != f) return null;

            quantos++;
            atual = a.Argumento;
        }

        return atual is Variavel vx && vx.Nome == x ? quantos : null;
    }

    /// <summary>O SUCESSOR: aplicar f mais uma vez.</summary>
    public static Termo Sucessor() => L("n", "f", "x", A(V("f"), A(V("n"), V("f"), V("x"))));

    /// <summary>A SOMA: aplicar f m vezes depois de aplicar n vezes.</summary>
    public static Termo Soma() => L("m", "n", L("f", "x", A(V("m"), V("f"), A(V("n"), V("f"), V("x")))));

    /// <summary>O PRODUTO: aplicar "n vezes f" m vezes.</summary>
    public static Termo Produto() => L("m", "n", L("f", A(V("m"), A(V("n"), V("f")))));

    /// <summary>A POTENCIA, que e a mais curta de todas: m aplicado a n.</summary>
    public static Termo Potencia() => L("m", "n", A(V("n"), V("m")));

    public static Termo Verdadeiro() => L("a", "b", V("a"));

    public static Termo Falso() => L("a", "b", V("b"));

    /// <summary>O SE, que nem precisa existir: o proprio logico escolhe.</summary>
    public static Termo Se() => L("c", "a", "b", A(V("c"), V("a"), V("b")));

    /// <summary>EH ZERO: aplicar "sempre falso" n vezes a verdadeiro.</summary>
    public static Termo EhZero() => L("n", A(V("n"), L("x", Falso()), Verdadeiro()));

    /// <summary>Le um logico de volta.</summary>
    public static bool? LerLogico(Termo termo)
    {
        ArgumentNullException.ThrowIfNull(termo);

        if (Substituicao.Iguais(termo, Verdadeiro())) return true;
        if (Substituicao.Iguais(termo, Falso())) return false;

        return null;
    }

    /// <summary>As contas de teste, com a resposta conhecida de fora.</summary>
    public static IEnumerable<(string Nome, Termo Termo, int Esperado)> Contas()
    {
        yield return ("sucessor de 3", A(Sucessor(), Numero(3)), 4);
        yield return ("2 mais 3", A(Soma(), Numero(2), Numero(3)), 5);
        yield return ("4 mais 0", A(Soma(), Numero(4), Numero(0)), 4);
        yield return ("3 vezes 4", A(Produto(), Numero(3), Numero(4)), 12);
        yield return ("2 elevado a 5", A(Potencia(), Numero(2), Numero(5)), 32);
        yield return ("3 elevado a 3", A(Potencia(), Numero(3), Numero(3)), 27);
        yield return ("(2 mais 3) vezes 2", A(Produto(), A(Soma(), Numero(2), Numero(3)), Numero(2)), 10);
    }

    /// <summary>Os testes logicos, com a resposta conhecida.</summary>
    public static IEnumerable<(string Nome, Termo Termo, bool Esperado)> Logicos()
    {
        yield return ("zero e zero", A(EhZero(), Numero(0)), true);
        yield return ("tres nao e zero", A(EhZero(), Numero(3)), false);
        yield return ("se verdadeiro", A(Se(), Verdadeiro(), Verdadeiro(), Falso()), true);
        yield return ("se falso", A(Se(), Falso(), Verdadeiro(), Falso()), false);
    }
}
