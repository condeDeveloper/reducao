namespace Conde.Reducao;

/// <summary>
/// A REDUCAO PREGUICOSA, que e por necessidade e com COMPARTILHAMENTO.
///
/// A ordem normal tem a garantia de terminacao e um defeito caro: ela copia o
/// argumento para cada lugar em que o parametro aparece, e depois reduz cada
/// copia separadamente. Um argumento usado tres vezes e calculado tres vezes.
///
/// A preguicosa conserta isso guardando o argumento UMA vez e apontando para ele:
/// quando a primeira copia e reduzida, todas as outras ja ficam reduzidas
/// tambem, porque sao a mesma coisa. Isso e o que uma linguagem preguicosa de
/// verdade faz, e e o motivo de ela ser pratica em vez de so elegante.
///
/// A medida mostra as duas coisas juntas: ela termina onde a aplicativa nao
/// termina, e gasta menos passos que a ordem normal.
/// </summary>
public static class Preguicosa
{
    /// <summary>Uma caixa com o argumento, compartilhada por todos os usos dele.</summary>
    private sealed class Caixa
    {
        public Caixa(Termo termo) => Termo = termo;

        public Termo Termo { get; set; }

        public bool Pronta { get; set; }
    }

    public sealed record Resultado(Termo Final, int Passos, bool Terminou, int Compartilhamentos)
    {
        public override string ToString() =>
            Terminou ? $"{Final} em {Passos} passos" : $"nao terminou em {Passos} passos";
    }

    /// <summary>
    /// Reduz ate a forma normal fraca, guardando cada argumento numa caixa.
    ///
    /// A conta de passos conta reducoes beta de verdade, e nao visitas: e por
    /// isso que ela da para comparar com as outras estrategias.
    /// </summary>
    public static Resultado Reduzir(Termo termo, int limite = 2_000)
    {
        ArgumentNullException.ThrowIfNull(termo);

        // O limite daqui e menor que o das outras estrategias de proposito: esta
        // avaliacao desce na pilha a cada aplicacao, entao o numero de passos
        // que ela aguenta e limitado pela pilha e nao pela paciencia.
        var passos = 0;
        var compartilhados = 0;
        var ambiente = new Dictionary<string, Caixa>(StringComparer.Ordinal);

        var final = Avaliar(termo, ambiente, ref passos, ref compartilhados, limite);

        return new Resultado(final ?? termo, passos, final is not null, compartilhados);
    }

    private static Termo? Avaliar(Termo termo, Dictionary<string, Caixa> ambiente,
                                  ref int passos, ref int compartilhados, int limite)
    {
        if (passos >= limite) return null;

        switch (termo)
        {
            case Variavel v:
            {
                if (!ambiente.TryGetValue(v.Nome, out var caixa)) return v;

                // Se a caixa ja foi avaliada, o resultado sai de graca: esse e o
                // compartilhamento, e e ele que separa esta estrategia da ordem
                // normal.
                if (caixa.Pronta) { compartilhados++; return caixa.Termo; }

                var valor = Avaliar(caixa.Termo, new Dictionary<string, Caixa>(ambiente, StringComparer.Ordinal),
                                    ref passos, ref compartilhados, limite);
                if (valor is null) return null;

                caixa.Termo = valor;
                caixa.Pronta = true;
                return valor;
            }

            case Funcao f:
            {
                // O parametro precisa ser renomeado quando ele aparece LIVRE em
                // algum termo do ambiente que vai entrar no corpo. Sem isso,
                // esse termo entra aqui dentro e e capturado por este lambda.
                //
                // O caso concreto: avaliando "K y omega", a caixa de x guarda o
                // termo "y" e o corpo e "\y.x". Sem renomear, a costura devolvia
                // "\y.y", que e a identidade, e a identidade aplicada a omega
                // NAO TERMINA. A resposta certa e "y", em dois passos.
                //
                // E a capture nao da para consertar so dentro da costura: o
                // lambda que captura e criado AQUI, fora dela.
                var perigosos = new HashSet<string>(StringComparer.Ordinal);

                foreach (var (nome, caixa) in ambiente)
                    if (nome != f.Parametro && f.Corpo.Livres().Contains(nome))
                        perigosos.UnionWith(caixa.Termo.Livres());

                var parametro = f.Parametro;
                var corpo = f.Corpo;

                if (perigosos.Contains(parametro))
                {
                    parametro = Substituicao.NomeNovo(f.Parametro, perigosos, Substituicao.TodosOsNomes(f.Corpo));
                    corpo = Substituicao.TrocarIngenuamente(f.Corpo, f.Parametro, new Variavel(parametro));
                }

                return new Funcao(parametro, Costurar(corpo, ambiente, parametro));
            }

            case Aplicacao a:
            {
                var alvo = Avaliar(a.Alvo, ambiente, ref passos, ref compartilhados, limite);
                if (alvo is null) return null;

                if (alvo is not Funcao f) return new Aplicacao(alvo, Costurar(a.Argumento, ambiente, null));

                passos++;
                if (passos >= limite) return null;

                // O argumento NAO e avaliado aqui: ele entra na caixa como esta,
                // e so e calculado se alguem olhar para ele.
                var dentro = new Dictionary<string, Caixa>(ambiente, StringComparer.Ordinal)
                {
                    [f.Parametro] = new(Costurar(a.Argumento, ambiente, null)),
                };

                return Avaliar(f.Corpo, dentro, ref passos, ref compartilhados, limite);
            }

            default:
                return termo;
        }
    }

    /// <summary>
    /// Fecha o termo com o que o ambiente ja sabe, sem reduzir nada.
    ///
    /// Sem isso, um termo devolvido de dentro de uma funcao sairia com variaveis
    /// soltas apontando para um ambiente que acabou, e a resposta mudaria.
    /// </summary>
    private static Termo Costurar(Termo termo, Dictionary<string, Caixa> ambiente, string? escondido)
    {
        // A costura e feita com a substituicao CUIDADOSA, uma entrada por vez, e
        // nao percorrendo a arvore a mao.
        //
        // A versao a mao parece obviamente certa e tem o mesmo buraco que a
        // substituicao ingenua: ao entrar num lambda, ela nao renomeia nada, e
        // um termo do ambiente que tenha o nome do parametro livre e CAPTURADO
        // por ele.
        //
        // O caso concreto: avaliando "K y omega", a caixa de x guarda o termo
        // "y" e o corpo a costurar e "\y.x". A costura a mao devolvia "\y.y",
        // que e a identidade, e a identidade aplicada a omega NAO TERMINA. A
        // resposta certa e "y", em dois passos.
        var saida = termo;

        foreach (var (nome, caixa) in ambiente)
        {
            if (nome == escondido) continue;
            if (!saida.Livres().Contains(nome)) continue;

            saida = Substituicao.Trocar(saida, nome, caixa.Termo).Termo;
        }

        return saida;
    }

    /// <summary>
    /// Quantas vezes o parametro aparece no corpo, que e quantas vezes a ordem
    /// normal vai recalcular o argumento.
    ///
    /// Esse numero e a medida do desperdicio: com o parametro aparecendo tres
    /// vezes, a ordem normal faz o trabalho do argumento tres vezes e a
    /// preguicosa faz uma.
    /// </summary>
    public static int Usos(Termo corpo, string parametro)
    {
        ArgumentNullException.ThrowIfNull(corpo);

        return corpo switch
        {
            Variavel v => v.Nome == parametro ? 1 : 0,
            Funcao f => f.Parametro == parametro ? 0 : Usos(f.Corpo, parametro),
            Aplicacao a => Usos(a.Alvo, parametro) + Usos(a.Argumento, parametro),
            _ => 0,
        };
    }
}
