using Conde.Reducao;
using Xunit;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Testes;

public class PreguicosaTestes
{
    /// <summary>
    /// Ela chega no mesmo VALOR que as outras, nas contas de numeral: a resposta
    /// e a mesma, contada de fora.
    /// </summary>
    [Fact]
    public void ElaChegaNoMesmoValor()
    {
        foreach (var (nome, conta, esperado) in Igreja.Contas())
        {
            var r = Preguicosa.Reduzir(conta, 100_000);

            Assert.True(r.Terminou, $"{nome} nao terminou");

            // o valor e um numeral so depois de reduzir o corpo, entao o
            // confronto e com a ordem normal aplicada ao que ela devolveu
            var normal = Reducao.Reduzir(r.Final, Estrategia.Normal, 100_000);
            Assert.Equal(esperado, Igreja.Ler(normal.Final));
        }
    }

    /// <summary>
    /// O ACHADO: ela TERMINA onde a aplicativa nao termina.
    ///
    /// O argumento nao e avaliado na chamada: ele entra na caixa como esta, e so
    /// e calculado se alguem olhar para ele. A funcao joga ele fora, entao
    /// ninguem olha.
    /// </summary>
    [Fact]
    public void ElaTerminaOndeAAplicativaNaoTermina()
    {
        var termo = Combinadores.ConstanteComOmega();

        Assert.False(Reducao.Reduzir(termo, Estrategia.Aplicativa, 5_000).Terminou);

        var preguicosa = Preguicosa.Reduzir(termo, 5_000);
        Assert.True(preguicosa.Terminou);
        Assert.Equal(V("y"), preguicosa.Final);
    }

    /// <summary>
    /// E esse caso me custou um bug: a costura do ambiente CAPTURAVA.
    ///
    /// Avaliando "K y omega", a caixa de x guarda o termo "y" e o corpo a
    /// costurar e "\y.x". Sem renomear o parametro, a costura devolvia "\y.y",
    /// que e a identidade, e a identidade aplicada a omega NAO TERMINA. A
    /// resposta certa e "y".
    /// </summary>
    [Fact]
    public void ACosturaNaoCapturaOParametro()
    {
        // K y, avaliado, tem que dar uma funcao que devolve y, e nao a identidade
        var parcial = Preguicosa.Reduzir(A(Combinadores.K(), V("y")), 1_000);

        Assert.True(parcial.Terminou);

        var funcao = Assert.IsType<Funcao>(parcial.Final);
        Assert.NotEqual(funcao.Parametro, (funcao.Corpo as Variavel)?.Nome);

        // e aplicada a qualquer coisa ela devolve y
        var usada = Reducao.Reduzir(A(parcial.Final, V("z")), Estrategia.Normal, 1_000);
        Assert.Equal(V("y"), usada.Final);
    }

    /// <summary>
    /// Ela para na forma normal FRACA: ela nao entra dentro de lambda, e e por
    /// isso que os numeros dela nao sao comparaveis com os das outras.
    ///
    /// O que ela mede e o custo de chegar a um VALOR, que e o que uma linguagem
    /// de verdade faz.
    /// </summary>
    [Fact]
    public void ElaParaNaFormaNormalFraca()
    {
        var conta = A(Igreja.Soma(), Igreja.Numero(2), Igreja.Numero(3));
        var r = Preguicosa.Reduzir(conta, 10_000);

        Assert.True(r.Terminou);
        Assert.IsType<Funcao>(r.Final);
        Assert.False(ChurchRosser.EhFormaNormal(r.Final), "ela entrou dentro do lambda");
    }

    /// <summary>
    /// E por isso ela gasta MENOS passos: ela nao faz o trabalho que as outras
    /// fazem dentro do corpo das funcoes.
    /// </summary>
    [Fact]
    public void ElaGastaMenosPassosQueAsOutras()
    {
        foreach (var (nome, conta, _) in Igreja.Contas())
        {
            var normal = Reducao.Reduzir(conta, Estrategia.Normal, 100_000);
            var preguicosa = Preguicosa.Reduzir(conta, 100_000);

            Assert.True(preguicosa.Passos < normal.Passos, $"{nome}: preguicosa {preguicosa.Passos}, normal {normal.Passos}");
        }
    }

    /// <summary>
    /// A contagem de USOS e a medida do desperdicio da ordem normal: com o
    /// parametro aparecendo tres vezes, ela faz o trabalho do argumento tres
    /// vezes.
    /// </summary>
    [Fact]
    public void AContagemDeUsosEhOQueDiz()
    {
        Assert.Equal(1, Preguicosa.Usos(V("x"), "x"));
        Assert.Equal(0, Preguicosa.Usos(V("y"), "x"));
        Assert.Equal(2, Preguicosa.Usos(A(V("x"), V("x")), "x"));

        // o parametro escondido por um lambda interno nao conta
        Assert.Equal(0, Preguicosa.Usos(L("x", V("x")), "x"));
        Assert.Equal(1, Preguicosa.Usos(A(L("x", V("x")), V("x")), "x"));
    }

    /// <summary>
    /// No argumento caro usado tres vezes, a diferenca para a ordem normal e de
    /// mais de dez vezes.
    /// </summary>
    [Fact]
    public void NoArgumentoCaroADiferencaEhGrande()
    {
        var termo = Combinadores.ArgumentoCaroUsadoTresVezes();

        var normal = Reducao.Reduzir(termo, Estrategia.Normal, 100_000);
        var preguicosa = Preguicosa.Reduzir(termo, 100_000);

        Assert.True(normal.Terminou);
        Assert.True(preguicosa.Terminou);
        Assert.True(normal.Passos > preguicosa.Passos * 5,
                    $"normal {normal.Passos}, preguicosa {preguicosa.Passos}");
    }

    [Fact]
    public void OOmegaNaoTerminaNemNaPreguicosa()
    {
        Assert.False(Preguicosa.Reduzir(Combinadores.Omega(), 1_000).Terminou);
    }

    [Fact]
    public void UmValorProntoSaiSemPassoNenhum()
    {
        var r = Preguicosa.Reduzir(Combinadores.I(), 100);

        Assert.True(r.Terminou);
        Assert.Equal(0, r.Passos);
        Assert.True(Substituicao.Iguais(Combinadores.I(), r.Final));
    }

    [Fact]
    public void OResumoEhLegivel()
    {
        var texto = Preguicosa.Reduzir(A(Combinadores.I(), V("a")), 100).ToString();
        Assert.Contains("passos", texto, StringComparison.Ordinal);
    }
}
