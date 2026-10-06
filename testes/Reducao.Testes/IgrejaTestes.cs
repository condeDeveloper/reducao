using Conde.Reducao;
using Xunit;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Testes;

public class IgrejaTestes
{
    /// <summary>
    /// Todas as contas dao o resultado conhecido DE FORA: ninguem precisa
    /// confiar no redutor para saber que dois mais tres e cinco.
    /// </summary>
    [Fact]
    public void TodasAsContasDaoOResultadoConhecido()
    {
        foreach (var (nome, termo, esperado) in Igreja.Contas())
        {
            var r = Reducao.Reduzir(termo, Estrategia.Normal, 100_000);

            Assert.True(r.Terminou, $"{nome} nao terminou");
            Assert.Equal(esperado, Igreja.Ler(r.Final));
        }
    }

    /// <summary>E dao o mesmo resultado sob ordem aplicativa.</summary>
    [Fact]
    public void EDaoOMesmoSobOrdemAplicativa()
    {
        foreach (var (nome, termo, esperado) in Igreja.Contas())
        {
            var r = Reducao.Reduzir(termo, Estrategia.Aplicativa, 100_000);

            Assert.True(r.Terminou, $"{nome} nao terminou");
            Assert.Equal(esperado, Igreja.Ler(r.Final));
        }
    }

    [Fact]
    public void OsLogicosDaoOEsperado()
    {
        foreach (var (nome, termo, esperado) in Igreja.Logicos())
        {
            var r = Reducao.Reduzir(termo, Estrategia.Normal, 10_000);
            Assert.Equal(esperado, Igreja.LerLogico(r.Final));
        }
    }

    /// <summary>
    /// Escrever e ler um numeral de volta devolve o mesmo numero, de zero a
    /// vinte.
    /// </summary>
    [Fact]
    public void EscreverELerDevolveOMesmoNumero()
    {
        for (var n = 0; n <= 20; n++) Assert.Equal(n, Igreja.Ler(Igreja.Numero(n)));
    }

    /// <summary>
    /// E a leitura RECUSA o que nao e numeral, em vez de inventar um numero.
    /// </summary>
    [Fact]
    public void ALeituraRecusaOQueNaoEhNumeral()
    {
        Assert.Null(Igreja.Ler(V("x")));
        Assert.Null(Igreja.Ler(L("x", V("x"))));
        Assert.Null(Igreja.Ler(Igreja.Verdadeiro()));
        Assert.Null(Igreja.LerLogico(Igreja.Numero(3)));
    }

    /// <summary>
    /// O numeral e a funcao que aplica f n vezes: aplicando ele a uma funcao de
    /// verdade, a funcao e aplicada n vezes.
    /// </summary>
    [Fact]
    public void ONumeralAplicaAFuncaoNVezes()
    {
        for (var n = 0; n <= 4; n++)
        {
            var termo = A(Igreja.Numero(n), V("g"), V("a"));
            var r = Reducao.Reduzir(termo, Estrategia.Normal, 10_000);

            // o resultado e g (g (... (g a)))
            var atual = r.Final;
            var quantos = 0;

            while (atual is Aplicacao a && a.Alvo is Variavel { Nome: "g" }) { quantos++; atual = a.Argumento; }

            Assert.Equal(n, quantos);
            Assert.Equal(V("a"), atual);
        }
    }

    /// <summary>A soma e comutativa, conferida nos numerais.</summary>
    [Fact]
    public void ASomaEhComutativa()
    {
        for (var a = 0; a <= 3; a++)
            for (var b = 0; b <= 3; b++)
            {
                var ida = Reducao.Reduzir(A(Igreja.Soma(), Igreja.Numero(a), Igreja.Numero(b)), Estrategia.Normal, 50_000);
                var volta = Reducao.Reduzir(A(Igreja.Soma(), Igreja.Numero(b), Igreja.Numero(a)), Estrategia.Normal, 50_000);

                Assert.Equal(a + b, Igreja.Ler(ida.Final));
                Assert.Equal(Igreja.Ler(ida.Final), Igreja.Ler(volta.Final));
            }
    }

    /// <summary>E o produto tambem, que nao e obvio na forma dos termos.</summary>
    [Fact]
    public void OProdutoEhComutativo()
    {
        for (var a = 0; a <= 3; a++)
            for (var b = 0; b <= 3; b++)
            {
                var ida = Reducao.Reduzir(A(Igreja.Produto(), Igreja.Numero(a), Igreja.Numero(b)), Estrategia.Normal, 50_000);
                Assert.Equal(a * b, Igreja.Ler(ida.Final));
            }
    }

    /// <summary>
    /// A POTENCIA e o termo mais curto de todos: m elevado a n e simplesmente n
    /// aplicado a m.
    /// </summary>
    [Fact]
    public void APotenciaEhOTermoMaisCurto()
    {
        Assert.True(Igreja.Potencia().Tamanho() < Igreja.Soma().Tamanho());
        Assert.True(Igreja.Potencia().Tamanho() < Igreja.Produto().Tamanho());

        for (var b = 1; b <= 3; b++)
            for (var e = 1; e <= 3; e++)
            {
                var r = Reducao.Reduzir(A(Igreja.Potencia(), Igreja.Numero(b), Igreja.Numero(e)), Estrategia.Normal, 100_000);
                Assert.Equal((int)Math.Pow(b, e), Igreja.Ler(r.Final));
            }
    }

    /// <summary>
    /// O EXPOENTE ZERO me corrigiu: m elevado a zero NAO reduz para o numeral
    /// um.
    ///
    /// Ele reduz para a identidade, porque "zero aplicado a m" e "aplicar m zero
    /// vezes", que e nao fazer nada. A identidade e o numeral um sao a mesma
    /// funcao quando aplicadas a alguma coisa, e NAO sao o mesmo termo: \x.x
    /// contra \f.\x.f x.
    ///
    /// A diferenca entre as duas e a regra ETA, que o calculo lambda puro nao
    /// tem. A leitura de numeral aqui e por forma, nao por comportamento, entao
    /// ela devolve nulo, e isso esta certo.
    /// </summary>
    [Fact]
    public void OExpoenteZeroDaAIdentidadeENaoONumeralUm()
    {
        var r = Reducao.Reduzir(A(Igreja.Potencia(), Igreja.Numero(5), Igreja.Numero(0)), Estrategia.Normal, 10_000);

        Assert.Null(Igreja.Ler(r.Final));
        Assert.True(Substituicao.Iguais(Combinadores.I(), r.Final), $"deu {r.Final}");

        // e as duas se comportam igual quando aplicadas
        var comIdentidade = Reducao.Reduzir(A(r.Final, V("g"), V("a")), Estrategia.Normal, 10_000);
        var comUm = Reducao.Reduzir(A(Igreja.Numero(1), V("g"), V("a")), Estrategia.Normal, 10_000);

        Assert.Equal(comUm.Final, comIdentidade.Final);
    }

    /// <summary>
    /// O condicional nem precisa existir: o proprio logico escolhe qual dos dois
    /// devolver.
    /// </summary>
    [Fact]
    public void OLogicoEhOProprioCondicional()
    {
        var comVerdadeiro = Reducao.Reduzir(A(Igreja.Verdadeiro(), V("a"), V("b")), Estrategia.Normal);
        var comFalso = Reducao.Reduzir(A(Igreja.Falso(), V("a"), V("b")), Estrategia.Normal);

        Assert.Equal(V("a"), comVerdadeiro.Final);
        Assert.Equal(V("b"), comFalso.Final);
    }

    [Fact]
    public void OSucessorAndaUmDeCadaVez()
    {
        var atual = Igreja.Numero(0);

        for (var n = 1; n <= 5; n++)
        {
            atual = Reducao.Reduzir(A(Igreja.Sucessor(), atual), Estrategia.Normal, 10_000).Final;
            Assert.Equal(n, Igreja.Ler(atual));
        }
    }
}
