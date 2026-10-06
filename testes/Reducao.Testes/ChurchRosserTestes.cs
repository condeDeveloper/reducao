using Conde.Reducao;
using Xunit;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Testes;

/// <summary>
/// CHURCH e ROSSER, que e o juiz do repositorio: se um termo tem forma normal,
/// ela e UNICA, independente da ordem em que os redexes foram escolhidos.
/// </summary>
public class ChurchRosserTestes
{
    /// <summary>
    /// O ACHADO CENTRAL: zero discordancias em cinquenta mil termos sorteados
    /// sem nenhum cuidado.
    /// </summary>
    [Fact]
    public void NenhumTermoSorteadoDiscorda()
    {
        var (total, terminaram, discordaram) = ChurchRosser.Sortear(7, 20_000);

        Assert.Equal(0, discordaram);
        Assert.True(terminaram > total / 2, $"so {terminaram} de {total} chegaram em forma normal");
    }

    /// <summary>E com qualquer semente, nao so com a que deu sorte.</summary>
    [Fact]
    public void EhZeroComQualquerSemente()
    {
        foreach (var semente in new ulong[] { 1, 42, 999, 12345 })
            Assert.Equal(0, ChurchRosser.Sortear(semente, 3_000).Discordaram);
    }

    /// <summary>E com termos mais fundos, onde as combinacoes ficam mais estranhas.</summary>
    [Fact]
    public void EhZeroComTermosMaisFundos()
    {
        foreach (var profundidade in new[] { 2, 3, 5 })
            Assert.Equal(0, ChurchRosser.Sortear(7, 2_000, profundidade).Discordaram);
    }

    /// <summary>
    /// A comparacao precisa ser entre FORMAS NORMAIS de verdade, e isso me
    /// corrigiu.
    ///
    /// A estrategia por valor para na forma normal FRACA: ela nao entra dentro de
    /// lambda. Comparar o termo dela com o da ordem normal nao e testar o
    /// teorema, e comparar duas coisas diferentes, e na primeira versao isso deu
    /// 4.784 "discordancias" em 50.000 termos, nenhuma delas contraexemplo.
    /// </summary>
    [Fact]
    public void AComparacaoEhEntreFormasNormaisDeVerdade()
    {
        var termo = A(Combinadores.S(), Combinadores.K(), Combinadores.K());
        var c = ChurchRosser.Confrontar("S K K", termo);

        // as tres param, e so duas chegam na forma normal de verdade
        Assert.Equal(3, c.QueTerminaram.Count());
        Assert.Equal(2, c.QueChegaramNaNormal.Count());
        Assert.DoesNotContain(Estrategia.PorValor, c.QueChegaramNaNormal);
        Assert.True(c.Concordam);
    }

    [Fact]
    public void AFormaNormalNaoTemRedexNenhum()
    {
        Assert.True(ChurchRosser.EhFormaNormal(V("x")));
        Assert.True(ChurchRosser.EhFormaNormal(L("x", V("x"))));
        Assert.True(ChurchRosser.EhFormaNormal(A(V("f"), V("x"))));

        Assert.False(ChurchRosser.EhFormaNormal(A(L("x", V("x")), V("y"))));
        Assert.False(ChurchRosser.EhFormaNormal(L("y", A(L("x", V("x")), V("z")))));
    }

    /// <summary>
    /// Nos termos com nome, todas as que chegam concordam, inclusive nos que nao
    /// chegam: quando ninguem chega, nao ha o que discordar.
    /// </summary>
    [Fact]
    public void TodosOsCombinadoresConcordam()
    {
        foreach (var (nome, termo) in Combinadores.Todos())
            Assert.True(ChurchRosser.Confrontar(nome, termo, 2_000).Concordam, nome);
    }

    /// <summary>E nas contas de numeral, que sao termos grandes de verdade.</summary>
    [Fact]
    public void AsContasDeNumeralConcordam()
    {
        foreach (var (nome, conta, _) in Igreja.Contas())
        {
            var c = ChurchRosser.Confrontar(nome, conta, 100_000);

            Assert.True(c.Concordam, nome);
            Assert.True(c.QueChegaramNaNormal.Count() >= 2, $"{nome}: so {c.QueChegaramNaNormal.Count()} chegaram");
        }
    }

    /// <summary>
    /// O teorema NAO diz que toda estrategia chega: so que as que chegam
    /// concordam. O termo da constante com omega mostra as duas frases juntas.
    /// </summary>
    [Fact]
    public void OTeoremaNaoPrometeQueTodaEstrategiaChega()
    {
        var c = ChurchRosser.Confrontar("K y omega", Combinadores.ConstanteComOmega(), 2_000);

        Assert.True(c.Concordam);
        Assert.Single(c.QueChegaramNaNormal);
        Assert.Equal(Estrategia.Normal, c.QueChegaramNaNormal.Single());
    }

    [Fact]
    public void OSorteioEhDeterministico()
    {
        var primeiro = new Sorteio(7);
        var segundo = new Sorteio(7);

        for (var i = 0; i < 200; i++)
            Assert.Equal(primeiro.Qualquer(4).ToString(), segundo.Qualquer(4).ToString());

        Assert.Equal(ChurchRosser.Sortear(7, 500), ChurchRosser.Sortear(7, 500));
    }

    [Fact]
    public void AAmostraTemVariedade()
    {
        var sorteio = new Sorteio(7);
        var tamanhos = new HashSet<int>();
        var formas = new HashSet<string>();

        for (var i = 0; i < 500; i++)
        {
            var termo = sorteio.Qualquer(4);
            tamanhos.Add(termo.Tamanho());
            formas.Add(termo.GetType().Name);
        }

        Assert.True(tamanhos.Count > 5, $"so {tamanhos.Count} tamanhos diferentes");
        Assert.Equal(3, formas.Count);
    }

    [Fact]
    public void SementeZeroNaoTravaOSorteio()
    {
        Assert.NotEqual(0u, new Sorteio(0).Proximo());
    }
}
