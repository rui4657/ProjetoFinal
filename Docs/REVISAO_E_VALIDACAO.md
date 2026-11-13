# Revisão de sobrevivência

## Âmbito

Revisão do código próprio, das cenas MainMenu e MainScene, do prefab de zombie usado pelo spawner,
das referências de UI, do NavMesh e das configurações de input/build/renderização.
Os assets de terceiros são preservados; não foi feita uma conversão do mapa ou do pipeline gráfico.

## Correções e melhorias

- **Rondas:** a morte do primeiro zombie entre dois spawns já não completa a ronda. A conclusão exige que a quota esteja toda gerada e não sobrem vivos. As mortes são notificadas uma vez, há limite de 28 ativos e falhas repetidas de spawn interrompem a geração sem atribuir bónus por rondas vazias. A primeira onda tem 12 inimigos.
- **Spawn e IA:** validação de NavMesh e de caminho até ao jogador, escolha de pontos acessíveis, progressão limitada de vida/velocidade e atualização de percursos a cada 0,2 segundos. O ataque tem antecipação e volta a verificar alcance e obstáculos no momento do dano.
- **Tiros:** a máscara original incluía apenas a layer dos inimigos e ignorava paredes. Agora o cenário bloqueia disparos e os colliders do próprio jogador são ignorados. Acertos em filhos encontram o zombie na hierarquia. A cápsula do prefab passa de raio 0,1 para 0,32.
- **Combate:** zoom ao apontar, dispersão diferenciada, recoil suave, headshot a 5×, confirmação visual e sonora. A cabeça do rig tem hitbox própria. Tracers usam temporização, os impactos são destruídos e a recarga pode ser cancelada com segurança.
- **Jogador:** sprint com stamina, aceleração, movimento diagonal limitado, teletransporte seguro, sensibilidade coerente entre menus e regeneração de vida com atraso. A morte bloqueia a partida uma única vez.
- **Progressão:** pontos por acerto/eliminação e ronda; compra de munições no posto; arsenal entre rondas com três níveis de dano, duas melhorias de vida e recarga rápida. Saldo e melhorias são guardados.
- **Zonas e variantes:** perímetros físicos com NavMeshObstacle, aberturas nas rondas 3/5/8 e compras antecipadas; geração sobre o NavMesh das áreas abertas; quatro prefabs com três conjuntos de pele, velocidades, resistência e ataques distintos; ácido com colisão varrida; ragdoll nos ossos que deformam o modelo. Ver [a revisão detalhada](ZONAS_E_VARIANTES.md).
- **Apresentação:** tema do menu com carvão, vermelho e âmbar; HUD que respeita a área segura do ecrã, barras de vida/stamina/recarga, inimigos restantes, prompts de interação, hitmarkers, indicação de dano e resumo de morte com retry/menu. Os novos sinais sonoros são gerados localmente.
- **Gravações:** deixam de fazer I/O por cada bala. Um único carregamento central aplica o estado e consome a flag, eliminando a disputa de inicialização entre vida, arma e posição. SQLite usa parâmetros, transações e migrações idempotentes, tratando erros sem bloquear a partida.
- **Pausa e saída:** ESC não abre a pausa ao fechar o arsenal, perder foco pausa a partida, sair em vida grava o estado, e terminar a partida apaga a recuperação. As rondas param antes de descarregar a cena.
- **Repositório:** `.gitignore` normalizado para UTF-8, removendo bytes nulos e mantendo as exclusões Unity.
- **Compatibilidade do Inspector:** o drawer de mínimos do PostProcessing antigo passa a usar o atributo do próprio pacote, em vez do homónimo de UnityEngine. Os exemplos TMP foram verificados contra os tipos atuais de uGUI 2.0.0.

## Decisões de compatibilidade

- Preservam-se GUIDs, nomes de campos serializados e métodos chamados pelos botões.
- Os novos elementos são instalados em runtime nas cenas existentes; não é necessário remontar a cena.
- O HUD antigo é ocultado apenas no seu próprio ramo; os painéis de pausa continuam ativos.
- O renderer continua Built-in. O pacote HDRP já existente não é ativado automaticamente.
- Continuar reinicia a onda da ronda guardada, mantendo o progresso. Não serializa posições/vida individuais de cada zombie.
- O tempo acumulado da partida, checkpoints e energia são preservados ao continuar; efeitos temporários ficam excluídos.
- Os headshots usam a cabeça real do rig; só recorrem à parte superior da cápsula num prefab sem esse osso.

## Verificação automatizada

O harness `Tools/Verification` compila os scripts próprios com Roslyn e substitutos mínimos para APIs Unity;
testa métodos e coroutines reais com um relógio/scheduler controlado. A persistência usa a DLL SQLite original
do projeto e SQLite nativo, incluindo migrações, cultura portuguesa, parâmetros e ficheiros inválidos.
O README do harness explica os cenários e o comando de execução.

Resultado atual: **75 testes passaram** (9 de rondas, 12 de SQLite e 54 de integração de gameplay).
Uma verificação adicional confirmou o registo e o cast do drawer PostProcessing corrigido.

Isto verifica lógica e contratos entre os scripts. **Não equivale a compilar ou executar o jogo no Unity:**
os substitutos não exercitam física, NavMesh, renderização, input real ou animação.
O ambiente usado para esta revisão não tem Unity Editor nem licença instalada.

## Aceitação no Unity 6000.2.8f1

1. Abrir MainMenu, verificar que a Console não tem erros e que iniciar/opções/continuar/apagar/sair respondem.
2. Iniciar uma partida: HUD legível, 500 pontos, vida100, AK30/120; experimentar preparação e Enter.
3. Mover, saltar, correr até esgotar stamina, recuperar e apontar. Comparar mira a diferentes framerates.
4. Disparar contra zombies, incluindo cabeças e alvos atrás de paredes; confirmar pontuação, recoil, hitmarkers e áudio.
5. Matar rapidamente o primeiro zombie antes do spawn seguinte: a ronda deve continuar até terminar toda a quota.
6. Terminar duas ou mais rondas: verificar bónus, preparação12s, aumento de dificuldade e contador de inimigos.
7. Abrir B durante a preparação; comprar cada melhoria com saldo suficiente e insuficiente. Fechar com B/Esc e confirmar que a pausa não abre por engano.
8. Testar E nos postos com reserva cheia, parcial e vazia; confirmar débito500 para reabastecimento completo e limite300, sem múltiplas cobranças no mesmo toque.
9. Levar dano, evitar novo ataque por6,5s e observar a regeneração; pausar não deve avançar a recuperação ou ataques.
10. Sair em vida e continuar: confirmar posição, vida máxima, arma, munições, pontos, melhorias e ronda.
11. Morrer: nenhum movimento/disparo/spawn adicional, um único resumo, recuperação eliminada, retry começa novo com500 pontos.
12. Testar 1280×720, 1920×1080 e ultrawide; menu, arsenal e resumo devem caber e responder ao rato.
13. Fazer uma build Windows e comparar com o Editor, sobretudo áudio3D, DLL SQLite, NavMesh e sombras do cenário.

## Conteúdo e validação adicional

Esta revisão inclui áreas fechadas, portas compráveis, inimigos especiais e quedas físicas, descritos em
[Zonas e variantes](ZONAS_E_VARIANTES.md). Os comportamentos e contratos têm testes fora do Editor;
vedações sobre terreno, navegação após carving, skins importadas e estabilidade dos ragdolls exigem o playtest
real descrito nesse documento. Novas armas e modelos totalmente distintos não fazem parte desta revisão.
