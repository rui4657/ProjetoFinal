# Zonas, variantes e mortes

A fase seguinte acrescenta [mapa, barricadas e armas](MAPA_BARRICADAS_E_ARMAS.md): a partida nova começa com
PX-9, há dois espaços de equipamento, 11 entradas reparáveis e dois atalhos. Os portões descritos abaixo mantêm-se.

Esta revisão responde ao mapa livre apesar dos checkpoints, à pouca dificuldade e aos cadáveres em pose aberta.
O cenário Flooded Grounds é conservado, mas passa a ter limites físicos e progressão de áreas.

## Abertura do mapa

| Área | Abertura automática | Antecipar com pontos |
| --- | --- | --- |
| Pátio inicial | Disponível desde o início | — |
| Igreja | Início da ronda 3 | 750, no portão da igreja |
| Vila e posto de guarda | Início da ronda 5 | 1250, depois de abrir a igreja |
| Saídas para o resto do mapa | Início da ronda 8 | — |

São vedações do pack original com colisão contínua e obstáculos de NavMesh com carving.
As portas rodam ao abrir e desligam simultaneamente a colisão e o bloqueio da navegação.
O acesso da igreja fica perto do arco original; a saída sul leva à vila e ao posto de guarda.
Um controlo de limites recupera a última posição permitida se o jogador atravessar uma falha do terreno.
Os postos indicam preço/ronda/estado; as luzes adicionais espalhadas pelos landmarks foram retiradas.

O ponto inicial passa para aproximadamente (445,37; 17,83; 555,5), a sul do portão da igreja.
As regras de área são partilhadas pelos portões, geração de inimigos e validação de posições guardadas.
Abrir a vila não liga a energia: o gerador continua a custar 1250 pontos, separadamente.
Os desbloqueios antigos são preservados. Para ver o início fechado, escolher **INICIAR JOGO**;
continuar um save da ronda 8 ou superior mantém o mapa aberto.

## Inimigos

Valores base, antes do crescimento de vida e velocidade com a ronda:

| Tipo | Surge desde | Vida | Dano | Velocidade | Comportamento e aparência |
| --- | --- | --- | --- | --- | --- |
| Infetado | Ronda 1 | 100 | 28 | 3,1 | Perseguição e ataque próximo; pele original 1 |
| Corredor | Ronda 2 | 70 | 20 | 6,2 | Corrida e ataque rápido, stagger curto; pele 2 e corpo mais estreito |
| Cuspidor | Ronda 3 | 95 | 24 | 3,0 | Mantém distância, recua e lança ácido; pele 3 e glândulas verdes |
| Brutamontes | Ronda 5 | 330 | 45 | 2,7 | Maior alcance, golpe lento, sem stagger; corpo maior, armadura e capacete |

São **quatro prefabs separados**, três conjuntos de texturas já incluídos no projeto e peças adicionais
presas aos ossos. Partilham o mesh/rig original; não foram importados quatro personagens novos.
Só o cuspidor lança projéteis. O lançamento tem antecipação de 0,8 s e o ácido viaja até à posição
do jogador no momento do disparo: pode ser evitado, é bloqueado por paredes e para durante a pausa.
No máximo ficam ativos três cuspidores e dois brutamontes; há limite global de 28 inimigos.

A primeira ronda tem 12 inimigos, as ondas crescem mais e o intervalo de geração diminui com as rondas.
A regeneração só começa após 6,5 s sem dano e recupera 8 de vida por segundo.
A AK tem 20 de dano, com **headshots a 5×**; a cabeça do esqueleto tem hitbox própria.

## Geração e morte

O spawner usa a triangulação do NavMesh e candidatos em todas as direções em torno do jogador,
entre 8 e 42 m, em vez dos três pontos fixos. Rejeita áreas fechadas, posições ocupadas e caminhos
incompletos ou que atravessam setores fechados. Prefere locais fora de vista; candidatos visíveis
de recurso continuam a pelo menos 12 m. A área de procura acompanha o jogador pelo mapa aberto.
Isto depende do NavMesh existente: superfícies sem bake ou sem caminho não são pontos válidos.

A morte deixa de depender dos clips death1/death2. Desliga o Animator, conserva a pose visível
e ativa corpos/juntas em 11 ossos deformadores do modelo. O impacto aplica impulso, o corpo cai
e fica no chão cerca de 6 s antes de afundar durante 1,5 s. Há limite de 12 cadáveres.
Os cadáveres não bloqueiam tiros nem o jogador. Um prefab personalizado sem o rig esperado usa
uma queda simples de fallback, em vez de aguardar de pé numa pose aberta.

## Verificar no Unity 6000.2.8f1

Passaram **96 testes fora do Unity**: 9 de rondas, 15 com SQLite real e 72 de gameplay com APIs
do motor substituídas. Validam lógica e transições de estado, sem simular fisicamente os ragdolls,
o carving ou os importers. Não foi possível executar o Editor nem criar uma build neste ambiente.

1. Fechar o Unity, fazer Fetch/Pull de `codex/zombies-survival-upgrade` e voltar a abrir o projeto.
2. Aguardar o import e executar **Tools → DEADLINE → Verificar zonas e variantes**. Deve encontrar os quatro prefabs, três peles e ossos da cabeça/anca.
3. Abrir MainMenu e escolher **INICIAR JOGO**. Percorrer o perímetro inicial, incluindo cantos/desníveis: o jogador não deve sair pelas vedações nem pelo portão fechado.
4. Repetir o comando durante Play. Além dos assets, verifica colliders, obstáculos e raios físicos dos portões.
5. Comprar a igreja ou chegar à ronda 3; atravessar a passagem. Repetir para a vila na ronda 5 e saídas na ronda 8. Confirmar também que os zombies atravessam portas abertas.
6. Observar as variantes desde as rondas 2/3/5. Identificar as peles/silhuetas e verificar que só o cuspidor lança ácido; esquivar e usar uma parede como cobertura.
7. Comparar tiros no torso/cabeça; um infetado da ronda 1 morre com um headshot da pistola inicial. Observar vários cadáveres a cair, incluindo perto de degraus e paredes.
8. Repetir o comando com cadáveres presentes. A pose final precisa de inspeção visual, mesmo sem erros no comando.
9. Dar a volta às zonas abertas, observar a diversidade de spawns, sair em vida e continuar. Confirmar zona, energia, posição e pausa.
10. Fazer build Windows e observar desempenho com 28 inimigos e vários cadáveres. Ajustes de colocação/física só podem ser confirmados no mapa real.
