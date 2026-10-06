using Conde.Reducao;
using static Conde.Reducao.Escrever;

namespace Conde.Reducao.Medidor;

/// <summary>
/// As medidas. Tudo contado em PASSOS de reducao e em termos conferidos: nenhuma
/// depende de relogio nem de maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "juiz": Juiz(); break;
            case "estrategias": Estrategias(); break;
            case "captura": Captura(); break;
            case "igreja": Igrejas(); break;
            case "preguicosa": Preguicosas(); break;
            case "recursao": Recursao(); break;
            case "tudo": Juiz(); Estrategias(); Captura(); Igrejas(); Preguicosas(); Recursao(); break;
            default:
                Console.Error.WriteLine("medidas: juiz, estrategias, captura, igreja, preguicosa, recursao, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>Church-Rosser, conferido em termos sorteados.</summary>
    private static void Juiz()
    {
        Console.WriteLine("== Church-Rosser: quem chega, chega no mesmo lugar ==");
        Console.WriteLine();
        Console.WriteLine($"{"termos sorteados",18}{"alguma terminou",18}{"discordaram",14}");

        foreach (var quantos in new[] { 1_000, 10_000, 50_000 })
        {
            var (total, terminaram, discordaram) = ChurchRosser.Sortear(7, quantos);
            Console.WriteLine($"{total,18:N0}{terminaram,18:N0}{discordaram,14}");
        }

        Console.WriteLine();
        Console.WriteLine("a ultima coluna e ZERO em cinquenta mil termos sorteados sem nenhum");
        Console.WriteLine("cuidado. Quando duas estrategias terminam, elas terminam no MESMO termo, a");
        Console.WriteLine("menos de renomear os parametros");
        Console.WriteLine();
        Console.WriteLine("e isso que transforma 'a minha estrategia deu este resultado' em 'este e o");
        Console.WriteLine("resultado'. Sem o teorema, cada estrategia poderia dar uma resposta");
        Console.WriteLine("diferente e nenhuma seria mais certa que a outra");
        Console.WriteLine();
        Console.WriteLine("e ele NAO diz que toda estrategia chega: so que as que chegam concordam. A");
        Console.WriteLine("diferenca entre as duas frases e o assunto do repositorio");
        Console.WriteLine();

        Console.WriteLine($"  nos termos com nome:");
        Console.WriteLine($"  {"termo",-16}{"normal",14}{"aplicativa",14}{"por valor",14}{"concordam",12}");

        foreach (var (nome, termo) in Combinadores.Todos())
        {
            var c = ChurchRosser.Confrontar(nome, termo, 2_000);
            var textos = Reducao.Todas()
                .Select(e => c.Resultados[e].Terminou ? $"{c.Resultados[e].Passos} passos" : "nao termina")
                .ToArray();

            Console.WriteLine($"  {nome,-16}{textos[0],14}{textos[1],14}{textos[2],14}{(c.Concordam ? "sim" : "NAO"),12}");
        }

        Console.WriteLine();
    }

    /// <summary>A diferenca entre as estrategias, em termo de uma linha.</summary>
    private static void Estrategias()
    {
        Console.WriteLine("== a estrategia nao muda a resposta, e muda SE ha resposta ==");
        Console.WriteLine();

        var termo = Combinadores.ConstanteComOmega();
        Console.WriteLine($"  o termo:  {termo}");
        Console.WriteLine($"  ele e uma funcao que JOGA FORA o argumento, aplicada a um argumento");
        Console.WriteLine($"  que nao termina");
        Console.WriteLine();

        foreach (var estrategia in Reducao.Todas())
        {
            var r = Reducao.Reduzir(termo, estrategia, 5_000);
            Console.WriteLine($"    {Reducao.Nome(estrategia),-20}{(r.Terminou ? $"{r.Final} em {r.Passos} passo(s)" : $"nao terminou em {r.Passos} passos"),-40}");
        }

        Console.WriteLine();
        Console.WriteLine("a ordem NORMAL responde no primeiro passo, porque ela reduz a aplicacao de");
        Console.WriteLine("fora antes de olhar o argumento, e o argumento nunca chega a ser calculado");
        Console.WriteLine();
        Console.WriteLine("a ordem APLICATIVA nao responde nunca, porque ela insiste em terminar o");
        Console.WriteLine("argumento primeiro. O argumento nao termina, e a funcao ia joga-lo fora");
        Console.WriteLine();
        Console.WriteLine("o teorema da terminacao e so sobre a ordem normal: se o termo tem forma");
        Console.WriteLine("normal, ela chega la. Nenhuma outra estrategia tem essa garantia");
        Console.WriteLine();

        Console.WriteLine($"  e quando as duas terminam, qual gasta menos passos?");
        Console.WriteLine($"  {"conta",-24}{"normal",12}{"aplicativa",14}{"razao",10}");

        foreach (var (nome, conta, _) in Igreja.Contas())
        {
            var normal = Reducao.Reduzir(conta, Estrategia.Normal, 50_000);
            var aplicativa = Reducao.Reduzir(conta, Estrategia.Aplicativa, 50_000);

            if (!normal.Terminou || !aplicativa.Terminou) continue;

            Console.WriteLine($"  {nome,-24}{normal.Passos,12:N0}{aplicativa.Passos,14:N0}" +
                              $"{(double)normal.Passos / aplicativa.Passos,10:N2}");
        }

        Console.WriteLine();
        Console.WriteLine("  a ordem aplicativa gasta MENOS passos quando termina, e e por isso que");
        Console.WriteLine("  quase toda linguagem usa ela: a garantia da ordem normal custa recalcular");
        Console.WriteLine("  o argumento em cada lugar onde o parametro aparece");
        Console.WriteLine();
    }

    /// <summary>A captura de variavel, e o estrago de nao renomear.</summary>
    private static void Captura()
    {
        Console.WriteLine("== a CAPTURA: a substituicao ingenua troca uma funcao por outra ==");
        Console.WriteLine();

        var onde = L("x", V("y"));
        var certo = Substituicao.Trocar(onde, "y", V("x"));
        var errado = Substituicao.TrocarIngenuamente(onde, "y", V("x"));

        Console.WriteLine($"  substituir y por x em {onde}:");
        Console.WriteLine($"    com renomear:  {certo.Termo}      ({certo.Renomeacoes} renomeacao)");
        Console.WriteLine($"    sem renomear:  {errado}");
        Console.WriteLine();
        Console.WriteLine("o certo e uma funcao que JOGA FORA o argumento e devolve o x de fora. O");
        Console.WriteLine("errado e a IDENTIDADE, que devolve o argumento. Sao funcoes completamente");
        Console.WriteLine("diferentes, e a substituicao ingenua trocou uma pela outra em silencio");
        Console.WriteLine();

        Console.WriteLine($"  e a diferenca aparece na hora de usar:");
        Console.WriteLine($"    ({certo.Termo}) z  reduz para  {Reducao.Reduzir(A(certo.Termo, V("z")), Estrategia.Normal).Final}");
        Console.WriteLine($"    ({errado}) z  reduz para  {Reducao.Reduzir(A(errado, V("z")), Estrategia.Normal).Final}");
        Console.WriteLine();
        Console.WriteLine("  uma devolve x e a outra devolve z. Nao e um detalhe de nomes: e a");
        Console.WriteLine("  resposta");
        Console.WriteLine();

        Console.WriteLine($"  quantas renomeacoes cada conta precisa:");
        Console.WriteLine($"  {"conta",-24}{"passos",10}{"renomeacoes",14}");

        foreach (var (nome, conta, _) in Igreja.Contas())
        {
            var r = Reducao.Reduzir(conta, Estrategia.Normal, 50_000);
            Console.WriteLine($"  {nome,-24}{r.Passos,10:N0}{r.Renomeacoes,14:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("  nas contas de numeral quase nao ha renomeacao, porque os nomes nao");
        Console.WriteLine("  colidem. E por isso que o erro passa despercebido: ele so aparece onde");
        Console.WriteLine("  alguem reusa um nome");
        Console.WriteLine();
    }

    /// <summary>Os numerais de Igreja: contas de verdade com resposta conhecida.</summary>
    private static void Igrejas()
    {
        Console.WriteLine("== os numerais de Igreja: a linguagem de tres formas basta ==");
        Console.WriteLine();
        Console.WriteLine($"{"conta",-24}{"esperado",10}{"reduziu para",14}{"passos",10}{"bate",8}");

        foreach (var (nome, termo, esperado) in Igreja.Contas())
        {
            var r = Reducao.Reduzir(termo, Estrategia.Normal, 100_000);
            var lido = Igreja.Ler(r.Final);

            Console.WriteLine($"{nome,-24}{esperado,10}{lido?.ToString() ?? "nao e numeral",14}" +
                              $"{r.Passos,10:N0}{(lido == esperado ? "sim" : "NAO"),8}");
        }

        Console.WriteLine();
        Console.WriteLine("nao ha numero no calculo lambda, e nao falta: o numero tres e 'a funcao que");
        Console.WriteLine("aplica alguma coisa tres vezes'. A soma, o produto e a potencia sao termos,");
        Console.WriteLine("nao primitivas");
        Console.WriteLine();
        Console.WriteLine("e a resposta e conhecida de FORA: ninguem precisa confiar no redutor para");
        Console.WriteLine("saber que dois mais tres e cinco");
        Console.WriteLine();

        Console.WriteLine($"  e os logicos, que tambem sao funcoes:");
        Console.WriteLine($"  {"teste",-24}{"esperado",12}{"reduziu para",16}{"bate",8}");

        foreach (var (nome, termo, esperado) in Igreja.Logicos())
        {
            var r = Reducao.Reduzir(termo, Estrategia.Normal, 10_000);
            var lido = Igreja.LerLogico(r.Final);

            Console.WriteLine($"  {nome,-24}{esperado,12}{lido?.ToString() ?? "nao e logico",16}" +
                              $"{(lido == esperado ? "sim" : "NAO"),8}");
        }

        Console.WriteLine();
        Console.WriteLine("  o condicional nem precisa existir: o proprio logico escolhe. Verdadeiro e");
        Console.WriteLine("  'a funcao que devolve o primeiro' e falso e 'a que devolve o segundo'");
        Console.WriteLine();
    }

    /// <summary>A preguicosa, que tem a terminacao de uma e o custo da outra.</summary>
    private static void Preguicosas()
    {
        Console.WriteLine("== a preguicosa: a terminacao de uma e o custo da outra ==");
        Console.WriteLine();
        Console.WriteLine($"{"conta",-26}{"normal",12}{"aplicativa",14}{"preguicosa",14}{"reusos",10}");

        foreach (var (nome, conta, _) in Igreja.Contas())
        {
            var normal = Reducao.Reduzir(conta, Estrategia.Normal, 100_000);
            var aplicativa = Reducao.Reduzir(conta, Estrategia.Aplicativa, 100_000);
            var preguicosa = Preguicosa.Reduzir(conta, 100_000);

            Console.WriteLine($"{nome,-26}{normal.Passos,12:N0}{aplicativa.Passos,14:N0}" +
                              $"{preguicosa.Passos,14:N0}{preguicosa.Compartilhamentos,10:N0}");
        }

        Console.WriteLine();

        var caro = Combinadores.ArgumentoCaroUsadoTresVezes();
        var normalCaro = Reducao.Reduzir(caro, Estrategia.Normal, 100_000);
        var preguicosaCaro = Preguicosa.Reduzir(caro, 100_000);

        Console.WriteLine("  e o caso que separa de verdade: um argumento caro usado TRES vezes");
        Console.WriteLine($"    ordem normal:  {normalCaro.Passos:N0} passos");
        Console.WriteLine($"    preguicosa:    {preguicosaCaro.Passos:N0} passos, com {preguicosaCaro.Compartilhamentos:N0} reusos");
        Console.WriteLine();
        Console.WriteLine("  a ordem normal COPIA o argumento para cada lugar em que o parametro");
        Console.WriteLine("  aparece, e depois reduz cada copia separadamente");
        Console.WriteLine();
        Console.WriteLine("  e aqui vale dizer o que a tabela NAO esta comparando. A preguicosa para");
        Console.WriteLine("  na forma normal FRACA: ela nao entra dentro de lambda, e o numeral de");
        Console.WriteLine("  Igreja e um lambda. Entao os numeros dela nao sao do mesmo problema");
        Console.WriteLine();
        Console.WriteLine("  o que ela mede e o custo de chegar a um VALOR, que e o que uma linguagem");
        Console.WriteLine("  de verdade faz: ninguem reduz dentro do corpo de uma funcao que ainda nao");
        Console.WriteLine("  foi chamada");
        Console.WriteLine();
        Console.WriteLine("  por isso a coluna de reusos e zero nestas contas: cada argumento e");
        Console.WriteLine("  olhado no maximo uma vez antes de o valor sair. O compartilhamento paga");
        Console.WriteLine("  quando o mesmo argumento e demandado varias vezes, e ai ele e exatamente");
        Console.WriteLine("  a diferenca entre a ordem normal e uma linguagem preguicosa usavel");
        Console.WriteLine();

        Console.WriteLine($"  e ela termina onde a aplicativa nao termina:");
        var armadilha = Combinadores.ConstanteComOmega();
        var aplicativaArmadilha = Reducao.Reduzir(armadilha, Estrategia.Aplicativa, 5_000);
        var preguicosaArmadilha = Preguicosa.Reduzir(armadilha, 5_000);

        Console.WriteLine($"    {armadilha}");
        Console.WriteLine($"      aplicativa:  {(aplicativaArmadilha.Terminou ? "terminou" : "nao terminou")}");
        Console.WriteLine($"      preguicosa:  {(preguicosaArmadilha.Terminou ? $"terminou em {preguicosaArmadilha.Final}" : "nao terminou")}");
        Console.WriteLine();
    }

    /// <summary>A recursao numa linguagem sem recursao.</summary>
    private static void Recursao()
    {
        Console.WriteLine("== a recursao numa linguagem em que nada tem nome ==");
        Console.WriteLine();

        Console.WriteLine($"  o combinador Y:  {Combinadores.Y()}");
        Console.WriteLine();
        Console.WriteLine("  Y f reduz para f (Y f), que reduz para f (f (Y f)), e assim por diante: a");
        Console.WriteLine("  funcao recebe a si mesma de presente, sem precisar de nome");
        Console.WriteLine();

        var yf = A(Combinadores.Y(), V("f"));
        var trilha = Reducao.Trilha(yf, Estrategia.Normal, 4);

        Console.WriteLine($"  os primeiros passos de Y f:");
        for (var i = 0; i < trilha.Count; i++)
            Console.WriteLine($"    {i}:  {Encurtar(trilha[i].ToString()!)}");

        Console.WriteLine();
        Console.WriteLine($"  e o tamanho do termo a cada passo:");
        Console.Write("    ");
        foreach (var t in Reducao.Trilha(yf, Estrategia.Normal, 8)) Console.Write($"{t.Tamanho(),6}");
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("  ele CRESCE, e e isso que ele deve fazer: cada passo desdobra mais uma");
        Console.WriteLine("  volta da recursao. Sob ordem APLICATIVA isso nao para nunca, nem aplicado");
        Console.WriteLine("  a uma funcao mansa, porque a estrategia insiste em terminar o argumento");
        Console.WriteLine();

        Console.WriteLine($"    {"combinador",-14}{"aplicativa",16}{"por valor",16}");

        foreach (var (nome, combinador) in new[] { ("Y", Combinadores.Y()), ("Z", Combinadores.Z()) })
        {
            var aplicativa = Reducao.Reduzir(A(combinador, Combinadores.K()), Estrategia.Aplicativa, 2_000);
            var porValor = Reducao.Reduzir(A(combinador, Combinadores.K()), Estrategia.PorValor, 2_000);

            Console.WriteLine($"    {nome,-14}{(aplicativa.Terminou ? "terminou" : "nao termina"),16}" +
                              $"{(porValor.Terminou ? "terminou" : "nao termina"),16}");
        }

        Console.WriteLine();
        Console.WriteLine("  e aqui a medida me corrigiu. Eu ia escrever que o Z funciona sob ordem");
        Console.WriteLine("  APLICATIVA e o Y nao, e os dois nao funcionam: a ordem aplicativa entra");
        Console.WriteLine("  DENTRO de lambda, e a protecao do Z e exatamente um lambda");
        Console.WriteLine();
        Console.WriteLine("  o que o Z resolve e a estrategia POR VALOR, que e a das linguagens de");
        Console.WriteLine("  verdade: ali funcao e valor pronto e o corpo nao e tocado, entao o lambda");
        Console.WriteLine("  a mais segura a expansao e o Z termina onde o Y nao termina");
        Console.WriteLine();

        Console.WriteLine($"  e o OMEGA, que e o menor termo sem forma normal que existe:");
        Console.WriteLine($"    {Combinadores.Omega()}");

        var omega = Reducao.Trilha(Combinadores.Omega(), Estrategia.Normal, 3);
        Console.WriteLine($"    e ele reduz para ELE MESMO: {Substituicao.Iguais(omega[0], omega[1])}");
        Console.WriteLine();
        Console.WriteLine("  nao cresce, nao encolhe, nao chega a lugar nenhum, e cabe em oito");
        Console.WriteLine("  caracteres");
        Console.WriteLine();

        Console.WriteLine($"  e tem a outra cara de 'nao termina', que e CRESCER:");
        Console.Write("    ");
        foreach (var t in Reducao.Trilha(Combinadores.QueCresce(), Estrategia.Normal, 7)) Console.Write($"{t.Tamanho(),8}");
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("  o limite de passos pega as duas; o limite de TAMANHO pega esta antes de a");
        Console.WriteLine("  memoria acabar, que e um jeito melhor de falhar");
        Console.WriteLine();
    }

    private static string Encurtar(string texto) => texto.Length <= 70 ? texto : texto[..67] + "...";
}
