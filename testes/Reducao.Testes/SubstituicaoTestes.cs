using Conde.Reducao;
using Xunit;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Testes;

public class SubstituicaoTestes
{
    [Fact]
    public void TrocarUmaVariavelSolta()
    {
        Assert.Equal(V("y"), Substituicao.Trocar(V("x"), "x", V("y")).Termo);
        Assert.Equal(V("z"), Substituicao.Trocar(V("z"), "x", V("y")).Termo);
    }

    /// <summary>
    /// O parametro ESCONDE a variavel: dentro de \x.x, o nome x significa outra
    /// coisa, e nao ha nada para trocar.
    /// </summary>
    [Fact]
    public void OParametroEscondeAVariavel()
    {
        var termo = L("x", V("x"));
        Assert.Equal(termo, Substituicao.Trocar(termo, "x", V("y")).Termo);
    }

    /// <summary>
    /// O ACHADO: a substituicao ingenua troca uma funcao por outra, em silencio.
    ///
    /// Substituir y por x em \x.y deveria dar \x'.x, que joga o argumento fora e
    /// devolve o x de fora. Sem renomear da \x.x, que e a IDENTIDADE.
    /// </summary>
    [Fact]
    public void AIngenuaTrocaAConstantePelaIdentidade()
    {
        var onde = L("x", V("y"));

        var certo = Substituicao.Trocar(onde, "y", V("x"));
        var errado = Substituicao.TrocarIngenuamente(onde, "y", V("x"));

        Assert.Equal(1, certo.Renomeacoes);
        Assert.Equal(L("x'", V("x")), certo.Termo);
        Assert.Equal(L("x", V("x")), errado);

        // e a diferenca aparece na hora de usar
        Assert.Equal(V("x"), Reducao.Reduzir(A(certo.Termo, V("z")), Estrategia.Normal).Final);
        Assert.Equal(V("z"), Reducao.Reduzir(A(errado, V("z")), Estrategia.Normal).Final);
    }

    /// <summary>
    /// Sem captura, nao ha renomeacao: o cuidado so custa quando e preciso.
    /// </summary>
    [Fact]
    public void SemCapturaNaoHaRenomeacao()
    {
        var r = Substituicao.Trocar(L("x", V("y")), "y", V("w"));

        Assert.Equal(0, r.Renomeacoes);
        Assert.Equal(L("x", V("w")), r.Termo);
    }

    /// <summary>
    /// O nome fresco precisa evitar os nomes LIGADOS tambem, e nao so os livres.
    ///
    /// Se ele for um nome que ja esta ligado la dentro, a renomeacao empurra o
    /// nome novo para dentro desse outro lambda e e capturado por ele.
    /// </summary>
    [Fact]
    public void ONomeFrescoEvitaOsLigadosTambem()
    {
        // \x.(\x'.x), substituindo y por um termo com x e x' livres
        var onde = L("x", A(L("x'", V("x")), V("y")));
        var r = Substituicao.Trocar(onde, "y", A(V("x"), V("x'")));

        Assert.True(r.Renomeacoes > 0);

        // nenhum x nem x' do termo que entrou pode ter virado ligado
        var fora = r.Termo.Livres();
        Assert.Contains("x", fora);
        Assert.Contains("x'", fora);
    }

    /// <summary>
    /// E ele precisa evitar a PROPRIA variavel que esta sendo substituida.
    ///
    /// Isso me custou nove discordancias em cinquenta mil termos sorteados, com
    /// o teorema de Church-Rosser parecendo falhar. Substituindo x' em \x.x, o
    /// nome fresco escolhido era x', a renomeacao virava \x'.x' e a substituicao
    /// seguinte trocava pelo termo que entrava: a identidade virava constante.
    /// </summary>
    [Fact]
    public void ONomeFrescoEvitaAPropriaVariavelSubstituida()
    {
        var identidade = L("x", V("x"));
        var entra = A(V("x"), V("w"));

        var r = Substituicao.Trocar(identidade, "x'", entra);

        // x' nao ocorre na identidade, entao ela nao pode mudar
        Assert.True(Substituicao.Iguais(identidade, r.Termo), $"a identidade virou {r.Termo}");
    }

    /// <summary>
    /// A IGUALDADE do calculo lambda e a menos de renomear: \x.x e \y.y sao a
    /// mesma funcao escrita com outra letra.
    /// </summary>
    [Fact]
    public void AIgualdadeEhAMenosDeRenomear()
    {
        Assert.True(Substituicao.Iguais(L("x", V("x")), L("y", V("y"))));
        Assert.True(Substituicao.Iguais(L("x", "y", V("x")), L("a", "b", V("a"))));
        Assert.False(Substituicao.Iguais(L("x", "y", V("x")), L("a", "b", V("b"))));
        Assert.False(Substituicao.Iguais(V("x"), V("y")));
    }

    /// <summary>
    /// E ela precisa contar a profundidade A PARTE, e nao pelo tamanho do mapa.
    ///
    /// Com sombreamento, o segundo x sobrescreve o primeiro e o tamanho do mapa
    /// nao cresce: o parametro seguinte recebia o mesmo nivel do x, e duas
    /// variaveis diferentes passavam a ser consideradas iguais.
    /// </summary>
    [Fact]
    public void AIgualdadeAguentaSombreamento()
    {
        // \x.\x.\y.y  contra  \x.\x.\y.x : sao diferentes
        var comY = L("x", "x", L("y", V("y")));
        var comX = L("x", "x", L("y", V("x")));

        Assert.False(Substituicao.Iguais(comY, comX), "o sombreamento confundiu os niveis");
        Assert.True(Substituicao.Iguais(comY, L("a", "b", L("c", V("c")))));
        Assert.True(Substituicao.Iguais(comX, L("a", "b", L("c", V("b")))));
    }

    [Fact]
    public void TodosOsNomesPegaLivresELigados()
    {
        var nomes = Substituicao.TodosOsNomes(L("x", A(V("y"), L("z", V("z")))));

        Assert.Contains("x", nomes);
        Assert.Contains("y", nomes);
        Assert.Contains("z", nomes);
    }

    [Fact]
    public void ONomeNovoEmpilhaApostrofos()
    {
        Assert.Equal("x", Substituicao.NomeNovo("x", []));
        Assert.Equal("x'", Substituicao.NomeNovo("x", new[] { "x" }));
        Assert.Equal("x''", Substituicao.NomeNovo("x", new[] { "x" }, new[] { "x'" }));
    }
}
