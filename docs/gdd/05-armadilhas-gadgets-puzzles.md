---
title: Armadilhas, gadgets e puzzles
chapter: "05"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §6, §7 e §8 — docs/source/GDD_One_More_Time_v0.1.pdf; protótipo em Assets/_Project (TrapSection, LaserGate, MovingBarrier, SwitchPad, ShipGadget, TimeMine)
---

# ONE MORE TIME | Armadilhas, gadgets e puzzles

O cenário que desafia o movimento, as ferramentas que a nave carrega e os puzzles que só se resolvem com o eco.

## Armadilhas e obstáculos

As armadilhas são parte central do jogo: desafiam a movimentação **e** permitem soluções com os ecos.

![As quatro armadilhas e sua interação temporal](../img/traps.svg)

| Armadilha | Desafio | Interação temporal |
|---|---|---|
| **Laser gate** | Barreiras que ligam e desligam em ciclos; atravessar no momento seguro | O eco ativa um interruptor que desliga uma barreira enquanto a nave atravessa outra |
| **Laser rotativo** | Um feixe varre a área e obriga a mudar de altitude | O eco atrai inimigos para a linha de fogo, ou repete uma sequência de ativação de escudos |
| **Campo de minas** | Minas explodem ao contato ou depois de um gatilho | A nave dispara um detonador quando os inimigos passam pela região registrada pelo eco |
| **Barreiras móveis** | Obstáculos que abrem e fecham rotas; exigem precisão | O eco mantém um mecanismo num estado, abrindo uma janela de passagem |

> Para a jam, implemente **apenas dois tipos de armadilha e uma variação de obstáculo**. A variedade
> vem das combinações com o eco, não da quantidade de objetos diferentes.

### No protótipo atual

O protótipo tem o **laser gate** (em duas variações: com ciclo, e preso a um interruptor) e as
**barreiras móveis**, ligados por um **interruptor de presença**. Ficam no corredor do encontro 2
(prefab `TrapSection_Corredor`), que entra pela direita quando as ondas do encontro 1 acabam → [06](06-chefe-e-runs.md).

| Peça | Como funciona | Valores de partida |
|---|---|---|
| **Laser gate com ciclo** | Feixe vertical de ponta a ponta da tela; liga e desliga sozinho e pisca uma linha fina de aviso antes de ligar | 1,5 s ligado, 1,5 s desligado, aviso nos últimos 0,5 s |
| **Laser gate do interruptor** | Sempre ligado; desliga enquanto alguém está sobre o interruptor ligado a ele | — |
| **Barreira móvel** | Dois blocos, um de cima e um de baixo, que se afastam e se fecham no meio da tela | fechada 1,2 s, abre em 0,5 s, aberta 1,6 s, fecha em 0,5 s; vão de 4,5 u |
| **Interruptor de presença** | Fica pressionado enquanto a nave **ou um eco** está sobre a placa; acende ciano para a nave e violeta para o eco, junto com o conduto até o laser | placa de 1,8 × 1,4 u; solta 0,15 s depois que a placa fica vazia |
| **Dano** | Feixe e blocos ferem a nave ao contato; não ferem inimigos nem o eco | 1 de dano, com a invulnerabilidade normal da nave → [03](03-combate.md) |

O corredor, na ordem em que chega: o laser com ciclo e a barreira móvel atravessam a tela com a
rolagem, e a nave precisa passar por eles no momento certo. Depois a rolagem **para** com o
interruptor e o laser preso a ele na tela; o encontro termina quando a nave passa desse laser. Aí as
armadilhas desligam, a tela acelera para tirar o corredor de cena e o chefe entra.

*Por quê:* a gravação é relativa à tela → [02](02-eco-temporal.md). Com a rolagem parada, o eco
volta exatamente ao ponto do interruptor onde foi gravado.

## Gadgets

A nave carrega **um gadget por vez** no protótipo. O objetivo é criar escolhas táticas sem sobrecarregar os controles.

| Gadget | Função principal | Interação temporal | Escopo |
|---|---|---|---|
| **Mina temporal** | Explode por proximidade ou detonação | O eco reproduz a colocação de minas | Inicial |
| **Pulso de interferência** | Desativa por um tempo lasers e sensores | Duas ativações podem sincronizar mecanismos | Inicial |
| **Escudo refletor** | Bloqueia ou reflete certos tiros | Permite construir uma sequência de ricochetes | Expansão |
| **Arpão magnético** | Puxa a nave ou inimigos leves | Pode conduzir inimigos para uma armadilha | Expansão |

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

## Puzzles integrados ao combate

Os puzzles surgem naturalmente durante o voo e duram poucos segundos. São desafios de **leitura,
sincronização e execução**.

### Puzzle 1 — Dois interruptores

**Objetivo:** atravessar um corredor com dois lasers ligados a interruptores em pontos diferentes.

![Puzzle 1 resolvido em quatro passos](../img/puzzle-switches.svg)

1. Gravar uma passagem pelo primeiro interruptor.
2. Reproduzir o eco para repetir essa ativação.
3. Pilotar a nave atual até o segundo interruptor.
4. Atravessar o corredor com as duas barreiras desativadas.

O jogador aprende a **dividir uma tarefa entre duas versões da nave**.

**No protótipo**, o corredor do encontro 2 tem a versão com **um** interruptor: grave a nave parada
sobre o interruptor, invoque o eco e atravesse o laser enquanto o eco segura o interruptor.

### Puzzle 2 — Refletir o ataque do chefe

**Objetivo:** atingir um núcleo protegido usando o ataque do próprio chefe.

1. O chefe dispara um projétil pesado.
2. O jogador grava uma sequência que posiciona um escudo refletor.
3. O eco reproduz a colocação da barreira.
4. O jogador se posiciona para induzir o disparo na direção certa.
5. O projétil ricocheteia e atinge o núcleo.

Depende do escudo refletor — pode ficar fora do protótipo inicial.

### Puzzle 3 — Sincronização de energia

**Objetivo:** ativar dois receptores dentro de uma janela curta.

O eco ativa um receptor durante a reprodução enquanto a nave atual ativa o outro. Se os dois estiverem
ativos ao mesmo tempo, a porta ou a defesa do chefe é desativada.

Feedback visual claro: os receptores acendem, uma conexão de energia aparece e o mecanismo muda de estado.

## Perguntas em aberto

- [x] Quais duas armadilhas e qual variação de obstáculo entram na jam? No protótipo: laser gate (com ciclo, e a variação presa a um interruptor) e barreiras móveis.
- [ ] Gadgets no MVP: §7 põe mina temporal **e** pulso de interferência no escopo inicial; §14 pede **um** gadget no MVP e o segundo como opcional → [10](10-producao.md). O protótipo tem só a mina temporal.
- [ ] "Detonação" da mina temporal: no protótipo ela explode sozinha no fim do pavio de 4 s. Falta decidir se o jogador também detona manualmente, e com qual tecla.
- [ ] Barreiras móveis presas a um interruptor ("o eco mantém um mecanismo num estado"): no protótipo a barreira só segue o ciclo.
- [ ] Puzzle 1 com dois interruptores, como no exemplo acima: o protótipo tem um.
- [ ] As armadilhas devem ferir inimigos (o combo Trap Bait depende disso → [03](03-combate.md))? No protótipo só ferem a nave.
- [ ] Puzzle 2 entra no protótipo? Depende do escudo refletor, que é expansão.
