using Conde.Reducao;
using Xunit;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Testes;

public class TermoTestes
{
    [Fact]
    public void OTamanhoContaOsNos()
    {
        Assert.Equal(1, V("x").Tamanho());
        Assert.Equal(2, L("x", V("x")).Tamanho());
        Assert.Equal(3, A(V("f"), V("x")).Tamanho());
        Assert.Equal(9, Combinadores.Omega().Tamanho());
    }

    /// <summary>
    /// As variaveis LIVRES sao as que nao estao ligadas por nenhum lambda, e a
    /// conta tem que entender o escondimento: em \x.x o x esta ligado.
    /// </summary>
    [Fact]
    public void AsLivresEntendemOEscondimento()
    {
        Assert.Equal(["x"], L("y", V("x")).Livres());
        Assert.Empty(L("x", V("x")).Livres());
        Assert.Equal(["x", "y"], A(V("x"), V("y")).Livres().OrderBy(n => n, StringComparer.Ordinal));

        // o x de dentro esta ligado, o de fora nao
        Assert.Equal(["x"], A(L("x", V("x")), V("x")).Livres());
    }

    [Fact]
    public void OsCombinadoresSaoFechados()
    {
        foreach (var combinador in new[] { Combinadores.I(), Combinadores.K(), Combinadores.S(), Combinadores.Y(), Combinadores.Omega() })
            Assert.True(combinador.Fechado(), combinador.ToString());

        Assert.False(V("x").Fechado());
    }

    [Fact]
    public void AEscritaEhLegivel()
    {
        Assert.Equal("x", V("x").ToString());
        Assert.Equal("\\x.x", L("x", V("x")).ToString());
        Assert.Equal("(\\x.x x) (\\x.x x)", Combinadores.Omega().ToString());
        Assert.Equal("f x", A(V("f"), V("x")).ToString());
    }

    /// <summary>
    /// A aplicacao associa a ESQUERDA: f g h e (f g) h, e nao f (g h). Sao
    /// termos diferentes.
    /// </summary>
    [Fact]
    public void AAplicacaoAssociaAEsquerda()
    {
        var cadeia = A(V("f"), V("g"), V("h"));

        Assert.IsType<Aplicacao>(cadeia);
        Assert.IsType<Aplicacao>(((Aplicacao)cadeia).Alvo);
        Assert.NotEqual(cadeia, new Aplicacao(V("f"), new Aplicacao(V("g"), V("h"))));
    }

    [Fact]
    public void OsAtalhosDeVariosParametrosSaoLambdasAninhados()
    {
        Assert.Equal(L("x", L("y", V("x"))), L("x", "y", V("x")));
        Assert.Equal(L("x", L("y", L("z", V("x")))), L("x", "y", "z", V("x")));
    }

    [Fact]
    public void AProfundidadeEhOAninhamento()
    {
        Assert.Equal(1, V("x").Profundidade());
        Assert.Equal(3, L("x", "y", V("x")).Profundidade());
        Assert.Equal(4, Combinadores.Omega().Profundidade());
    }

    /// <summary>
    /// O numeral de Igreja tem o tamanho que ele deve ter: dois lambdas mais n
    /// aplicacoes mais n+1 variaveis.
    /// </summary>
    [Fact]
    public void ONumeralTemOTamanhoEsperado()
    {
        for (var n = 0; n <= 5; n++)
            Assert.Equal(2 + 2 * n + 1, Igreja.Numero(n).Tamanho());
    }

    [Fact]
    public void OsTermosSaoComparadosPorEstrutura()
    {
        Assert.Equal(L("x", V("x")), L("x", V("x")));
        Assert.NotEqual(L("x", V("x")), L("y", V("y")));
        Assert.Equal(V("x"), V("x"));
    }
}
