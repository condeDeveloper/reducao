using Conde.Reducao;
using Xunit;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Testes;

public class EstrategiaTestes
{
    [Fact]
    public void UmPassoDeBetaEhASubstituicao()
    {
        var termo = A(L("x", V("x")), V("y"));
        var (depois, _) = Reducao.UmPasso(termo, Estrategia.Normal);

        Assert.Equal(V("y"), depois);
    }

    [Fact]
    public void SemRedexNaoHaPasso()
    {
        foreach (var estrategia in Reducao.Todas())
        {
            Assert.Null(Reducao.UmPasso(V("x"), estrategia).Termo);
            Assert.Null(Reducao.UmPasso(A(V("f"), V("x")), estrategia).Termo);
        }
    }

    /// <summary>
    /// O ACHADO: a ordem NORMAL responde e a APLICATIVA nao, no mesmo termo.
    ///
    /// Uma funcao que joga o argumento fora, aplicada a um argumento que nao
    /// termina. A normal nunca olha o argumento; a aplicativa insiste em
    /// termina-lo.
    /// </summary>
    [Fact]
    public void ANormalRespondeOndeAAplicativaNaoResponde()
    {
        var termo = Combinadores.ConstanteComOmega();

        var normal = Reducao.Reduzir(termo, Estrategia.Normal, 5_000);
        var aplicativa = Reducao.Reduzir(termo, Estrategia.Aplicativa, 5_000);

        Assert.True(normal.Terminou);
        Assert.Equal(V("y"), normal.Final);
        Assert.True(normal.Passos <= 2);

        Assert.False(aplicativa.Terminou);
    }

    /// <summary>
    /// E quando as duas terminam, a APLICATIVA gasta menos passos. E por isso
    /// que quase toda linguagem usa ela.
    /// </summary>
    [Fact]
    public void AAplicativaGastaMenosPassosQuandoTermina()
    {
        var algumaGanhou = false;

        foreach (var (nome, conta, _) in Igreja.Contas())
        {
            var normal = Reducao.Reduzir(conta, Estrategia.Normal, 50_000);
            var aplicativa = Reducao.Reduzir(conta, Estrategia.Aplicativa, 50_000);

            if (!normal.Terminou || !aplicativa.Terminou) continue;

            Assert.True(aplicativa.Passos <= normal.Passos, $"{nome}: aplicativa {aplicativa.Passos}, normal {normal.Passos}");
            if (aplicativa.Passos < normal.Passos) algumaGanhou = true;
        }

        Assert.True(algumaGanhou, "nenhuma conta separou as duas estrategias");
    }

    /// <summary>
    /// Na potencia a diferenca e de QUATRO vezes: a ordem normal copia o
    /// argumento para cada lugar em que o parametro aparece.
    /// </summary>
    [Fact]
    public void NaPotenciaADiferencaEhDeQuatroVezes()
    {
        var conta = A(Igreja.Potencia(), Igreja.Numero(2), Igreja.Numero(5));

        var normal = Reducao.Reduzir(conta, Estrategia.Normal, 50_000);
        var aplicativa = Reducao.Reduzir(conta, Estrategia.Aplicativa, 50_000);

        Assert.True((double)normal.Passos / aplicativa.Passos > 3);
    }

    /// <summary>
    /// A POR VALOR nao entra dentro de lambda: ela para na forma normal FRACA,
    /// que pode ter redex no corpo de uma funcao.
    /// </summary>
    [Fact]
    public void APorValorNaoEntraDentroDeLambda()
    {
        var termo = L("y", A(L("x", V("x")), V("z")));

        Assert.Null(Reducao.UmPasso(termo, Estrategia.PorValor).Termo);
        Assert.NotNull(Reducao.UmPasso(termo, Estrategia.Normal).Termo);
        Assert.NotNull(Reducao.UmPasso(termo, Estrategia.Aplicativa).Termo);

        Assert.False(ChurchRosser.EhFormaNormal(Reducao.Reduzir(termo, Estrategia.PorValor).Final));
    }

    /// <summary>
    /// O OMEGA reduz para ELE MESMO, em um passo, para sempre: o menor termo sem
    /// forma normal que existe.
    /// </summary>
    [Fact]
    public void OOmegaReduzParaEleMesmo()
    {
        var omega = Combinadores.Omega();
        var (depois, _) = Reducao.UmPasso(omega, Estrategia.Normal);

        Assert.NotNull(depois);
        Assert.True(Substituicao.Iguais(omega, depois!));

        foreach (var estrategia in Reducao.Todas())
            Assert.False(Reducao.Reduzir(omega, estrategia, 500).Terminou);
    }

    /// <summary>
    /// E tem a outra cara de "nao termina", que e CRESCER. O limite de tamanho
    /// pega esta antes de a memoria acabar.
    /// </summary>
    [Fact]
    public void OTermoQueCresceEhPegoPeloLimiteDeTamanho()
    {
        var trilha = Reducao.Trilha(Combinadores.QueCresce(), Estrategia.Normal, 6);

        for (var i = 2; i < trilha.Count; i++)
            Assert.True(trilha[i].Tamanho() > trilha[i - 1].Tamanho(), $"no passo {i} ele parou de crescer");

        Assert.False(Reducao.Reduzir(Combinadores.QueCresce(), Estrategia.Normal, 1_000).Terminou);
    }

    [Fact]
    public void ATrilhaMostraOsPassos()
    {
        var trilha = Reducao.Trilha(A(Igreja.Soma(), Igreja.Numero(1), Igreja.Numero(1)), Estrategia.Normal, 20);

        Assert.True(trilha.Count > 1);
        Assert.Equal(A(Igreja.Soma(), Igreja.Numero(1), Igreja.Numero(1)), trilha[0]);
        Assert.True(ChurchRosser.EhFormaNormal(trilha[^1]));
    }

    [Fact]
    public void CadaEstrategiaTemNome()
    {
        Assert.Equal(3, Reducao.Todas().Count());

        foreach (var estrategia in Reducao.Todas())
            Assert.False(string.IsNullOrWhiteSpace(Reducao.Nome(estrategia)));
    }
}
