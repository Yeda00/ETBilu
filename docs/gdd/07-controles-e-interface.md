---
title: Controles e interface
chapter: "07"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §11 — docs/source/GDD_One_More_Time_v0.1.pdf
---

# ONE MORE TIME | Controles e interface

Como o jogador pilota, grava e invoca ecos sem parar de desviar — e o que o HUD precisa mostrar.

## Controles propostos

![Mapa de teclas proposto](../img/controls.svg)

| Ação | Teclado |
|---|---|
| Movimentar nave | WASD ou setas |
| Disparo principal | Espaço ou botão esquerdo do mouse |
| Gravar sequência | Segurar Shift |
| Invocar eco | E |
| Usar gadget | Q |
| Pausar | Esc |

Os atalhos são provisórios.

*Por quê:* o que importa é **separar a gravação da reprodução**, para o jogador não invocar o eco sem
querer enquanto tenta desviar de uma armadilha.

O projeto já tem um `Assets/InputSystem_Actions.inputactions` (o padrão do Unity); estas ações ainda não estão nele.

## HUD

![Mockup conceitual do HUD](../img/hud.svg)

*Mockup conceitual — não representa uma interface implementada.*

O HUD mostra com clareza:

| Elemento | Por que o jogador precisa |
|---|---|
| Vida restante (integridade) | Saber quanto risco ainda pode correr |
| Estado e duração da gravação (memória temporal) | Planejar a sequência sem olhar o relógio |
| Quantidade de ecos ativos | Saber o que já está trabalhando por ele |
| Gadget equipado e recarga | Saber quando pode usar de novo |
| Vida do chefe e indicação de vulnerabilidade | Saber quando atacar |
| Feedback de combo temporal ativado | Entender o que funcionou → [03](03-combate.md) |

## Perguntas em aberto

- [ ] Suporte a controle: "pode vir depois" — entra na jam?
- [ ] Mapeamento definitivo das teclas no Input System.
