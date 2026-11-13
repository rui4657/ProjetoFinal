# Verificação sem Editor Unity

Este harness compila **os scripts reais do projeto** com Roslyn e executa 96 cenários de regressão. Não precisa de NuGet nem inclui dependências binárias novas.

Requisitos: Python 3, .NET SDK 8 ou superior e SQLite 3 nativo. Foi executado em Debian Linux; o runner também prevê a DLL SQLite x64 já existente no projeto para Windows, mas esse caminho não foi testado aqui.

```bash
python3 Tools/Verification/run.py
# SDK instalado numa pasta local:
python3 Tools/Verification/run.py --dotnet-root /caminho/para/dotnet
# Outro checkout:
python3 Tools/Verification/run.py /caminho/do/projeto --dotnet-root /caminho/para/dotnet
```

Os assemblies e bases de dados de teste são criados numa pasta temporária e apagados no fim. O runner usa `Mono.Data.SqliteClient.dll` que já pertence ao projeto, sem a substituir. Num checkout sparse pode obtê-la através de `git show HEAD:...`.

## O que é verificado

- **Waves — 9 cenários:** morte antes do fim dos spawns; conclusão exata de uma ronda; mortes duplicadas; limite de zombies ativos sem descartar a fila; falhas de spawn limitadas; configuração ausente; pausa; fim da partida; limites da curva de dificuldade.
- **Database — 15 cenários:** SQLite real, DLL real e `Database.cs`/`GameState.cs` reais; criação do schema sem inventar um save; snapshots; migrações dos formatos antigos e do progresso de mundo/tempo; idempotência; parâmetros SQL; cultura `pt-PT`; saves mortos; eliminação por slot; dados corrompidos; tempo inválido e flags. Inclui inventário/barricadas/atalhos, migração da AK e limites de texto/bits.
- **Gameplay — 72 cenários:** compila todos os scripts próprios em `Assets/Scripts`; economia, limites e compras; munições/recarga; debounce; morte/reset/restauro; ESC e regeneração; buffs e pausa; limite/expiração/oclusão dos drops; gravação de checkpoints/tempo; compras físicas com proximidade/LOS; requisitos de energia; posição inválida; melee; pose da arma; stagger; morte única; migração de postos; conforto; placement e objetivos. Abrange também variantes, ácido, headshots 5×, setores, colliders/obstacles, contenção, spawns e ragdoll. Os 18 cenários desta fase acrescentam pistola inicial, input semi/automático, conservação/substituição das duas armas, compras de munições, inventário inválido, projéteis da caçadeira/oclusão/estatísticas, recarga interrompível por cartucho, munições máximas nas duas armas, troca de modelos/muzzle, limite de recompensas, E mantido, ataques e transposição das janelas, entradas únicas, atalhos, caixa misteriosa e continuação completa via SQLite.

## Limites

**Isto não é um build nem um teste de execução no Unity.** `Gameplay/UnityStubs.cs` e os pequenos stubs das outras suites substituem exclusivamente APIs do motor para permitir executar lógica C# fora do Editor. O scheduler de coroutines é simulado manualmente: não valida os tempos reais de `WaitForSeconds`. Física, NavMesh, input, desenho, eventos visuais e áudio não são simulados de forma fiel e não são considerados validados por estes testes.

O grupo Database usa SQLite real. Os outros grupos executam os métodos reais do jogo, mas com objetos do motor simulados. Ainda é necessário abrir no Unity **6000.2.8f1**, aguardar o import, confirmar a Console, testar as cenas `MainMenu` e `MainScene`, verificar colisões/LOS, navegação, HUD em várias resoluções, áudio e fazer um build de Windows.

`Assets/Editor/DeadlineValidation.cs` não pertence ao harness: é um comando do Editor que verifica assets importados,
skins e rig; durante Play acrescenta verificações de colisão/navegação dos portões e estado dos cadáveres.
Não substitui observar as quedas nem percorrer os perímetros no terreno real.
