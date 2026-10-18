# Contexto para agentes de IA neste repositório

ONE MORE TIME é um protótipo de game jam em Unity 6 (6000.3.25f1), URP, Input System.
Antes de qualquer mudança, leia [`docs/gdd.md`](docs/gdd.md) — o GDD de uma página — e,
se a tarefa tocar uma área específica, o capítulo correspondente em [`docs/gdd/`](docs/gdd/).

## Regras

1. **O GDD descreve o jogo como ele é.** Quando uma mudança no código ou nos assets fizer o
   jogo diferir do que o GDD diz (regra, número, controle, inimigo, armadilha, combo, chefe,
   HUD, escopo, arquitetura), atualize o capítulo afetado em `docs/gdd/` **no mesmo commit**.
   Se a mudança aparecer no resumo de uma página, atualize `docs/gdd.md` também.
   Código e GDD divergentes: o código é a verdade e o GDD é o bug.

2. **Regere as páginas visuais.** Depois de mudar um Markdown, atualize o HTML equivalente
   (`docs/gdd.html` para o resumo, `docs/gdd-full.html` para os capítulos) com o mesmo texto,
   para que as duas versões não divirjam.

3. **Mudança ainda em discussão não entra no GDD.** Uma proposta não confirmada pelo dono
   vai como `> **Q:**` no capítulo ou como pergunta em aberto, nunca como fato.

4. **Nada inventado.** O que não está decidido é `_TBD_` e vai para "Perguntas em aberto".
   Números de design são valores de partida, e isso deve ficar dito quando couber.

5. **Ilustrações acompanham a mecânica.** Se a mudança alterar algo que uma ilustração
   mostra (ex.: teclas, HUD, ondas, combos), atualize o SVG correspondente em `docs/img/`.

6. **Texto de jogo em português brasileiro,** como aparece no jogo. Nomes de código em inglês.

7. **Novo capítulo** começa de [`docs/gdd/_template.md`](docs/gdd/_template.md).

8. **Peça confirmação antes de push, merge ou exclusão.** Commits locais podem ser feitos
   quando o usuário pedir.

## Como aplicar a regra 1 numa tarefa

Ao terminar uma mudança de jogo, antes do commit:
- liste os capítulos afetados;
- confira se o que o GDD diz ainda vale;
- se não valer, edite o capítulo e o resumo, regenere os HTML e inclua tudo no mesmo commit.

## Organização do projeto

Tudo do jogo fica em `Assets/_Project/`; `Assets/Settings/` guarda os assets do URP.

| Pasta | O que vai nela |
|---|---|
| `Scripts/Core` | Regras em C# puro, sem MonoBehaviour, cobertas por testes (`OneMoreTime.Core`) |
| `Scripts/Gameplay` | Componentes da cena (`OneMoreTime.Gameplay`), uma subpasta por área: Input, World, Ship, Echo, Combat, Enemies, Gadgets, Traps, Boss, Session |
| `Scripts/UI` | HUD (`OneMoreTime.UI`); layout e estilo em `UI/Hud.uxml` e `UI/Hud.uss` |
| `Scripts/Editor` | Ferramentas de Editor (`OneMoreTime.Editor`) |
| `Tests/EditMode` | Testes do Core |
| `Data` | ScriptableObjects de ajuste: `EchoConfig`, ondas em `Data/Waves` |
| `Prefabs` | Por área: Ship, Echo, Enemies, Projectiles, Gadgets, Traps, Boss, World |
| `Art/Placeholder` | Materiais placeholder; a arte final entra em `Art/` ao lado, sem mexer no gameplay |
| `Art/Ships` | Modelos de blockout: `BI_Ships_BlockOut` (nave e eco), `Enemie_S_BlockOut` (caça), `Enemie_M_BlockOut` (artilheiro), `Enemie_Boss_BlockOut` (Guardião) |
| `Scenes` | `Arena_01.unity`, a única cena no build por enquanto |
| `Input` | `OneMoreTimeControls.inputactions` — o único asset de input do jogo |

Convenções:
- **Nada de lógica nova em MonoBehaviour se ela cabe no Core.** Regra testável vai para `Core` com teste.
- **Referências por `[SerializeField]`, não por busca** (`Find*`) nem singletons. O `Playfield` e o `ProjectilePool` chegam a quem precisa pela cena ou por injeção.
- **Só o `PlayerInputReader` lê o Input System.**
- **Arte troca sem tocar no código:** cada prefab tem um filho `Visual`; substitua o que está dentro dele.
  Na nave, no eco e nos inimigos o `Visual` é o pivô da inclinação (o código reescreve a rotação dele): o modelo fica em
  `Visual/Model`, que guarda a rotação, a escala e o centro (no Guardião também). Os colisores ficam no plano de jogo
  (z = 0), não no modelo; no Guardião, onde cada peça aparece pela câmera.
- **A cena tem grupos fixos na raiz:** `Systems`, `Lighting`, `Playfield` (câmera, nave, ecos), `World` (cenário), `UI`.
- **Physics layers** (8–12): Player, PlayerProjectile, Enemy, EnemyProjectile, Echo. A matriz de colisão só deixa tiro acertar o outro lado e inimigo bater na nave; o eco não colide com nada.
  Armadilhas e tiros do chefe ficam em EnemyProjectile (só ferem a nave). Mina e interruptor não usam colisão: a mina faz consultas de física, o interruptor compara posições.
- **Encontros:** o `StageDirector` encadeia ondas → corredor de armadilhas → Guardião. Para testar um encontro direto, mude `startAt` no inspector (sem salvar a cena).

## Trabalhando pelo Unity CLI

Com o Editor aberto, use `unity command <nome>` (lista: `unity command`) em vez de editar `.unity`, `.prefab` ou `.asset` à mão.

- **Testes:** `unity command run_tests --mode editor`.
- **Conteúdo placeholder:** o menu *One More Time › Bootstrap › Build Placeholder Content* (`unity command menu --path "One More Time/Bootstrap/Build Placeholder Content"`) recria o que faltar — materiais, prefabs, dados, painel de UI e a cena. Não sobrescreve nada que exista; apague um asset para ele ser refeito.
  Os inimigos `Enemy_S` e `Enemy_M` saem do bootstrap já com os modelos. Atenção: apagar `PlayerShip`, `EchoGhost` ou `Guardian`
  refaz a versão de primitivas, sem o modelo (o modelo foi posto neles depois, pelo CLI).
- **Play mode só anda com a janela do Unity em primeiro plano.** Em segundo plano o jogo fica parado no frame 1 e o teclado é ignorado. Rode `unity command editor_focus` depois de `editor_play`.
- **Teclas simultâneas:** `simulate_key` troca o estado inteiro do teclado. Para segurar várias, enfileire um `KeyboardState` com `unity command eval` (`InputSystem.QueueStateEvent`).
  O teclado só chega ao jogo com a Game view em foco (`EditorWindow.GetWindow(GameView).Focus()` por eval); com a tela do Mac bloqueada, os eventos se perdem.
  Cada `eval` leva alguns segundos: para segurar uma tecla por um tempo exato, enfileire os estados a partir de um `EditorApplication.update` registrado no próprio eval.
- **Play mode em segundo plano:** `unity command eval 'UnityEngine.Application.runInBackground = true;'` depois de `editor_play` mantém o jogo rodando sem foco (vale só até sair do Play mode).
  Para o teclado enfileirado chegar ao jogo sem focar o Editor, ponha também `InputSystem.settings.backgroundBehavior = IgnoreFocus`
  e `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView` por eval. O projeto não tem asset de InputSettings, então nada é salvo.
- **Comece de um teclado solto:** uma tecla que já está pressionada quando a cena recarrega não dispara a ação de botão
  (Fire, por exemplo) até ser solta e pressionada de novo. Enfileire um `KeyboardState()` vazio antes de uma sequência.
- **`eval` roda dentro de um método:** sem `using`; escreva os nomes completos (`UnityEngine.Object.FindFirstObjectByType<...>`).
- **Visual pixelado:** o `PixelView` (em `Systems`) muda a escala de render do `PC_RPAsset` durante o Play e devolve ao sair.
  Se o Play travar ou o Editor fechar no meio, confira se o *Render Scale* do asset voltou a 1 e não commite essa mudança.
- **Pular para um encontro no Play mode:** `RunCheckpoint.Save(StageSegment.Boss)` e `SceneManager.LoadScene(0)` por eval, o mesmo caminho de uma morte.
- **Capturas** (`capture_game_view --save_path`) são relativas a `Assets/` e precisam ficar dentro do projeto: apague depois
  (`AssetDatabase.DeleteAsset` por eval), para não versionar.
