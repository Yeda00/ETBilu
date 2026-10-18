---
title: Visão geral
chapter: "01"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §1, §2 e §17 — docs/source/GDD_One_More_Time_v0.1.pdf
---

# BILU'S INVASION | Visão geral

O que o jogo é, qual é a fantasia do jogador, os pilares que orientam cada decisão e o formato do protótipo.

![Nave atual e eco temporal lutando juntos](../img/echo-concept.svg)

## High concept

Um shooter de nave em que o jogador **grava e reproduz suas próprias ações passadas** para lutar
ao mesmo tempo com versões anteriores de si mesmo, desviar dos ataques e explorar combinações
temporais para derrotar chefes.

O tema central é *One More Time*: cada tentativa deixa para trás ações que podem ser reproduzidas
para criar novas estratégias.

O jogo se chama **Bilu's Invasion**. *One More Time* foi o nome inicial e ficou como o tema do design; o código mantém o nome
`OneMoreTime` nos namespaces e nos menus do Editor.

| | |
|---|---|
| Pilar de design | Cada tentativa deve ensinar algo e criar uma nova possibilidade |
| Fantasia do jogador | Ser um piloto preso num ciclo temporal que transforma cada movimento, esquiva e disparo em parte de uma estratégia maior |
| Premissa de câmera | Side-scrolling **2.5D**: mundo 3D visto por uma câmera lateral que lê como 2D; a nave avança na horizontal — referência: as fases de avião de *Cuphead* |

*Por quê:* o jogador não fica mais forte por ter mais vida ou dano. Ele fica mais forte porque
aprende a coordenar suas ações com as versões anteriores da nave.

## Pilares de experiência

| # | Pilar | O que significa |
|---|---|---|
| 1 | **O passado continua lutando** | As ações gravadas viram ecos que repetem trajetórias, tiros e gadgets |
| 2 | **Combate baseado em habilidade** | O jogador precisa dominar o movimento, ler padrões de ataque e criar boas trajetórias |
| 3 | **Soluções inteligentes** | Inimigos e chefes são vencidos por sincronização e posicionamento, não só por dano bruto |

## Formato e escopo

| Item | Definição inicial |
|---|---|
| Gênero | Shooter side-scrolling 2.5D com padrões de bullet hell |
| Câmera | Lateral, com avanço horizontal; leitura 2D sobre um mundo 3D |
| Plataforma-alvo | PC |
| Motor | Unity 6 (6000.3.25f1), URP 17.3 e Input System 1.20 |
| Visual | Mundo e jogabilidade em 3D; parte dos elementos serão sprites 2D → [08](08-direcao-visual-e-sonora.md) |
| Modo | Single-player |
| Estrutura | Runs curtas com encontros de combate e chefe → [06](06-chefe-e-runs.md) |
| Duração-alvo da run | 5–10 minutos no protótipo completo |
| Duração de uma gravação | 5 segundos → [02](02-eco-temporal.md) |
| Ecos simultâneos | Até 3 no protótipo |
| Conteúdo inicial | Uma arena, ondas de inimigos e um chefe (sem armadilhas nem puzzles) |
| Controles | Teclado e mouse, ou controle (gamepad); as teclas e botões podem ser trocados → [07](07-controles-e-interface.md) |
| Contexto | Protótipo para game jam |

Os números são parâmetros iniciais de design para testar, não valores definitivos.

## Resumo executivo

O diferencial é transformar o **próprio histórico do jogador em ferramenta de gameplay**. Uma esquiva
pode virar distração, um disparo pode preparar um ataque posterior e um gadget pode abrir uma rota
que seria impossível atravessar sozinho.

O protótipo se concentra em três experiências:

| Experiência | O jogador… |
|---|---|
| **Repetir** | observa o eco executando exatamente o que foi gravado |
| **Combinar** | usa a nave atual e o eco para atacar e desviar |
| **Descobrir** | percebe uma solução temporal para um desafio que parecia impossível |

> **Recomendação de produção:** começar com uma única arena e um chefe que exija dois ataques
> sincronizados. Se essa experiência funcionar, e o jogador sentir que planejou uma solução
> inteligente, o coração do jogo está validado. O resto — mais gadgets, chefes, upgrades e estrutura
> roguelite — cresce a partir desse núcleo sem pôr em risco a entrega da jam.

## Perguntas em aberto

- [ ] Quem é dono de cada capítulo deste GDD? Hoje todos estão como `_TBD_`.
- [ ] Público-alvo: não está escrito no GDD v0.1.
- [ ] O GDD v0.1 descrevia o jogo como 2D; a correção para 2.5D vem do dono do projeto, e outros trechos do v0.1 ainda podem assumir 2D → [09](09-arquitetura-tecnica.md).
