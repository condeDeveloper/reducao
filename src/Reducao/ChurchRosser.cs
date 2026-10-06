namespace Conde.Reducao;

/// <summary>
/// CHURCH e ROSSER, 1936, que e o JUIZ deste repositorio.
///
/// O teorema diz uma coisa forte: se um termo tem forma normal, ela e UNICA. Nao
/// importa que redex foi escolhido em cada passo, nem em que ordem: quem chega,
/// chega no mesmo lugar.
///
/// Isso e o que transforma "a minha estrategia deu este resultado" em "este e o
/// resultado". Sem ele, cada estrategia poderia dar uma resposta diferente e
/// nenhuma seria mais certa que a outra.
///
/// E ele NAO diz que toda estrategia chega: so que as que chegam concordam. A
/// diferenca entre as duas frases e o assunto inteiro deste repositorio, e aqui
/// ela e medida.
/// </summary>
public static class ChurchRosser
{
    public sealed record Confronto(string Nome, Dictionary<Estrategia, Reducao.Resultado> Resultados)
    {
        /// <summary>As que pararam, em qualquer forma.</summary>
        public IEnumerable<Estrategia> QueTerminaram => Resultados.Where(e => e.Value.Terminou).Select(e => e.Key);

        /// <summary>
        /// As que chegaram na forma normal DE VERDADE, sem redex nenhum sobrando.
        ///
        /// A diferenca entre esta e a de cima me custou uma tabela errada. A
        /// estrategia POR VALOR para na forma normal FRACA: ela nao entra dentro
        /// de lambda, entao o termo que ela devolve pode ter redex no corpo de
        /// uma funcao e mesmo assim ela diz que acabou.
        ///
        /// Comparar esse termo com o da ordem normal nao e testar Church-Rosser:
        /// e comparar duas coisas diferentes. Na primeira versao isso deu 4.784
        /// "discordancias" em 50.000 termos, e NENHUMA delas era contraexemplo
        /// do teorema.
        /// </summary>
        public IEnumerable<Estrategia> QueChegaramNaNormal =>
            Resultados.Where(e => e.Value.Terminou && EhFormaNormal(e.Value.Final)).Select(e => e.Key);

        /// <summary>Se todas as que chegaram na forma normal concordam, a menos de renomear.</summary>
        public bool Concordam
        {
            get
            {
                var normais = Resultados.Where(e => e.Value.Terminou && EhFormaNormal(e.Value.Final))
                                        .Select(e => e.Value.Final).ToList();

                for (var i = 1; i < normais.Count; i++)
                    if (!Substituicao.Iguais(normais[0], normais[i])) return false;

                return true;
            }
        }
    }

    /// <summary>
    /// Roda o termo em todas as estrategias e confronta os resultados.
    ///
    /// A comparacao e a menos de RENOMEAR, e isso nao e frouxidao: \x.x e \y.y
    /// sao a mesma funcao escrita com outra letra, e exigir a mesma letra seria
    /// comparar a escrita em vez do termo.
    /// </summary>
    public static Confronto Confrontar(string nome, Termo termo, int limite = 10_000)
    {
        ArgumentNullException.ThrowIfNull(termo);

        var resultados = new Dictionary<Estrategia, Reducao.Resultado>();

        foreach (var estrategia in Reducao.Todas())
            resultados[estrategia] = Reducao.Reduzir(termo, estrategia, limite);

        return new Confronto(nome, resultados);
    }

    /// <summary>
    /// Se o termo nao tem redex NENHUM, nem dentro de lambda: a forma normal de
    /// verdade.
    /// </summary>
    public static bool EhFormaNormal(Termo termo)
    {
        ArgumentNullException.ThrowIfNull(termo);
        return Reducao.UmPasso(termo, Estrategia.Normal).Termo is null;
    }

    /// <summary>
    /// Confere o teorema em termos SORTEADOS, que e o que torna a conferencia
    /// uma medida em vez de um exemplo.
    ///
    /// Sete termos escolhidos a mao nao dizem nada sobre um teorema que fala de
    /// todos os termos. Milhares de termos sorteados sem cuidado nenhum dizem
    /// bem mais.
    /// </summary>
    public static (int Total, int Terminaram, int Discordaram) Sortear(ulong semente, int quantos,
                                                                      int profundidade = 4, int limite = 2_000)
    {
        var sorteio = new Sorteio(semente);
        int terminaram = 0, discordaram = 0;

        for (var i = 0; i < quantos; i++)
        {
            var termo = sorteio.Qualquer(profundidade);
            var confronto = Confrontar("sorteado", termo, limite);

            if (confronto.QueChegaramNaNormal.Any()) terminaram++;
            if (!confronto.Concordam) discordaram++;
        }

        return (quantos, terminaram, discordaram);
    }
}
