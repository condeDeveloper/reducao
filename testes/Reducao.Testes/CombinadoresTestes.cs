using Conde.Reducao;
using Xunit;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Testes;

public class CombinadoresTestes
{
    [Fact]
    public void ADevolveOQueRecebe()
    {
        Assert.Equal(V("a"), Reducao.Reduzir(A(Combinadores.I(), V("a")), Estrategia.Normal).Final);
    }

    [Fact]
    public void KJogaOSegundoFora()
    {
        Assert.Equal(V("a"), Reducao.Reduzir(A(Combinadores.K(), V("a"), V("b")), Estrategia.Normal).Final);
    }

    /// <summary>
    /// S K K e a IDENTIDADE, que e o resultado mais conhecido da logica
    /// combinatoria: com S e K da para escrever I.
    /// </summary>
    [Fact]
    public void SKKEhAIdentidade()
    {
        var termo = A(Combinadores.S(), Combinadores.K(), Combinadores.K());
        var r = Reducao.Reduzir(termo, Estrategia.Normal, 10_000);

        Assert.True(Substituicao.Iguais(Combinadores.I(), r.Final), $"deu {r.Final}");

        // e ela age como a identidade
        Assert.Equal(V("z"), Reducao.Reduzir(A(termo, V("z")), Estrategia.Normal, 10_000).Final);
    }

    /// <summary>
    /// A AUTO-APLICACAO sozinha e inofensiva: ela e um valor pronto, e ninguem a
    /// chamou ainda.
    /// </summary>
    [Fact]
    public void AAutoAplicacaoSozinhaEhUmValor()
    {
        var r = Reducao.Reduzir(Combinadores.Mockingbird(), Estrategia.Normal, 100);

        Assert.True(r.Terminou);
        Assert.True(ChurchRosser.EhFormaNormal(r.Final));
    }

    /// <summary>E aplicada a si mesma ela e o OMEGA, que nao termina em nenhuma estrategia.</summary>
    [Fact]
    public void OOmegaNaoTerminaEmEstrategiaNenhuma()
    {
        foreach (var estrategia in Reducao.Todas())
            Assert.False(Reducao.Reduzir(Combinadores.Omega(), estrategia, 1_000).Terminou);
    }

    /// <summary>
    /// O combinador Y desdobra a recursao: Y f reduz para f (Y f), que reduz
    /// para f (f (Y f)).
    /// </summary>
    [Fact]
    public void OYDesdobraARecursao()
    {
        var trilha = Reducao.Trilha(A(Combinadores.Y(), V("f")), Estrategia.Normal, 6);

        // depois do inicio, cada passo acrescenta um f na frente
        var comF = trilha.Skip(2).Select(t => ContarFs(t)).ToList();

        for (var i = 1; i < comF.Count; i++)
            Assert.Equal(comF[i - 1] + 1, comF[i]);
    }

    private static int ContarFs(Termo termo)
    {
        var quantos = 0;
        var atual = termo;

        while (atual is Aplicacao a && a.Alvo is Variavel { Nome: "f" }) { quantos++; atual = a.Argumento; }

        return quantos;
    }

    /// <summary>
    /// O ACHADO que me corrigiu: o Z NAO funciona sob ordem APLICATIVA.
    ///
    /// Eu ia escrever que ele funciona onde o Y nao funciona, e os dois nao
    /// funcionam: a ordem aplicativa entra DENTRO de lambda, e a protecao do Z
    /// e exatamente um lambda.
    ///
    /// O que o Z resolve e a estrategia POR VALOR, que e a das linguagens de
    /// verdade: ali funcao e valor pronto e o corpo nao e tocado.
    /// </summary>
    [Fact]
    public void OZFuncionaPorValorENaoSobOrdemAplicativa()
    {
        var y = A(Combinadores.Y(), Combinadores.K());
        var z = A(Combinadores.Z(), Combinadores.K());

        Assert.False(Reducao.Reduzir(y, Estrategia.Aplicativa, 2_000).Terminou);
        Assert.False(Reducao.Reduzir(z, Estrategia.Aplicativa, 2_000).Terminou);

        Assert.False(Reducao.Reduzir(y, Estrategia.PorValor, 2_000).Terminou);
        Assert.True(Reducao.Reduzir(z, Estrategia.PorValor, 2_000).Terminou);
    }

    /// <summary>
    /// O termo que CRESCE e a outra cara de "nao termina": ele nao repete, ele
    /// incha.
    /// </summary>
    [Fact]
    public void OQueCresceIncha()
    {
        var trilha = Reducao.Trilha(Combinadores.QueCresce(), Estrategia.Normal, 6);

        Assert.True(trilha[^1].Tamanho() > trilha[1].Tamanho() * 2);
        Assert.False(Reducao.Reduzir(Combinadores.QueCresce(), Estrategia.Normal, 2_000).Terminou);
    }

    /// <summary>
    /// E o termo da constante com omega e o caso de uma linha que separa as
    /// estrategias.
    /// </summary>
    [Fact]
    public void AConstanteComOmegaSeparaAsEstrategias()
    {
        var termo = Combinadores.ConstanteComOmega();

        Assert.Equal(V("y"), Reducao.Reduzir(termo, Estrategia.Normal, 2_000).Final);
        Assert.False(Reducao.Reduzir(termo, Estrategia.Aplicativa, 2_000).Terminou);
    }

    [Fact]
    public void TodosOsCombinadoresTemNome()
    {
        foreach (var (nome, termo) in Combinadores.Todos())
        {
            Assert.False(string.IsNullOrWhiteSpace(nome));
            Assert.True(termo.Tamanho() > 0);
        }
    }

    [Fact]
    public void OArgumentoCaroUsaOParametroTresVezes()
    {
        var termo = Combinadores.ArgumentoCaroUsadoTresVezes();
        var aplicacao = Assert.IsType<Aplicacao>(termo);
        var funcao = Assert.IsType<Funcao>(aplicacao.Alvo);

        Assert.Equal(3, Preguicosa.Usos(funcao.Corpo, funcao.Parametro));
    }
}
