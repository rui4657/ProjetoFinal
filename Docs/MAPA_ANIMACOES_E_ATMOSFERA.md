# Objetivos, animações e atmosfera

## O que esta revisão muda

O mapa Flooded Grounds é preservado. Os novos pontos de interesse usam os móveis do próprio pack,
com placas, luzes e comandos próprios. Não há novos modelos de armas ou animações comprados/importados;
o trabalho aproveita os clips existentes e acrescenta movimento procedural à arma.
As alterações posteriores de zonas fechadas, variantes e morte física estão detalhadas em
[Zonas, variantes e mortes](ZONAS_E_VARIANTES.md).

### Progressão física

| Local | Interação | Resultado |
| --- | --- | --- |
| Perto do spawn | Reabastecimento500 | Carregador e reserva completos |
| Entrada do pátio da igreja | Abrir750 ou chegar à ronda3 | Abre o portão físico e desbloqueia o arsenal; requer energia para melhorias |
| Portão da vila | Abrir1250 ou chegar à ronda5 | Dá acesso à vila e ao posto de guarda |
| Saídas exteriores | Chegar à ronda8 | Abre os restantes acessos ao mapa |
| Posto de guarda | Ativar1250 | Liga energia para arsenal e vantagens |
| Igreja | Melhorar arma | Até3níveis de dano |
| Posto de guarda | Fortitude / Recarga Rápida | Vida máxima / recarga mais rápida |

As posições partem dos landmarks reais da cena: spawn≈(445.37,17.83,555.5), acesso da igreja≈(455.97,17.13,563.28)
e posto de guarda≈(466.37,17.99,521.04). Os postos são projetados para o NavMesh e procuram ligação ao spawn.
O objetivo atual, direção relativa e distância aparecem no HUD. Uma única seleção central trata o E,
para evitar que dois postos cobrem no mesmo frame. Distância, direção da câmara e obstáculos são verificados.

Os setores têm vedações visíveis, colisão e obstáculos com carving no NavMesh existente.
O gerador tem uma compra separada da abertura da vila. Se o NavMesh não permitir colocar um posto essencial,
o posto não é inventado num local inacessível:
o HUD informa que o arsenal B está disponível entre rondas, permitindo continuar a progressão.
O antigo reabastecimento usava um helper humano a cerca de125m do spawn; passa a equipamento visível,
e existe um posto inicial adicional para não depender dessa deslocação.

### Animações e combate

- O modelo AK estava separado do GameObject GunSystem. O novo pivot segue o ramo real WeaponHolder e preserva a pose original.
- Marcha, respiração, sway e corrida usam offsets aditivos; a mira alinha a referência rearSight do modelo.
- Recarga faseada: baixar/inclinar, retirar carregador, voltar a inserir, puxar a culatra e recuperar.
- Os clips originais de disparo e culatra são usados; o disparo regressa ao idle para evitar loop contínuo.
- V executa um golpe curto com alcance2,1m, cooldown0,8s e recuperação0,28s. Verifica obstáculos, cancela a recarga e não gasta balas.
- O controller de zombie tinha ataque a3×; o ataque passa a2×. Caminhada/corrida e reação a dano usam os clips existentes.
- Corredores usam corrida; infetados, cuspidores e brutamontes mantêm passada de caminhada ajustada à velocidade do NavMesh.
- A morte desliga o Animator e ativa um ragdoll nos ossos deformadores. O corpo cai, permanece cerca de6s e afunda antes de ser removido. Não depende dos clips de morte, que anteriormente deixavam o modelo numa pose aberta.
- Ferimentos podem interromper brevemente o ataque, com cooldown para evitar stun-lock; a cor varia por MaterialPropertyBlock sem duplicar materiais.

### Ambiente e conforto

A cena tinha fog linear de0 a20m e luz direta extremamente fraca. O perfil de jogo passa para fog55–175m,
ambiente frio e luz direta moderada, conservando a direção da luz original. Foram retirados os pontos quentes
adicionais nos landmarks e a poeira procedural; as pequenas luzes dos postos comunicam apenas o seu estado.
O sky original é clonado em runtime, sem alterar o material do asset. A câmara conserva FOV e usa clipping0,08–300m.
Uma camada subtil de vento respeita o volume principal e a pausa.

A opção Movimento reduzido reduz movimentos secundários da arma e dos power-ups.
Não se usam flashes rápidos para sinalizar expiração: os power-ups mostram os segundos finais em texto.
Materiais, meshes, texturas e áudio gerados são libertados ao sair da cena.

### Gravação

SQLite acrescenta unlocked_zones, power_enabled e elapsed_seconds através de migrações idempotentes.
O carregamento restaura checkpoints/energia antes de validar a posição. O tempo do resumo passa a incluir
sessões continuadas; buffs temporários ficam excluídos. Dados antigos continuam a poder ser carregados.

## Validação no Editor

Os testes fora do Unity verificam lógica, persistência e contratos entre scripts. Não avaliam o aspeto final,
a física, os importers FBX, as transições reais do Animator ou a navegação real.
Passaram75cenários (9rondas,12SQLite real e54gameplay). Referências de skins e ossos foram inspecionadas
nos prefabs. Isso não comprova a aparência nem a estabilidade física no Editor.

1. Fazer Fetch/Pull da branch e deixar o Unity6000.2.8f1 reimportar o controller e o meta de run.
2. Abrir MainMenu e iniciar; confirmar que o horizonte, os zombies e os caminhos são legíveis com o novo fog.
3. Seguir o HUD, abrir igreja e vila por compra ou pelas rondas3/5 e ligar energia no posto de guarda. Confirmar que os portões fechados bloqueiam a passagem e os abertos permitem atravessar.
4. Tentar E fora de alcance, a olhar de costas e através de paredes. Cada tecla deve executar uma só compra válida.
5. Experimentar upgrades antes/depois da energia e confirmar que B usa os mesmos requisitos.
6. Recarregar, correr, apontar, disparar e usar V. Confirmar que carregador/culatra se movem corretamente e que a arma regressa à pose original.
7. Ver zombies a caminhar/correr, ferir e morrer com tiro normal/headshot. Afinar a correspondência entre pé, velocidade e chão caso necessário.
8. Recolher cada power-up e confirmar duração, pontuação, munições e suspensão durante pausa. Não devem ser recolhidos através de paredes.
9. Sair em vida e continuar; confirmar checkpoints, energia, tempo e posição. Morrer deve invalidar a recuperação como na revisão anterior.
10. Ativar Movimento reduzido em ambos os menus; verificar conforto, resposta ao rato e ausência de sobreposição com Voltar.
11. Fazer build Windows, comparar frame time antes/depois e verificar texturas das variantes/móveis, mix de áudio, navegação e estabilidade dos ragdolls.

Os novos elementos de mapa e atmosfera são instalados em runtime: surgem ao jogar, sem exigir montagem manual no Inspector.
