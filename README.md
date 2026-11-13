# DEADLINE — Zombie Survival

Sobrevive a rondas de zombies, ganha pontos e melhora o equipamento entre rondas.
Projeto Unity 3D a solo, com o mapa e os modelos originais do projeto.

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
| Clique esquerdo | Disparar; manter premido para fogo automático |
| Clique direito | Apontar: menos dispersão, menor sensibilidade e zoom |
| R | Recarregar |
| Shift esquerdo | Sprint enquanto houver stamina |
| Espaço | Saltar |
| E a olhar para um posto | Interagir, desbloquear um objetivo ou comprar equipamento |
| V | Ataque corpo a corpo, com recuperação curta e alcance limitado |
| B entre rondas | Abrir / fechar o arsenal; a partida fica em pausa |
| Enter entre rondas | Saltar a preparação |
| Esc | Pausa / opções; fechar o arsenal |

## Progressão

Começas com **500 pontos**, AK, 30 balas no carregador e 120 de reserva.
Um acerto não letal dá 10 pontos; uma eliminação dá 60, 100 com headshot ou 130 em corpo a corpo.
Completar uma ronda dá 100 + 25 × ronda, com o bónus limitado a 1350.

O arsenal abre durante os 12 segundos entre rondas:

| Compra | Custo | Efeito |
| --- | --- | --- |
| Munições | 500 | Carregador e reserva completos |
| Arma reforçada | 1500 / 3000 / 4500 | +50% do dano base por nível, até 2,5× |
| Fortitude | 2000 por melhoria | +50 vida máxima, até duas melhorias |
| Recarga rápida | 1500 | Recarga 35% mais rápida |

Os zombies ganham vida e velocidade com as rondas. Existem no máximo 24 ativos simultaneamente;
os restantes ficam por gerar, sem desaparecerem da quota da ronda.
Depois de 5 segundos sem sofrer dano, a vida regenera gradualmente.
O HUD mostra vida, stamina, munições, recarga, pontos, inimigos restantes e confirmação de acerto/headshot.

## Objetivos no mapa

O mapa original ganha postos físicos, sinalização e orientação no HUD:

1. **Reclama o pátio da igreja por 750 pontos**, junto ao acesso da igreja. Desbloqueia o terminal do arsenal.
2. **Ativa a energia no posto de guarda por 1250 pontos**. A energia permite usar as melhorias.
3. Usa **E** a olhar para os postos: arsenal na igreja e Fortitude/Recarga Rápida junto ao posto de guarda.

O reabastecimento inicial fica perto do ponto de partida: **500 pontos** enchem carregador e reserva.
O antigo placeholder de reabastecimento é substituído por equipamento visível, mantendo o local.
O arsenal de **B** continua disponível entre rondas, com os mesmos requisitos de igreja/energia.
Os objetivos desbloqueiam serviços; não criam paredes invisíveis nem substituem o cenário por uma arena nova.

## Power-ups, animações e visual

Os zombies podem deixar **munições máximas**, **pontos a dobrar por 30 segundos** e **morte instantânea por 10 segundos**.
Aproxima-te para recolher. Os efeitos e a expiração do objeto ficam suspensos durante a pausa.
O HUD mostra a duração; os efeitos temporários não são guardados ao sair.

A arma tem movimento de marcha/corrida, mira alinhada com o modelo, recoil e recarga por fases com movimento do carregador.
Os zombies usam corrida, reação a ferimentos e duas mortes, com velocidade de passada ajustada ao movimento.
Foram corrigidas a referência da animação de morte e a remoção demasiado precoce do corpo.

A atmosfera mantém os assets do mapa e usa luz fria legível, contraste quente nos objetivos,
nevoeiro de distância, poeira discreta e vento subtil. Estas alterações aparecem **ao entrar em Play**.
Em **Opções**, ativa **Movimento reduzido da arma** para reduzir bob/sway e movimento dos power-ups e desligar a poeira.

## Guardar e continuar

Existe um slot de recuperação SQLite, em `Application.persistentDataPath/savegame.db`.
O jogo agrupa alterações durante 2 segundos, grava periodicamente e ao pausar ou sair em vida.
Continuar restaura vida, posição válida, arma, munições, ronda, pontos, eliminações, melhorias, objetivos, energia e tempo acumulado;
**a ronda atual recomeça com uma nova onda completa**. Uma posição inválida conserva o ponto inicial do mapa.
Uma morte termina a partida e elimina a recuperação; os recordes de ronda e eliminações permanecem.
Os saves anteriores são migrados automaticamente, acrescentando os campos de progressão.

## Desenvolvimento e validação

Os novos HUD, arsenal, feedback sonoro e tema do menu são criados automaticamente ao carregar as cenas.
Não é necessário ligar componentes novos no Inspector.
Os scripts antigos mantêm os campos e métodos usados pelas referências das cenas.

Consulta [a revisão técnica e os testes de aceitação](Docs/REVISAO_E_VALIDACAO.md).
Consulta também [o mapa de objetivos, as animações e a atmosfera](Docs/MAPA_ANIMACOES_E_ATMOSFERA.md).
O harness em `Tools/Verification` testa a lógica fora do Unity e descreve os seus limites.
Uma compilação Windows e um playtest no Editor continuam necessários para validar renderização,
animações, áudio, navegação real e o equilíbrio final.
