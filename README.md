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
| E junto ao posto | Comprar até 90 munições de reserva por 250 pontos |
| B entre rondas | Abrir / fechar o arsenal; a partida fica em pausa |
| Enter entre rondas | Saltar a preparação |
| Esc | Pausa / opções; fechar o arsenal |

## Progressão

Começas com **500 pontos**, AK, 30 balas no carregador e 120 de reserva.
Um acerto não letal dá 10 pontos; uma eliminação dá 60, ou 100 com headshot.
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

## Guardar e continuar

Existe um slot de recuperação SQLite, em `Application.persistentDataPath/savegame.db`.
O jogo agrupa alterações durante 2 segundos, grava periodicamente e ao pausar ou sair em vida.
Continuar restaura vida, posição, arma, munições, ronda, pontos, eliminações e melhorias;
**a ronda atual recomeça com uma nova onda completa**. O tempo do resumo refere-se à sessão atual.
Uma morte termina a partida e elimina a recuperação; os recordes de ronda e eliminações permanecem.
Os saves anteriores são migrados automaticamente, acrescentando os campos de progressão.

## Desenvolvimento e validação

Os novos HUD, arsenal, feedback sonoro e tema do menu são criados automaticamente ao carregar as cenas.
Não é necessário ligar componentes novos no Inspector.
Os scripts antigos mantêm os campos e métodos usados pelas referências das cenas.

Consulta [a revisão técnica e os testes de aceitação](Docs/REVISAO_E_VALIDACAO.md).
O harness em `Tools/Verification` testa a lógica fora do Unity e descreve os seus limites.
Uma compilação Windows e um playtest no Editor continuam necessários para validar renderização,
animações, áudio, navegação real e o equilíbrio final.
