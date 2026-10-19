# Contexto para agentes de IA neste repositório

Antes de trabalhar, leia [`docs/specs/README.md`](docs/specs/README.md).

BILU'S INVASION é um protótipo de game jam em Unity 6 (6000.3.25f1), URP, Input System. O nome do jogo era *One More Time*, que ficou como o tema do design; o código mantém o nome `OneMoreTime` nos namespaces e nos menus do Editor.
Antes de qualquer mudança, leia [`docs/gdd.md`](docs/gdd.md) — o GDD de uma página — e,
se a tarefa tocar uma área específica, o capítulo correspondente em [`docs/gdd/`](docs/gdd/).

## Regras

1. **O GDD descreve o jogo como ele é.** Quando uma mudança no código ou nos assets fizer o
   jogo diferir do que o GDD diz (regra, número, controle, inimigo, combo, chefe,
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

6. **O jogo é em inglês por padrão e tem português brasileiro** (Unity Localization). Todo texto de jogo vai para a tabela `Game` (`Assets/_Project/Localization`), com a chave nos dois idiomas, nunca como literal no código. A documentação e os nomes de código seguem como estão: docs em português, código em inglês.

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
| `Scripts/Gameplay` | Componentes da cena (`OneMoreTime.Gameplay`), uma subpasta por área: Input, World, Ship, Echo, Combat, Enemies, Gadgets, Boss, Session, Feedback, Audio |
| `Scripts/UI` | HUD, menus e cartão de missão (`OneMoreTime.UI`); layout e estilo em `UI/Hud.*`, `UI/Menu.*` e `UI/MissionCard.*` |
| `Scripts/Editor` | Ferramentas de Editor (`OneMoreTime.Editor`) |
| `Tests/EditMode` | Testes do Core |
| `Data` | ScriptableObjects de ajuste: `EchoConfig`, `AudioLibrary` (qual clipe toca em cada cue e qual música em cada tela e encontro), `Missions/Mission_01` (cartão, mapa e história da missão), ondas em `Data/Waves` |
| `Localization` | Unity Localization: `Localization Settings`, os `Locales` (inglês e português do Brasil) e a tabela de strings `Game` (`Tables/`), com todo o texto do jogo. A tabela é a fonte; o código lê por `L10n.Get("chave")` |
| `Audio` | Clipes CC0 do OpenGameArt: `SFX/`, `UI/`, `Music/`. Fontes e licenças em `Audio/CREDITS.md`; ao trocar ou somar um pack, atualize o arquivo |
| `Prefabs` | Por área: Ship, Echo, Enemies, Projectiles, Gadgets, Boss, World |
| `Art/Placeholder` | Materiais placeholder; a arte final entra em `Art/` ao lado, sem mexer no gameplay |
| `Art/Ships` | Modelos: `Bilu_Ship` (nave e eco, texturizado; mapas em `Textures/`, materiais em `Materials/`; reexporte com o mesmo nome e os mesmos nomes de objetos e materiais), `Enemie_S_1`/`Enemie_S`/`Enemie_S_3_BlockOut` (caça), `Enemie_M_1`/`_2`/`_3_BlockOut` (artilheiro), `Enemie_Boss_BlockOut` (Guardião). `Bilu_BlockOut`, `BI_Ships_BlockOut` e `Enemie_M_BlockOut` (igual ao `M_2`) estão sem uso |
| `Scenes` | `Menu.unity` (título, menu, mapa de missões, história) é a cena 0 do build; `Arena_01.unity`, a cena 1 |
| `Input` | `OneMoreTimeControls.inputactions` — o único asset de input do jogo |

Convenções:
- **Nada de lógica nova em MonoBehaviour se ela cabe no Core.** Regra testável vai para `Core` com teste.
- **Referências por `[SerializeField]`, não por busca** (`Find*`) nem singletons. O `Playfield` e o `ProjectilePool` chegam a quem precisa pela cena ou por injeção.
- **Só o `PlayerInputReader` (jogo) e o `MenuInputReader` (menus) leem o Input System.** Os dois usam o mesmo asset, em mapas diferentes (`Gameplay` e `Menu`). O jogador pode trocar teclas e botões pela tela de controles (`ControlsStore`, `ControlsCatalog`): leia sempre a ação pelo asset, nunca uma tecla fixa. Gamepad e teclado têm bindings separados no asset (grupos `Gamepad` e `Keyboard&Mouse`).
- **Arte troca sem tocar no código:** cada prefab tem um filho `Visual`; substitua o que está dentro dele.
  Na nave, no eco e nos inimigos o `Visual` é o pivô da inclinação (o código reescreve a rotação dele): o modelo fica em
  `Visual/Model`, que guarda a rotação, a escala e o centro (no Guardião também). Os colisores ficam no plano de jogo
  (z = 0), não no modelo; no Guardião, onde cada peça aparece pela câmera.
  Os inimigos têm um pivô por variação (`Visual/Model_1` a `Model_3`); o `ModelVariants` liga um ao surgir e ajusta a
  hitbox e a boca do tiro ao modelo ligado. O modelo também decide o comportamento: o `Enemy` guarda um `EnemyProfile` por
  modelo (caminho, vida, tiro, explosão, esquiva) e as ondas escolhem o modelo (`variant` no `WaveSchedule`).
- **A cena tem grupos fixos na raiz:** `Systems`, `Lighting`, `Playfield` (câmera, nave, ecos), `World` (cenário), `UI`.
- **Physics layers** (8–12): Player, PlayerProjectile, Enemy, EnemyProjectile, Echo. A matriz de colisão só deixa tiro acertar o outro lado e inimigo bater na nave; o eco não colide com nada.
  Os tiros e o laser do chefe ficam em EnemyProjectile (só ferem a nave). A mina não usa colisão: ela faz consultas de física.
- **Encontros:** o `StageDirector` encadeia ondas → Guardião (o jogo é só combate: sem armadilhas nem puzzles). Para testar um encontro direto, mude `startAt` no inspector (sem salvar a cena).

## Trabalhando pelo Unity CLI

Com o Editor aberto, use `unity command <nome>` (lista: `unity command`) em vez de editar `.unity`, `.prefab` ou `.asset` à mão.

- **Testes:** `unity command run_tests --mode editor`.
- **Áudio:** o menu *One More Time › Bootstrap › Build Audio* (`unity command menu --path "One More Time/Bootstrap/Build Audio"`) cria o `AudioLibrary`, ajusta a importação da música (Streaming) e liga o grupo `Systems/Audio` na cena. Não sobrescreve nada que exista: para refazer o mapeamento, apague `Data/AudioLibrary.asset`. Som novo = um `SoundCue` no Core, um pedido no `AudioDirector` (ou `ProjectilePool.Raise`) e o clipe no asset.
- **Textos e idiomas:** o menu *One More Time › Bootstrap › Build Localization* (`unity command menu --path "One More Time/Bootstrap/Build Localization"`) cria os locales, a tabela `Game` e as chaves que faltam, a partir de `Scripts/Editor/LocalizationStrings.cs`, e põe as chaves no `Mission_01`. Não sobrescreve valores que já estão na tabela: para mudar um texto, edite a tabela (janela *Localization Tables*) e, para uma chave nova, some-a também ao `LocalizationStrings.cs`. O inglês é o padrão (`SpecificLocaleSelector`); o idioma escolhido fica em `PlayerPrefs`. `MenuTexts` não é mais lido por nada.
- **Menu e missões:** o menu *One More Time › Bootstrap › Build Menu* (`unity command menu --path "One More Time/Bootstrap/Build Menu"`) cria `Mission_01`, a cena `Menu`, põe o cartão de missão na arena e deixa o build em Menu → Arena. Rode *Build Audio* antes. Não sobrescreve: para refazer a cena, apague `Scenes/Menu.unity`. Para testar o menu em Play, abra a cena `Menu` (`unity command open_scene --path ...`); o editor precisa estar em primeiro plano para o tempo e as teclas andarem.
- **Inimigos e chefe:** o menu *One More Time › Bootstrap › Build Enemy Variants* (`unity command menu --path "One More Time/Bootstrap/Build Enemy Variants"`) grava os perfis de `Enemy_S`/`Enemy_M` (um por modelo), cria os prefabs de tiro `Shot_Enemy_*` e põe o modelo de cada onda do encontro 1. Só escreve onde falta: para refazer os perfis, esvazie `profiles` no prefab. As peças do `Guardian` (antena, olhos, canhões, dentes) são filhos com `Health`, ligados em `GuardianBoss` → `parts`; o bootstrap de primitivas monta a mesma estrutura.
- **Conteúdo placeholder:** o menu *One More Time › Bootstrap › Build Placeholder Content* (`unity command menu --path "One More Time/Bootstrap/Build Placeholder Content"`) recria o que faltar — materiais, prefabs, dados, painel de UI e a cena. Não sobrescreve nada que exista; apague um asset para ele ser refeito.
  Os inimigos `Enemy_S` e `Enemy_M` saem do bootstrap já com as três variações de modelo (prefabs antigos, de um modelo só,
  ganham as variações no lugar, sem perder as referências das ondas). Atenção: apagar `PlayerShip`, `EchoGhost` ou `Guardian`
  refaz a versão de primitivas, sem o modelo (o modelo foi posto neles depois, pelo CLI).
- **Prefab aberto no Prefab Mode:** o Editor pode salvar a versão aberta por cima de uma edição feita pelo CLI (o `Guardian`
  voltou à versão antiga depois de um `run_tests`). Feche o prefab antes de editá-lo pelo CLI e confira o `git status` depois.
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
- **Pular para um encontro no Play mode:** `RunCheckpoint.Save(StageSegment.Boss)` e `SceneManager.LoadScene("Arena_01")` por eval, o mesmo caminho de uma morte (a cena 0 agora é o menu).
- **Capturas** (`capture_game_view --save_path`) são relativas a `Assets/` e precisam ficar dentro do projeto: apague depois
  (`AssetDatabase.DeleteAsset` por eval), para não versionar.
