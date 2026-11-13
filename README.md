# DEADLINE — Zombie Survival

Sobrevive a rondas de zombies, ganha pontos e melhora o equipamento entre rondas.
Projeto Unity 3D a solo no mapa Flooded Grounds.

## Abrir e jogar

1. No Unity Hub, instala **Unity 6000.2.8f1** e adiciona a pasta principal deste repositório.
2. Deixa o Unity importar os assets e resolver os pacotes.
3. Abre **Assets/Scenes/MainMenu.unity** e carrega em Play.
4. Escolhe **INICIAR JOGO**. A preparação inicial dura 8 segundos; Enter permite começar imediatamente.

O projeto principal é a pasta que contém este README. A subpasta antiga `My project` não é o jogo principal.
O renderer ativo é Built-in; não é necessário converter os materiais para HDRP.
As cenas MainMenu e MainScene já estão na lista de build.

## Controlos

| Tecla | Ação |
| --- | --- |
| WASD / rato | Mover / olhar |
| Clique esquerdo | Disparar; manter premido na SM-40 e AK-74 |
| Clique direito | Apontar: menos dispersão, menor sensibilidade e zoom |
| R | Recarregar |
| 1 / 2 | Equipar o primeiro / segundo espaço de arma |
| Q / roda do rato | Alternar entre as duas armas |
| Shift esquerdo | Sprint enquanto houver stamina |
| Espaço | Saltar |
| E a olhar para um posto | Interagir, desbloquear um objetivo ou comprar equipamento |
| Manter E a olhar para uma barricada | Reparar uma tábua a cada 0,8 segundos |
| V | Ataque corpo a corpo, com recuperação curta e alcance limitado |
| B entre rondas | Abrir / fechar o arsenal; a partida fica em pausa |
| Enter entre rondas | Saltar a preparação |
| Esc | Pausa / opções; fechar o arsenal |

## Progressão

Começas com **500 pontos**, pistola **PX-9**, 12 balas no carregador e 60 de reserva.
Um acerto não letal dá 10 pontos; uma eliminação dá 60, 100 com headshot ou 130 em corpo a corpo.
Completar uma ronda dá 100 + 25 × ronda, com o bónus limitado a 1350.

O arsenal abre durante os 12 segundos entre rondas:

| Compra | Custo | Efeito |
| --- | --- | --- |
| Munições | 500 | Carregador e reserva completos |
| Arma reforçada | 1500 / 3000 / 4500 | +50% do dano base por nível, até 2,5× |
| Fortitude | 2000 por melhoria | +50 vida máxima, até duas melhorias |
| Recarga rápida | 1500 | Recarga 35% mais rápida |

Os zombies ganham vida e velocidade com as rondas. A primeira onda tem 12 inimigos; existem no máximo 28 ativos simultaneamente;
os restantes ficam por gerar, sem desaparecerem da quota da ronda.
Depois de 6,5 segundos sem sofrer dano, a vida regenera a 8 pontos por segundo. Headshots causam **5× dano**.
O HUD mostra vida, stamina, munições, recarga, pontos, inimigos restantes e confirmação de acerto/headshot.

## Armas e barricadas

Podes transportar **duas armas**, cada uma com carregador e reserva próprios:

| Arma | Onde comprar | Preço | Munições no posto da arma | Característica |
| --- | --- | --- | --- | --- |
| PX-9 | Arma inicial | — | — | Semiautomática, 12 balas |
| SM-40 | Pátio inicial | 1200 | 450 | Automática rápida, 32 balas |
| SG-8 | Igreja | 1500 | 500 | 8 projéteis por tiro, 8 cartuchos; recarga cartucho a cartucho |
| AK-74 | Vila / posto de guarda | 1800 | 600 | Automática com mais alcance, 30 balas |

A primeira compra ocupa o segundo espaço; uma terceira arma substitui a equipada. Os postos passam a vender
munições dessa arma quando já a tens. O arsenal B e os postos genéricos de 500 pontos abastecem a arma equipada;
o power-up de munições máximas abastece as duas. Na caçadeira, disparar após carregar um cartucho interrompe a recarga.
A caixa misteriosa na vila custa **950 pontos** e entrega uma SM-40, SG-8 ou AK-74 que ainda não tenhas.

As **11 barricadas** têm seis tábuas: quatro entradas no início, mais três na igreja e quatro na vila.
Os zombies aproximam-se, arrancam tábuas e transpõem o peitoril antes de começar a perseguição normal.
Manter E repõe tábuas, com **10 pontos por tábua e até 120 pontos base de reparação por ronda**.
A reparação continua disponível depois desse limite. Os spawns normais no NavMesh complementam estas entradas.

## Objetivos no mapa

O mapa original fica dividido por **vedações visíveis e portões com colisão e bloqueio de NavMesh**:

1. Começas num pátio fechado a sul da entrada da igreja.
2. **Igreja: ronda 3**, ou abre antes por **750 pontos** com E no portão assinalado.
3. **Vila: ronda 5**, ou abre antes por **1250 pontos**, depois da igreja.
4. **Saídas para o resto do mapa: ronda 8**. Os portões interiores continuam utilizáveis.
5. Na vila, **liga a energia no posto de guarda por 1250 pontos**. É uma compra separada dos portões e permite usar as melhorias.
6. Usa **E** nos postos: arsenal na igreja e Fortitude/Recarga Rápida junto ao posto de guarda.

Depois de abrir a vila, dois **atalhos laterais de 650 pontos** criam percursos alternativos à volta da igreja.
Os atalhos são compras separadas dos portões automáticos. Placas indicam os setores, os percursos e o equipamento.
Consulta [a planta e os detalhes de mapa, barricadas e armas](Docs/MAPA_BARRICADAS_E_ARMAS.md).

O reabastecimento inicial fica perto do ponto de partida: **500 pontos** enchem carregador e reserva.
O antigo placeholder de reabastecimento é substituído por equipamento visível, mantendo o local.
O arsenal de **B** continua disponível entre rondas, com os mesmos requisitos de igreja/energia.
Os inimigos surgem em posições navegáveis de todas as direções das áreas abertas, com caminho até ao jogador.
As zonas fechadas ficam excluídas do spawn; a distância e a visibilidade limitam aparecimentos à tua frente.

## Power-ups, animações e visual

Os zombies podem deixar **munições máximas**, **pontos a dobrar por 30 segundos** e **morte instantânea por 10 segundos**.
Aproxima-te para recolher. Os efeitos e a expiração do objeto ficam suspensos durante a pausa.
O HUD mostra a duração; os efeitos temporários não são guardados ao sair.

A arma tem movimento de marcha/corrida, mira alinhada com o modelo, recoil e recarga por fases com movimento do carregador.
Existem quatro prefabs de inimigo com comportamentos próprios: **infetado**, **corredor** (ronda 2),
**cuspidor de ácido** (ronda 3) e **brutamontes** (ronda 5). Usam três conjuntos de texturas originais,
proporções diferentes e equipamento adicional no brutamontes/cuspidor; partilham o rig original.
Na morte, o Animator desliga e o esqueleto cai com física; o corpo permanece no chão antes de afundar e ser removido.

A atmosfera mantém a direção da luz original, nevoeiro mais distante e vento subtil. Foram retiradas as luzes
adicionais dos landmarks e a poeira procedural. As pequenas luzes dos postos identificam o seu estado.
Estas alterações aparecem **ao entrar em Play**. Em **Opções**, ativa **Movimento reduzido da arma** para reduzir bob/sway e movimento dos power-ups.

## Guardar e continuar

Existe um slot de recuperação SQLite, em `Application.persistentDataPath/savegame.db`.
O jogo agrupa alterações durante 2 segundos, grava periodicamente e ao pausar ou sair em vida.
Continuar restaura vida, posição válida, arma, munições, ronda, pontos, eliminações, melhorias, objetivos, energia e tempo acumulado;
**a ronda atual recomeça com uma nova onda completa**. Uma posição inválida conserva o ponto inicial do mapa.
Uma morte termina a partida e elimina a recuperação; os recordes de ronda e eliminações permanecem.
Os saves anteriores são migrados automaticamente, acrescentando os campos de progressão.
O inventário das duas armas, o espaço ativo, as tábuas/recompensas de reparação e os atalhos também são guardados.
Saves antigos com AK mantêm essa arma e as suas munições num único espaço.
Para experimentar os portões todos fechados, escolhe **INICIAR JOGO**: continuar preserva as áreas já desbloqueadas
e abre também as previstas para a ronda guardada.

## Desenvolvimento e validação

Os novos HUD, arsenal, feedback sonoro e tema do menu são criados automaticamente ao carregar as cenas.
Não é necessário ligar componentes novos no Inspector.
Os scripts antigos mantêm os campos e métodos usados pelas referências das cenas.

Consulta [a revisão técnica e os testes de aceitação](Docs/REVISAO_E_VALIDACAO.md).
Consulta também [o mapa de objetivos, as animações e a atmosfera](Docs/MAPA_ANIMACOES_E_ATMOSFERA.md).
Os detalhes desta revisão estão em [zonas, variantes e mortes](Docs/ZONAS_E_VARIANTES.md).
No Editor, **Tools → DEADLINE → Verificar zonas e variantes** verifica os assets importados; repete durante Play para verificar os portões com a física real.
O comando **Tools → DEADLINE → Verificar mapa, barricadas e armas** acrescenta verificações das entradas e do inventário ativo.
O harness em `Tools/Verification` testa a lógica fora do Unity e descreve os seus limites.
Uma compilação Windows e um playtest no Editor continuam necessários para validar renderização,
animações, áudio, navegação real e o equilíbrio final.
