using static Conde.Reducao.Escrever;

namespace Conde.Reducao;

/// <summary>
/// Os COMBINADORES: os termos fechados que valem a pena ter nome.
///
/// Eles sao poucos e aparecem em tudo. Os tres primeiros, I, K e S, bastam para
/// escrever qualquer termo sem usar lambda nenhum, o que e um resultado
/// surpreendente e nao e o assunto aqui. O que interessa sao os dois ultimos:
/// OMEGA, que nao termina, e Y, que faz recursao existir numa linguagem sem
/// recursao.
/// </summary>
public static class Combinadores
{
    /// <summary>I: a identidade.</summary>
    public static Termo I() => L("x", V("x"));

    /// <summary>K: devolve o primeiro e joga o segundo fora.</summary>
    public static Termo K() => L("x", "y", V("x"));

    /// <summary>S: distribui o argumento pelos dois lados.</summary>
    public static Termo S() => L("f", "g", "x", A(V("f"), V("x"), A(V("g"), V("x"))));

    /// <summary>
    /// A AUTO-APLICACAO: a funcao que aplica o argumento a ele mesmo.
    ///
    /// Sozinha ela e inofensiva e e um valor pronto. Aplicada a si mesma, ela e
    /// o termo que nao termina.
    /// </summary>
    public static Termo Mockingbird() => L("x", A(V("x"), V("x")));

    /// <summary>
    /// OMEGA: a auto-aplicacao aplicada a si mesma.
    ///
    /// Ele reduz para ELE MESMO, em um passo, para sempre. Nao cresce, nao
    /// encolhe, nao chega a lugar nenhum: e o menor termo sem forma normal que
    /// existe, e cabe em oito caracteres.
    /// </summary>
    public static Termo Omega() => A(Mockingbird(), Mockingbird());

    /// <summary>
    /// O combinador Y, de CURRY: o ponto fixo.
    ///
    /// Ele e o que permite recursao numa linguagem em que nenhuma funcao tem
    /// nome e portanto nenhuma pode se chamar. Y f reduz para f (Y f), que reduz
    /// para f (f (Y f)), e assim por diante: a funcao recebe a si mesma de
    /// presente.
    ///
    /// Sob ordem APLICATIVA ele nao termina nunca, nem aplicado a uma funcao
    /// mansa: a estrategia insiste em terminar o argumento, e o argumento e uma
    /// torre infinita.
    /// </summary>
    public static Termo Y() =>
        L("f", A(L("x", A(V("f"), A(V("x"), V("x")))), L("x", A(V("f"), A(V("x"), V("x"))))));

    /// <summary>
    /// O combinador Z, que e o Y com uma protecao.
    ///
    /// A diferenca e um lambda a mais em volta da chamada recursiva, e ele
    /// segura a expansao: o Z funciona sob ordem aplicativa, onde o Y nao
    /// funciona. E a mesma ideia, atrasada de proposito.
    /// </summary>
    public static Termo Z() =>
        L("f", A(L("x", A(V("f"), L("v", A(V("x"), V("x"), V("v"))))),
                 L("x", A(V("f"), L("v", A(V("x"), V("x"), V("v")))))));

    /// <summary>
    /// O caso classico: uma funcao que joga o argumento fora, aplicada a um
    /// argumento que nao termina.
    ///
    /// A ordem normal responde na hora, porque nunca olha o argumento. A ordem
    /// aplicativa nao responde nunca, porque insiste em termina-lo primeiro.
    /// Essa e a diferenca inteira entre as duas, num termo de uma linha.
    /// </summary>
    public static Termo ConstanteComOmega() => A(K(), V("y"), Omega());

    /// <summary>
    /// Um termo que CRESCE a cada passo, em vez de repetir.
    ///
    /// Ele existe porque "nao terminou" tem duas caras: repetir para sempre e
    /// crescer para sempre. O limite de passos pega as duas; o limite de
    /// tamanho pega a segunda antes de a memoria acabar.
    /// </summary>
    public static Termo QueCresce()
    {
        var dobra = L("x", A(V("x"), V("x"), V("x")));
        return A(dobra, dobra);
    }

    /// <summary>
    /// O caso que mostra o desperdicio da ordem normal: um argumento caro usado
    /// tres vezes.
    ///
    /// A ordem normal copia o argumento para os tres lugares e reduz os tres; a
    /// preguicosa reduz uma vez e aponta.
    /// </summary>
    public static Termo ArgumentoCaroUsadoTresVezes()
    {
        var caro = A(Igreja.Produto(), Igreja.Numero(3), Igreja.Numero(4));
        return A(L("n", A(Igreja.Soma(), A(Igreja.Soma(), V("n"), V("n")), V("n"))), caro);
    }

    public static IEnumerable<(string Nome, Termo Termo)> Todos()
    {
        yield return ("I", I());
        yield return ("K", K());
        yield return ("S", S());
        yield return ("S K K", A(S(), K(), K()));
        yield return ("K y omega", ConstanteComOmega());
        yield return ("omega", Omega());
        yield return ("que cresce", QueCresce());
    }
}
