---
title: Arquitetura técnica
chapter: "09"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §13; código em Assets/_Project/Scripts
---

# ONE MORE TIME | Arquitetura técnica

A arquitetura sugerida para Unity: quem grava, quem reproduz, e como o eco conversa com os outros sistemas.

A implementação é **modular**: o sistema temporal não depende diretamente da lógica de cada inimigo ou armadilha.

![Diagrama de componentes](../img/architecture.svg)

*Diagrama conceitual do GDD v0.1. O que existe hoje está em "Implementação atual", abaixo.*

## Componentes principais

| Componente | Responsabilidade |
|---|---|
| `PlayerController` | Movimento e entrada do jogador |
| `TemporalRecorder` | Captura as amostras e os eventos |
| `TemporalRecording` | Armazena a sequência gravada |
| `EchoPlayback` | Reproduz a sequência em ordem temporal |
| `WeaponSystem` | Gerencia disparos e projéteis |
| `GadgetSystem` | Ativa e registra gadgets |
| `TrapController` | Controla lasers, interruptores e armadilhas |
| `EnemyController` | Movimento e comportamento dos inimigos |
| `BossController` | Fases, ataques e pontos fracos |
| `ComboEvaluator` | Detecta as condições dos combos temporais |

## Implementação atual

O código fica em `Assets/_Project/Scripts`, em quatro assemblies:

| Assembly | Conteúdo |
|---|---|
| `OneMoreTime.Core` | Regras em C# puro, testáveis sem cena: gravação, reprodução, vida e escudo, linha do tempo das ondas, ciclos das armadilhas, interruptor de presença, recarga e pavio da mina, fases do Guardião, combos temporais, caminhos e tiro dos inimigos |
| `OneMoreTime.Gameplay` | Componentes da cena: input, playfield, nave, tiro, gadget, inimigos, ondas, armadilhas, chefe, encontros, combos e sessão |
| `OneMoreTime.UI` | O HUD |
| `OneMoreTime.Editor` | A ferramenta que monta o conteúdo placeholder (menu *One More Time › Bootstrap*) |

Os testes ficam em `Assets/_Project/Tests/EditMode`.

| Sugerido no GDD | No protótipo | Nota |
|---|---|---|
| `PlayerController` | `PlayerInputReader` + `ShipMotor` + `ShipAim` | Input separado do movimento; `ShipAim` projeta o ponteiro no plano, orienta apenas o visual e desenha o indicador |
| `TemporalRecorder` | `EchoRecorder` (Core) + `EchoAbility` | `EchoAbility` liga input, nave e arma ao gravador |
| `TemporalRecording` | `EchoRecording` (Core) | |
| `EchoPlayback` | `EchoPlayback` (Core) + `EchoGhost` | `EchoGhost` é a nave fantasma na cena |
| `WeaponSystem` | `ShipWeapon` + `Projectile` + `ProjectilePool` | Tiros reaproveitados por pool |
| `DamageSystem` | `Health` (+ `HealthModel` no Core) + `ContactDamage` | `Health` publica dano aceito, bloqueio por escudo e morte; invulnerabilidade repetida fica silenciosa para não gerar spam visual |
| `EnemyController` | `Enemy` + `EnemyGun` + `ModelVariants` + `WaveSpawner` + `WaveSchedule` (+ `SwoopMotion`, `HoverMotion`, `FireCycle` no Core) | Ondas são um asset de dados; o caminho e o tiro de cada inimigo vêm do prefab (`Enemy_S`, `Enemy_M`). O `WaveSpawner` passa a nave e o pool de tiros aos inimigos. `ModelVariants` sorteia o modelo ao surgir e ajusta a hitbox e a boca do tiro a ele |
| `GadgetSystem` | `ShipGadget` + `TimeMine` (+ `Cooldown`, `TimeMineModel` no Core) | A nave solta a mina; o eco reproduz o evento `Gadget` |
| `TrapController` | `TrapSection` + `LaserGate` + `MovingBarrier` + `SwitchPad` (+ `TrapCycle`, `ShutterCycle`, `PresenceSwitch`, `LaserGateModel` no Core) | `TrapSection` para a rolagem no puzzle e encerra o encontro |
| `BossController` | `GuardianBoss` + `SweepLaser` (+ `GuardianModel` no Core) | Fases, escudos, geradores e padrões de tiro |
| `ComboEvaluator` | `ComboTracker` (+ `CrossfireRule`, `SynchronizedStrikeRule` no Core) | Uma regra por combo; só Crossfire e Synchronized Strike existem. O `ComboTracker` ouve o `Health.Damaged` dos inimigos que as ondas criam e das partes do Guardião, e o `GeneratorDestroyed` do Guardião; aplica o bônus e avisa o HUD e a sessão (`Triggered`) |
| — | `Playfield` | O quadro que rola, com a câmera, a nave e os ecos |
| — | `PixelView` | O visual pixelado: enquanto está ligado, muda a escala de render e o filtro (Point) do asset do URP; devolve os dois ao desligar → [08](08-direcao-visual-e-sonora.md) |
| — | `StageDirector` + `RunCheckpoint` | Encadeia os encontros (ondas → corredor → Guardião) e guarda em qual recomeçar depois de uma morte |
| — | `GameSession` | Pontuação (inimigos, Guardião e combos), pausa, vitória (o Guardião cai) e derrota |
| `AudioSystem` | `AudioDirector` + `SfxPlayer` + `MusicPlayer` + `AudioLibrary` (+ `SoundCue`, `CueThrottle`, `RecordPulse`, `VolumeFader` no Core) | O `AudioDirector` traduz eventos de tiro, dano, morte, eco, combo, chefe, armadilha e pausa em cues; tiros e minas chegam por `ProjectilePool.CueRaised`. O `SfxPlayer` toca numa reserva fixa de `AudioSource` (escolhe clipe, tom, pan e abafamento) e o `MusicPlayer` encadeia intro e loop por encontro. Os clipes de cada cue ficam no `AudioLibrary` (asset em `Data/`); o menu *One More Time › Bootstrap › Build Audio* cria o asset e liga o áudio na cena → [08](08-direcao-visual-e-sonora.md) |
| — | `CombatFeedbackDirector` + `CombatVfxPool` + `CameraImpact` | Traduz eventos de dano, escudo, morte e combo em flashes, VFX reutilizados e impulsos proporcionais de câmera; não altera vida, dano ou colisão |

Armadilhas e tiros do chefe usam a layer `EnemyProjectile`, que só colide com a nave. A mina não tem
colisor: o alcance e a explosão são consultas de física (`CheckSphere`, `OverlapSphere`) na layer `Enemy`.
O interruptor também não usa física: compara posições, para o eco, que não tem colisor, poder segurá-lo.

Os combos sabem quem acertou pelo `DamageInfo.Source` (`Ship` ou `Echo`) de cada dano. O bônus do Crossfire é
dano `Combined`, que o próprio `ComboTracker` ignora, para não realimentar a regra. O `Health` decide se o
acerto matou antes de avisar `Damaged`: um ouvinte que cause mais dano nesse aviso (o bônus) não faz o
`Died` disparar duas vezes.

O feedback de combate acompanha os alvos dinâmicos pelo `EnemySpawned`/`EnemyRemoved` do `WaveSpawner` e
as partes do chefe quando o `StageDirector` cria o Guardião. Partículas e anéis pertencem a um pool em
`Systems`, então continuam visíveis mesmo quando o alvo morto é destruído no mesmo frame.

## Como armazenar a gravação

Uma amostra de movimento:

```csharp
public struct EchoSample
{
    public float time;
    public Vector2 position;
    public float rotation;
    public Vector2 aimDirection;
}
```

Disparos e gadgets ficam numa lista separada de eventos, cada um com seu instante relativo à gravação:

```csharp
public struct EchoEvent
{
    public float time;
    public EchoEventType type;
    public Vector2 position;
    public Vector2 direction;
}
```

`EchoEventType` é um enum com os tipos de evento que o protótipo suporta, como disparo e uso de gadget.

| Regra | Detalhe |
|---|---|
| Taxa de amostragem | Fixa, não a cada frame — **20 amostras por segundo** costuma bastar para começar |
| Pose entre amostras | A reprodução interpola posição, inclinação e direção da mira pelo menor arco |
| Eventos | Respeitam o instante original e disparam **uma única vez** |
| Relógio | A reprodução usa o tempo decorrido do próprio eco |
| Projéteis do eco | Nascem no ponto e na direção registrados; as colisões são avaliadas no estado **atual** do mundo |

*Por quê:* gravar dados, e não o mundo, é o que mantém a regra fundamental barata de implementar →
[02](02-eco-temporal.md#regra-fundamental).

## Perguntas em aberto

- [ ] Lista final de valores de `EchoEventType`. O protótipo usa `Shot` e `Gadget`.
- [x] Com o mundo em 3D (2.5D), `EchoSample` e `EchoEvent` continuam com `Vector2`: a nave se move só no plano de jogo, e as posições são relativas ao playfield → [02](02-eco-temporal.md).
