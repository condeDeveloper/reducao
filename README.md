# reducao

Cálculo lambda do zero em C# e .NET 8: ordem normal, ordem aplicativa, por valor
e redução preguiçosa com compartilhamento. O juiz é o teorema de
**Church-Rosser**, conferido em 50.000 termos sorteados.

```
$ dotnet medidor.dll juiz

  termos sorteados   alguma terminou   discordaram
             1,000               992             0
            10,000             9,915             0
            50,000            49,645             0
```

O teorema diz que se um termo tem forma normal, ela é **única**: não importa que
redex foi escolhido em cada passo. É isso que transforma "a minha estratégia deu
este resultado" em "este é o resultado".

E ele **não** diz que toda estratégia chega: só que as que chegam concordam. A
diferença entre as duas frases é o repositório inteiro.

## A estratégia não muda a resposta, e muda se há resposta

```
$ dotnet medidor.dll estrategias

  o termo:  (\x.\y.x) y ((\x.x x) (\x.x x))

    ordem normal        y em 2 passo(s)
    ordem aplicativa    nao terminou em 5000 passos
    por valor           nao terminou em 5000 passos
```

Uma função que **joga o argumento fora**, aplicada a um argumento que não termina.

A ordem normal responde no primeiro passo, porque reduz a aplicação de fora antes
de olhar o argumento. A ordem aplicativa não responde nunca, porque insiste em
terminar um argumento que ia ser jogado fora.

E quando as duas terminam, a aplicativa gasta **menos**:

```
  conta                         normal    aplicativa     razao
  2 elevado a 5                     64            16      4.00
  3 elevado a 3                     28            12      2.33
  3 vezes 4                          9             9      1.00
```

É por isso que quase toda linguagem usa ordem aplicativa: a garantia da ordem
normal custa recalcular o argumento em cada lugar onde o parâmetro aparece.

## A captura troca uma função por outra, em silêncio

```
$ dotnet medidor.dll captura

  substituir y por x em \x.y:
    com renomear:  \x'.x      (1 renomeacao)
    sem renomear:  \x.x

    (\x'.x) z  reduz para  x
    (\x.x) z  reduz para  z
```

O certo joga o argumento fora e devolve o `x` de fora. O errado é a **identidade**.
Uma devolve `x` e a outra devolve `z`: não é detalhe de nomes, é a resposta.

## Os numerais de Igreja: três formas bastam

```
$ dotnet medidor.dll igreja

conta                     esperado  reduziu para    passos    bate
2 mais 3                         5             5         6     sim
3 vezes 4                       12            12         9     sim
2 elevado a 5                   32            32        64     sim
3 elevado a 3                   27            27        28     sim
```

Não há número no cálculo lambda, e não falta: o número três é "a função que
aplica alguma coisa três vezes". A soma, o produto e a potência são termos, não
primitivas. E a resposta é conhecida **de fora**: ninguém precisa confiar no
redutor para saber que dois mais três é cinco.

O condicional nem precisa existir. Verdadeiro é "a função que devolve o primeiro"
e falso é "a que devolve o segundo", então o próprio lógico escolhe.

## A recursão numa linguagem em que nada tem nome

```
$ dotnet medidor.dll recursao

  o combinador Y:  \f.(\x.f (x x)) (\x.f (x x))

    0:  (\f.(\x.f (x x)) (\x.f (x x))) f
    1:  (\x.f (x x)) (\x.f (x x))
    2:  f ((\x.f (x x)) (\x.f (x x)))
    3:  f (f ((\x.f (x x)) (\x.f (x x))))
```

`Y f` reduz para `f (Y f)`, que reduz para `f (f (Y f))`: a função recebe a si
mesma de presente, sem precisar de nome.

E o **omega**, que é o menor termo sem forma normal que existe, cabe em oito
caracteres e reduz para ele mesmo, para sempre:

```
    (\x.x x) (\x.x x)
```

## O que as medidas me corrigiram

**O teste de Church-Rosser achou dois bugs na minha substituição.** A primeira
rodada deu nove discordâncias em cinquenta mil termos, e o teorema não falha:
quem falhava era eu.

O nome fresco escolhido na renomeação precisa evitar **três** conjuntos, e eu
estava usando um: as variáveis livres do termo que entra, **todos** os nomes do
corpo (livres ou ligados) e a **própria variável que está sendo substituída**. O
terceiro foi o que pegou: substituindo `x'` numa função `\x.x`, o nome fresco
escolhido era exatamente `x'`, a renomeação virava `\x'.x'` e a substituição
seguinte trocava pelo termo que entrava. A identidade virava uma constante.

**E a igualdade a menos de renomear contava a profundidade pelo tamanho do mapa.**
Com sombreamento, o segundo `x` sobrescreve o primeiro e o tamanho **não** cresce:
o parâmetro seguinte recebia o mesmo nível do `x`, e duas variáveis diferentes
passavam a ser consideradas iguais.

**4.784 "discordâncias" em 50.000 termos eram a comparação errada, não o
teorema.** A estratégia por valor para na forma normal **fraca**: ela não entra
dentro de lambda, então o termo que ela devolve pode ter redex no corpo de uma
função e mesmo assim ela diz que acabou. Comparar esse termo com o da ordem
normal não é testar Church-Rosser.

**A redução preguiçosa capturava na costura do ambiente, e isso fazia ela não
terminar onde ela deveria terminar.** Avaliando `K y omega`, a caixa de `x` guarda
o termo `y` e o corpo a costurar é `\y.x`. Sem renomear o parâmetro, a costura
devolvia `\y.y`, que é a identidade, e a identidade aplicada a omega não termina.
A resposta certa é `y`, em dois passos. E o conserto não cabia só dentro da
costura: o lambda que captura é criado fora dela.

**Eu ia escrever que o combinador Z funciona sob ordem aplicativa, e ele não
funciona.**

```
    combinador          aplicativa       por valor
    Y                  nao termina     nao termina
    Z                  nao termina        terminou
```

A ordem aplicativa entra **dentro** de lambda, e a proteção do Z é exatamente um
lambda. O que o Z resolve é a estratégia **por valor**, que é a das linguagens de
verdade: ali função é valor pronto e o corpo não é tocado.

**Contar passos limita o trabalho e não limita a pilha, e a integração contínua
descobriu isso em dois dos três sistemas.** Todo percurso de termo aqui é
recursivo, e o combinador Y aplicado a K, sob ordem aplicativa, cresce em
**profundidade** muito mais depressa do que em número de nós: ele derrubou o
processo com 3.211 quadros de pilha, com o contador de passos longe do limite e o
tamanho ainda na casa dos milhares. O Linux aguentou e o macOS e o Windows não,
que é o pior jeito de um erro aparecer: o teste passa na máquina de quem
escreveu. São três limites diferentes, e os três precisam existir.

**E `m` elevado a zero não reduz para o numeral um.** Ele reduz para a
identidade, porque "zero aplicado a `m`" é "aplicar `m` zero vezes", que é não
fazer nada. `\x.x` e `\f.\x.f x` se comportam igual quando aplicadas e **não** são
o mesmo termo: a diferença entre as duas é a regra eta, que o cálculo lambda puro
não tem.

## Por que o juiz é o teorema

Sete termos escolhidos à mão não dizem nada sobre um teorema que fala de **todos**
os termos. Cinquenta mil termos sorteados sem cuidado nenhum dizem bem mais, e
foram eles que acharam os dois bugs da substituição: nenhum dos exemplos
escritos à mão reusava nome do jeito certo para quebrar.

O sorteio é de um gerador congruente escrito aqui, e não do sorteio da
biblioteca, cuja documentação não promete a mesma sequência entre versões nem
entre sistemas. Com o gerador próprio, a semente 7 produz exatamente os mesmos
termos no Linux, no Windows e no macOS.

Nenhuma medida aqui usa relógio. Todas são contagem de passos de redução, de
renomeações ou de termos conferidos.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Termo.cs` | as três formas, as variáveis livres e os atalhos de escrita |
| `Substituicao.cs` | a captura, as duas versões e a igualdade a menos de renomear |
| `Estrategia.cs` | ordem normal, aplicativa e por valor, um passo de cada vez |
| `Preguicosa.cs` | por necessidade, com caixa compartilhada |
| `Igreja.cs` | numerais, lógicos e as contas com resposta conhecida de fora |
| `Combinadores.cs` | I, K, S, omega, Y e Z |
| `ChurchRosser.cs` | o confronto entre estratégias, que é o juiz |
| `Sorteio.cs` | o gerador determinístico de termos |

## Como rodar

```
dotnet test testes/Reducao.Testes/Reducao.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `juiz`, `estrategias`, `captura`, `igreja`, `preguicosa`,
`recursao` e `tudo`.

## Licença

MIT.
