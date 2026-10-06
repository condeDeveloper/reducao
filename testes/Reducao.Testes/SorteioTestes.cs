using Conde.Reducao;
using Xunit;

namespace Conde.Reducao.Testes;

public class SorteioTestes
{
    [Fact]
    public void OSorteioEhDeterministico()
    {
        var primeiro = new Sorteio(7);
        var segundo = new Sorteio(7);

        for (var i = 0; i < 1000; i++) Assert.Equal(primeiro.Proximo(), segundo.Proximo());
    }

    [Fact]
    public void SementesDiferentesDaoTermosDiferentes()
    {
        Assert.NotEqual(new Sorteio(1).Qualquer(5).ToString(), new Sorteio(2).Qualquer(5).ToString());
    }

    [Fact]
    public void OSorteioFicaDentroDoLimite()
    {
        var sorteio = new Sorteio(7);

        for (var i = 0; i < 2000; i++) Assert.InRange(sorteio.Ate(4), 0, 3);
    }

    /// <summary>
    /// A amostra tem variedade: ela nao e feita so de variaveis soltas. Sem
    /// isso, a conferencia do teorema seria sobre nada.
    /// </summary>
    [Fact]
    public void AAmostraTemAsTresFormas()
    {
        var sorteio = new Sorteio(7);
        var formas = new HashSet<string>();
        var tamanhos = new HashSet<int>();

        for (var i = 0; i < 1000; i++)
        {
            var termo = sorteio.Qualquer(4);
            formas.Add(termo.GetType().Name);
            tamanhos.Add(termo.Tamanho());
        }

        Assert.Equal(3, formas.Count);
        Assert.True(tamanhos.Count > 5, $"so {tamanhos.Count} tamanhos diferentes");
    }

    /// <summary>
    /// E ela tem termos que NAO terminam: se todos terminassem, a medida nao
    /// diria nada sobre a parte interessante do teorema.
    /// </summary>
    [Fact]
    public void AAmostraTemTermosQueNaoTerminam()
    {
        var sorteio = new Sorteio(7);
        var naoTerminaram = 0;

        for (var i = 0; i < 2000; i++)
        {
            var termo = sorteio.Qualquer(5);
            if (!Reducao.Reduzir(termo, Estrategia.Normal, 500).Terminou) naoTerminaram++;
        }

        Assert.True(naoTerminaram > 0, "nenhum termo sorteado deixou de terminar");
    }

    /// <summary>
    /// A profundidade pedida limita o termo: com profundidade zero sai uma
    /// variavel.
    /// </summary>
    [Fact]
    public void AProfundidadeZeroDaUmaVariavel()
    {
        var sorteio = new Sorteio(7);

        for (var i = 0; i < 100; i++) Assert.IsType<Variavel>(sorteio.Qualquer(0));
    }

    [Fact]
    public void TermosMaisFundosSaoMaiores()
    {
        var raso = 0.0;
        var fundo = 0.0;

        var sorteio = new Sorteio(7);
        for (var i = 0; i < 300; i++) raso += sorteio.Qualquer(2).Tamanho();

        sorteio = new Sorteio(7);
        for (var i = 0; i < 300; i++) fundo += sorteio.Qualquer(6).Tamanho();

        Assert.True(fundo > raso * 2, $"raso {raso / 300:N1}, fundo {fundo / 300:N1}");
    }

    [Fact]
    public void SementeZeroNaoTravaOSorteio()
    {
        Assert.NotEqual(0u, new Sorteio(0).Proximo());
    }

    /// <summary>
    /// Todo termo sorteado e escrevivel e relivel: a escrita nao quebra em
    /// nenhuma combinacao.
    /// </summary>
    [Fact]
    public void TodoTermoSorteadoEhEscrevivel()
    {
        var sorteio = new Sorteio(7);

        for (var i = 0; i < 2000; i++)
        {
            var texto = sorteio.Qualquer(5).ToString();
            Assert.False(string.IsNullOrWhiteSpace(texto));
        }
    }
}
