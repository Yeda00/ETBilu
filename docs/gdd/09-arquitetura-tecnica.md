---
title: Arquitetura técnica
chapter: "09"
status: draft
owner: _TBD_
updated: 2026-10-17
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
| `OneMoreTime.Core` | Regras em C# puro, testáveis sem cena: gravação, reprodução, vida, linha do tempo das ondas |
| `OneMoreTime.Gameplay` | Componentes da cena: input, playfield, nave, tiro, inimigos, ondas, sessão |
| `OneMoreTime.UI` | O HUD |
| `OneMoreTime.Editor` | A ferramenta que monta o conteúdo placeholder (menu *One More Time › Bootstrap*) |

Os testes ficam em `Assets/_Project/Tests/EditMode`.

| Sugerido no GDD | No protótipo | Nota |
|---|---|---|
| `PlayerController` | `PlayerInputReader` + `ShipMotor` | Input separado do movimento |
| `TemporalRecorder` | `EchoRecorder` (Core) + `EchoAbility` | `EchoAbility` liga input, nave e arma ao gravador |
| `TemporalRecording` | `EchoRecording` (Core) | |
| `EchoPlayback` | `EchoPlayback` (Core) + `EchoGhost` | `EchoGhost` é a nave fantasma na cena |
| `WeaponSystem` | `ShipWeapon` + `Projectile` + `ProjectilePool` | Tiros reaproveitados por pool |
| `DamageSystem` | `Health` (+ `HealthModel` no Core) + `ContactDamage` | |
| `EnemyController` | `Enemy` + `WaveSpawner` + `WaveSchedule` | Ondas são um asset de dados |
| `GadgetSystem`, `TrapController`, `BossController`, `ComboEvaluator` | — | Ainda não existem |
| — | `Playfield` | O quadro que rola, com a câmera, a nave e os ecos |
| — | `GameSession` | Pontuação, pausa, vitória e derrota |

## Como armazenar a gravação

Uma amostra de movimento:

```csharp
public struct EchoSample
{
    public float time;
    public Vector2 position;
    public float rotation;
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
| Posições entre amostras | A reprodução interpola |
| Eventos | Respeitam o instante original e disparam **uma única vez** |
| Relógio | A reprodução usa o tempo decorrido do próprio eco |
| Projéteis do eco | Nascem no ponto e na direção registrados; as colisões são avaliadas no estado **atual** do mundo |

*Por quê:* gravar dados, e não o mundo, é o que mantém a regra fundamental barata de implementar →
[02](02-eco-temporal.md#regra-fundamental).

## Perguntas em aberto

- [ ] Lista final de valores de `EchoEventType`.
- [x] Com o mundo em 3D (2.5D), `EchoSample` e `EchoEvent` continuam com `Vector2`: a nave se move só no plano de jogo, e as posições são relativas ao playfield → [02](02-eco-temporal.md).
