namespace Conde.Reducao;

/// <summary>
/// Um gerador de termos ALEATORIOS, com o sorteio escrito aqui.
///
/// A documentacao do sorteio de biblioteca nao promete a mesma sequencia entre
/// versoes nem entre sistemas, e a integracao continua roda em tres: com este, a
/// semente 7 produz exatamente os mesmos termos nos tres.
/// </summary>
public sealed class Sorteio
{
    private static readonly string[] Nomes = ["x", "y", "z", "w"];

    private ulong estado;

    public Sorteio(ulong semente) => estado = semente == 0 ? 1 : semente;

    public uint Proximo()
    {
        estado = estado * 6364136223846793005UL + 1442695040888963407UL;
        return (uint)(estado >> 32);
    }

    public int Ate(int limite) => (int)(Proximo() % (uint)limite);

    /// <summary>
    /// Um termo qualquer.
    ///
    /// Nada aqui tenta produzir termo que termina. A maioria termina, alguns nao,
    /// e e exatamente isso que faz a medida valer: a amostra nao foi escolhida
    /// para o teorema se dar bem nela.
    /// </summary>
    public Termo Qualquer(int profundidade, IReadOnlyList<string>? visiveis = null)
    {
        visiveis ??= [];

        if (profundidade <= 0 || Ate(100) < 30)
            return new Variavel(visiveis.Count > 0 ? visiveis[Ate(visiveis.Count)] : Nomes[Ate(Nomes.Length)]);

        if (Ate(2) == 0)
        {
            var nome = Nomes[Ate(Nomes.Length)];
            var comNome = visiveis.Append(nome).Distinct(StringComparer.Ordinal).ToList();

            return new Funcao(nome, Qualquer(profundidade - 1, comNome));
        }

        return new Aplicacao(Qualquer(profundidade - 1, visiveis), Qualquer(profundidade - 1, visiveis));
    }
}
