namespace Conde.Reducao;

/// <summary>
/// A ESTRATEGIA: qual redex reduzir quando ha mais de um.
///
/// Esse e o assunto inteiro do repositorio. O calculo lambda tem uma regra so, e
/// um termo pode ter varios lugares onde ela se aplica. Escolher qual nao muda a
/// resposta, quando ha resposta, e muda SE ha resposta.
/// </summary>
public enum Estrategia
{
    /// <summary>
    /// ORDEM NORMAL: o redex mais a esquerda e mais externo, sempre.
    ///
    /// Ela e a unica com garantia: se o termo tem forma normal, esta estrategia
    /// chega nela. O preco e recalcular o argumento toda vez que ele aparece.
    /// </summary>
    Normal,

    /// <summary>
    /// ORDEM APLICATIVA: reduzir o argumento ate o fim antes de aplicar.
    ///
    /// E o que quase toda linguagem faz, e ela NAO tem a garantia: ha termos com
    /// forma normal em que ela nao termina, porque ela insiste em calcular um
    /// argumento que a funcao ia jogar fora.
    /// </summary>
    Aplicativa,

    /// <summary>
    /// POR VALOR: como a aplicativa, mas sem entrar dentro de lambda.
    ///
    /// E a mais parecida com uma linguagem de verdade: funcao e valor, e o corpo
    /// dela so e tocado quando ela e chamada. Ela para antes da forma normal, no
    /// que se chama forma normal fraca.
    /// </summary>
    PorValor,
}

/// <summary>
/// A REDUCAO: aplicar a regra beta ate nao dar mais.
///
/// A regra e uma so: aplicar uma funcao a um argumento e trocar, no corpo dela,
/// o parametro pelo argumento. Tudo o mais e consequencia.
/// </summary>
public static class Reducao
{
    public sealed record Resultado(Termo Final, int Passos, bool Terminou, int Renomeacoes)
    {
        public bool Divergiu => !Terminou;

        public override string ToString() =>
            Terminou ? $"{Final} em {Passos} passos" : $"nao terminou em {Passos} passos";
    }

    /// <summary>
    /// UM passo de reducao, conforme a estrategia. Devolve nulo quando nao ha
    /// mais redex para aquela estrategia.
    /// </summary>
    public static (Termo? Termo, int Renomeacoes) UmPasso(Termo termo, Estrategia estrategia)
    {
        ArgumentNullException.ThrowIfNull(termo);

        return estrategia switch
        {
            Estrategia.Normal => PassoNormal(termo),
            Estrategia.Aplicativa => PassoAplicativo(termo),
            Estrategia.PorValor => PassoPorValor(termo),
            _ => (null, 0),
        };
    }

    /// <summary>
    /// O redex mais a ESQUERDA e mais EXTERNO.
    ///
    /// "Mais externo" e o que da a garantia: reduzir a aplicacao de fora antes
    /// de olhar o argumento significa que um argumento que vai ser jogado fora
    /// nunca chega a ser calculado.
    /// </summary>
    private static (Termo? Termo, int Renomeacoes) PassoNormal(Termo termo)
    {
        if (termo is Aplicacao { Alvo: Funcao f } a)
        {
            var r = Substituicao.Trocar(f.Corpo, f.Parametro, a.Argumento);
            return (r.Termo, r.Renomeacoes);
        }

        switch (termo)
        {
            case Funcao funcao:
            {
                var (dentro, renomeacoes) = PassoNormal(funcao.Corpo);
                return dentro is null ? (null, 0) : (new Funcao(funcao.Parametro, dentro), renomeacoes);
            }

            case Aplicacao aplicacao:
            {
                var (noAlvo, renomeacoes) = PassoNormal(aplicacao.Alvo);
                if (noAlvo is not null) return (new Aplicacao(noAlvo, aplicacao.Argumento), renomeacoes);

                var (noArgumento, outras) = PassoNormal(aplicacao.Argumento);
                return noArgumento is null ? (null, 0) : (new Aplicacao(aplicacao.Alvo, noArgumento), outras);
            }

            default:
                return (null, 0);
        }
    }

    /// <summary>
    /// O redex mais a esquerda e mais INTERNO: o argumento primeiro.
    /// </summary>
    private static (Termo? Termo, int Renomeacoes) PassoAplicativo(Termo termo)
    {
        switch (termo)
        {
            case Funcao funcao:
            {
                var (dentro, renomeacoes) = PassoAplicativo(funcao.Corpo);
                return dentro is null ? (null, 0) : (new Funcao(funcao.Parametro, dentro), renomeacoes);
            }

            case Aplicacao aplicacao:
            {
                var (noAlvo, renomeacoes) = PassoAplicativo(aplicacao.Alvo);
                if (noAlvo is not null) return (new Aplicacao(noAlvo, aplicacao.Argumento), renomeacoes);

                var (noArgumento, outras) = PassoAplicativo(aplicacao.Argumento);
                if (noArgumento is not null) return (new Aplicacao(aplicacao.Alvo, noArgumento), outras);

                // So depois que os dois lados estao prontos.
                if (aplicacao.Alvo is Funcao f)
                {
                    var r = Substituicao.Trocar(f.Corpo, f.Parametro, aplicacao.Argumento);
                    return (r.Termo, r.Renomeacoes);
                }

                return (null, 0);
            }

            default:
                return (null, 0);
        }
    }

    /// <summary>
    /// POR VALOR: como a aplicativa, sem entrar dentro de lambda.
    ///
    /// E a regra das linguagens de verdade: funcao e valor pronto, e o corpo so
    /// e mexido quando alguem chama.
    /// </summary>
    private static (Termo? Termo, int Renomeacoes) PassoPorValor(Termo termo)
    {
        if (termo is not Aplicacao aplicacao) return (null, 0);

        var (noAlvo, renomeacoes) = PassoPorValor(aplicacao.Alvo);
        if (noAlvo is not null) return (new Aplicacao(noAlvo, aplicacao.Argumento), renomeacoes);

        var (noArgumento, outras) = PassoPorValor(aplicacao.Argumento);
        if (noArgumento is not null) return (new Aplicacao(aplicacao.Alvo, noArgumento), outras);

        if (aplicacao.Alvo is Funcao f)
        {
            var r = Substituicao.Trocar(f.Corpo, f.Parametro, aplicacao.Argumento);
            return (r.Termo, r.Renomeacoes);
        }

        return (null, 0);
    }

    /// <summary>
    /// Reduz ate acabar, ou ate bater no limite.
    ///
    /// O limite nao e preguica: ha termos que nao terminam nunca, e o mais famoso
    /// deles cabe em oito caracteres. Sem limite, a medida nao seria uma medida:
    /// seria um processo travado.
    /// </summary>
    /// <param name="fundo">
    /// A PROFUNDIDADE maxima do termo, que e um limite diferente do de passos e
    /// precisa existir junto com ele.
    ///
    /// Isso me custou uma integracao continua vermelha em DOIS dos tres
    /// sistemas. Contar passos limita o trabalho e nao limita a PILHA: todo
    /// percurso de termo aqui e recursivo, entao um termo que fica mais FUNDO a
    /// cada reducao estoura a pilha mesmo com o contador de passos longe do
    /// limite.
    ///
    /// E o limite de tamanho nao pega isso: o combinador Y aplicado a K cresce
    /// em profundidade muito mais depressa do que em numero de nos, e derrubou
    /// o processo com 3.211 quadros, com o tamanho ainda na casa dos milhares.
    ///
    /// O Linux aguentou e o macOS e o Windows nao, que e o pior jeito de um
    /// erro aparecer: o teste passa na maquina de quem escreveu.
    /// </param>
    public static Resultado Reduzir(Termo termo, Estrategia estrategia, int limite = 10_000, int fundo = 400)
    {
        ArgumentNullException.ThrowIfNull(termo);

        var atual = termo;
        var renomeacoes = 0;

        for (var passo = 0; passo < limite; passo++)
        {
            var (proximo, quantas) = UmPasso(atual, estrategia);
            renomeacoes += quantas;

            if (proximo is null) return new Resultado(atual, passo, true, renomeacoes);

            atual = proximo;

            // Um termo que cresce sem parar e tao divergente quanto um que
            // repete: parar nele evita estourar a memoria em vez do limite. E o
            // que cresce para BAIXO estoura a pilha antes da memoria.
            if (atual.Profundidade() > fundo || atual.Tamanho() > 200_000)
                return new Resultado(atual, passo + 1, false, renomeacoes);
        }

        return new Resultado(atual, limite, false, renomeacoes);
    }

    /// <summary>A trilha inteira, para olhar a reducao acontecer.</summary>
    public static List<Termo> Trilha(Termo termo, Estrategia estrategia, int limite = 50, int fundo = 400)
    {
        var trilha = new List<Termo> { termo };
        var atual = termo;

        for (var passo = 0; passo < limite; passo++)
        {
            var (proximo, _) = UmPasso(atual, estrategia);
            if (proximo is null) break;

            atual = proximo;
            trilha.Add(atual);

            if (atual.Profundidade() > fundo) break;
        }

        return trilha;
    }

    public static IEnumerable<Estrategia> Todas()
    {
        yield return Estrategia.Normal;
        yield return Estrategia.Aplicativa;
        yield return Estrategia.PorValor;
    }

    public static string Nome(Estrategia estrategia) => estrategia switch
    {
        Estrategia.Normal => "ordem normal",
        Estrategia.Aplicativa => "ordem aplicativa",
        Estrategia.PorValor => "por valor",
        _ => estrategia.ToString(),
    };
}
