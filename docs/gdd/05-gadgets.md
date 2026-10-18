---
title: Gadgets
chapter: "05"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §6, §7 e §8 — docs/source/GDD_One_More_Time_v0.1.pdf; protótipo em Assets/_Project (ShipGadget, TimeMine)
---

# BILU'S INVASION | Gadgets

As ferramentas que a nave carrega além do tiro principal.

## O que saiu do escopo

O jogo é **só combate**: ondas de inimigos e o chefe. O PDF original previa armadilhas (laser gate, laser rotativo,
campo de minas, barreiras móveis) e puzzles resolvidos com o eco (dois interruptores, refletir o ataque do chefe,
sincronização de energia). Em 2026-10-18 o dono decidiu **tirar tudo isso**. O protótipo chegou a ter um corredor
de armadilhas com laser gate, barreira móvel e um interruptor de presença; o código, os prefabs e os testes foram
removidos. Ficou o laser que varre a tela no ataque do **Guardião** → [06](06-chefe-e-runs.md), que é um ataque do
chefe, não uma armadilha do cenário.

## Gadgets

A nave carrega **um gadget por vez** no protótipo. O objetivo é criar escolhas táticas sem sobrecarregar os controles.

| Gadget | Função principal | Interação temporal | Escopo |
|---|---|---|---|
| **Mina temporal** | Explode por proximidade ou detonação | O eco reproduz a colocação de minas | Inicial |
| **Pulso de interferência** | _TBD_: no PDF desativava lasers e sensores, que saíram do jogo | _TBD_ | A redefinir |
| **Escudo refletor** | Bloqueia ou reflete certos tiros | Permite construir uma sequência de ricochetes | Expansão |
| **Arpão magnético** | Puxa a nave ou inimigos leves | _TBD_: no PDF levava inimigos a uma armadilha | Expansão |

Escudo refletor e arpão ficam para expansões, caso o núcleo já esteja divertido.

### No protótipo atual

O gadget do protótipo é a **mina temporal**, na tecla **Q** (`ShipGadget` na nave, prefab `TimeMine_Ship`).

| Regra | Valor de partida |
|---|---|
| Onde cai | No ponto da nave, e fica parada no mundo: não acompanha a tela que rola |
| Armar | 0,4 s depois de solta; antes disso não explode |
| Explosão | Quando um inimigo chega a 1,4 u da mina armada, ou sozinha depois de 4 s |
| Dano | 4, do tipo gadget, em todo inimigo num raio de 2,4 u → [03](03-combate.md) |
| Recarga | 1,5 s entre uma mina e outra |
| Com o eco | O uso entra na gravação (`EchoEventType.Gadget`) e o eco solta uma mina (`TimeMine_Echo`, violeta) no mesmo ponto e no mesmo instante da gravação |

O pulso de interferência ainda não existe.

## Perguntas em aberto

- [ ] Gadgets no MVP: §7 põe mina temporal **e** pulso de interferência no escopo inicial; §14 pede **um** gadget no MVP e o segundo como opcional → [10](10-producao.md). O protótipo tem só a mina temporal. Sem armadilhas, o pulso de interferência precisa de uma nova função.
- [ ] "Detonação" da mina temporal: no protótipo ela explode sozinha no fim do pavio de 4 s. Falta decidir se o jogador também detona manualmente, e com qual tecla.
- [ ] Escudo refletor e arpão magnético (expansão): o que fazem agora que não há armadilhas?
