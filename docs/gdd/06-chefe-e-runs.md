---
title: Chefe e estrutura das runs
chapter: "06"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §9 e §10, reescrito com as peças pedidas pelo designer em 2026-10-18; protótipo em Assets/_Project (GuardianBoss, GuardianModel, StageDirector, GameSession, ComboTracker; modelo Art/Ships/Enemie_Boss_BlockOut.fbx)
---

# BILU'S INVASION | Chefe e estrutura das runs

O chefe do protótipo, fase a fase, e como os encontros de uma run ensinam e combinam as mecânicas.

## O Guardião do Ciclo *(nome provisório)*

O protótipo tem **um único chefe**, desenhado para mostrar o potencial da mecânica temporal: uma máquina
de combate que protege uma passagem temporal e que **se desmonta peça por peça**. Cada peça derrubada muda a luta, e o
jogador escolhe a ordem. Os dentes, o ponto fraco, só ficam à mostra quando as peças que os guardam caem.

![O Guardião do Ciclo, suas peças e o que cada uma muda](../img/boss.svg)

| Fase | Nome | O que acontece | O jogador aprende |
|---|---|---|---|
| 1 | **Sobrevivência** | Tudo blindado. Projéteis em padrões simples e um laser varre a tela | Mover-se, achar janelas seguras e gravar uma rota |
| 2 | **Desmonte** | As peças ficam expostas e saem uma a uma, em qualquer ordem; cada uma muda como o Guardião luta. Os dentes acabam com ele | Escolher o que derrubar primeiro, e usar o eco para atacar duas peças ao mesmo tempo |

> **Critério de sucesso:** o chefe é vencível com uma boa sequência temporal, mas **não exige combinação
> pixel-perfect**. O desafio vem da leitura de padrões e do planejamento.

Em 2026-10-18 o designer trocou o desenho anterior (dois geradores que abriam o núcleo numa janela, quatro fases) por
este: peças destrutíveis que mudam a luta e dois pontos fixos de dano para finalizar. Os geradores, o núcleo, as janelas
e a fase 4 saíram.

### No protótipo atual

O Guardião entra pela direita em 2,5 s e passa a lutar entre três estações na borda direita: alta, central e baixa.
Cada investida tem antecipação visual e sonora, deslocamento rápido e recuperação.

Ele usa o modelo de blockout do designer (`Enemie_Boss_BlockOut`), **visto de lado**, como as naves e os
inimigos: virado para a nave, com o topo inclinado 25° para a câmera e o rosto virado 15° para ela. O modelo traz a cabeça
com o visor, a antena (`metalHair`), os dentes (`Teeth`) e os braços-canhão (`Canon`); as peças de jogo são **colisores com
marcadores** postos onde cada uma aparece pela câmera, no plano de jogo (o modelo fica logo atrás), porque o modelo
ainda não separa os dentes de cima dos de baixo.

| Peça | Vida | Enquanto está de pé | Quando cai |
|---|---|---|---|
| **Antena** (no alto da cabeça) | 14 | **Chama caças** (S) de reforço: 2 a cada 8 s, no máximo 4 vivos | Não chama mais caças, e o Guardião **atira com mais frenesi**: o tempo entre os ataques cai 28% |
| **Olhos** (visor) | 24 | **Chamam artilheiros** (M): 1 a cada 15 s, um vivo por vez | Não chamam mais artilheiros; o Guardião ganha um **ataque novo** (a cada quatro ataques, uma espiral de tiros girando que sai das órbitas dos olhos); os **dentes de cima** ficam expostos |
| **Canhões** (pontas dos dois braços) | 20 cada | Os dois braços-canhão, ligados aos olhos por cabos de energia | Cada canhão derrubado faz o Guardião **se mover 40% mais rápido** e usar **padrões mais densos**. Com os **dois** caídos, os **dentes de baixo** ficam expostos |
| **Dentes de cima** | 45 | Blindados enquanto os olhos estiverem de pé | Ponto fraco |
| **Dentes de baixo** | 45 | Blindados enquanto algum canhão estiver de pé | Ponto fraco |

**Os dois conjuntos de dentes caindo derrota o Guardião**, e essa é a vitória do encontro. A blindagem (corpo e braços)
absorve todos os tiros e fere a nave que encostar nela. Nada volta depois de cair. Os reforços que o Guardião chamou
somem quando ele é derrotado.

| Fase | No protótipo |
|---|---|
| 1 · Sobrevivência | 10 s com tudo blindado. Alterna leque de 7 tiros e parede com corredor seguro a cada 1,1 s. O laser varre a tela com aviso de 0,9 s |
| 2 · Desmonte, sem canhão caído | Alterna rajadas de leque e paredes a cada 0,95 s |
| 2 · com um canhão caído | Alterna anéis com corredor seguro, leques e paredes a cada 1 s; o Guardião se move 40% mais rápido |
| 2 · com os dois canhões caídos | As versões mais densas dos três padrões a cada 0,78 s, o laser volta, e ele se move 80% mais rápido |

A antena e os olhos mexem em **quem ele chama** e em **como atira**; os canhões mexem em **onde e quão depressa ele
vai** e em **quão denso é o ataque**. A espiral dos olhos entra em qualquer das linhas acima.

**Os reforços.** Os caças do Guardião entram como os das ondas, com os modelos S_2, S_3 e S_1 (kamikaze) em rodízio; os
artilheiros entram por M_2 e M_3 e param no meio da tela, longe do corpo do Guardião → [04](04-inimigos-e-ondas.md).
Contam como inimigos comuns: vida, pontos e combos. Mantê-los em cima do jogador enquanto ele mira nas peças é a
pressão da antena e dos olhos; derrubá-las é tirar essa pressão.

O HUD mostra a vida dos dois conjuntos de dentes juntos, os seis marcadores (antena, olhos, canhões e dentes) e o estado:
tudo blindado, os dentes que estão expostos, ou o que derrubar para abri-los → [07](07-controles-e-interface.md).
Pontuação: 150 pela antena, 250 pelos olhos e por cada canhão e 2500 por cada conjunto de dentes.

**Combos no Guardião** → [03](03-combate.md):

| Combo | No Guardião |
|---|---|
| **Synchronized Strike** | Um canhão cai pela nave e o outro por um eco, com até 2,5 s entre as quedas: aviso "SYNCHRONIZED STRIKE" no HUD e 1000 pontos. Os dentes de baixo abririam de qualquer jeito |
| **Crossfire** | A nave e um eco acertando a mesma peça com até 1 s de diferença: +3 de dano combinado |

Todos os números são valores de partida para testar, no prefab `Guardian` (`GuardianBoss`, `PartRef`) e em `GuardianModel`.

*Por quê:* o eco pede o ataque simultâneo. Grave os tiros contra uma peça (a fase 1, com tudo blindado, é um bom
momento), solte quando as peças abrirem e derrube outra com a nave. E cada peça é uma decisão: a antena tira pressão
de caças, os olhos abrem os dentes de cima, os canhões abrem os de baixo, e todos tornam o Guardião mais perigoso
em troca.

## Estrutura das runs

Runs curtas, com encontros que introduzem e depois combinam as mecânicas.

| Encontro | Conteúdo | Objetivo de design |
|---|---|---|
| 1. **Primeiro ciclo** | Inimigos simples → [04](04-inimigos-e-ondas.md) | Ensinar movimento, tiro e gravação |
| 2. **Combinação** | Inimigos e gadget | Ensinar combos temporais |
| 3. **Guardião** | O chefe | Testar o domínio da mecânica |

No protótipo, os encontros podem ser segmentos de **uma única fase**, sem mapa de progressão.

**No protótipo atual**, a cena `Arena_01` encadeia dois encontros (`StageDirector`):

| Ordem | Encontro | Começa quando | Termina quando |
|---|---|---|---|
| 1 | **Primeiro ciclo** — ondas → [04](04-inimigos-e-ondas.md) | A cena carrega | 2,5 s depois da última onda, sem inimigo vivo na tela |
| 2 | **Guardião** | 2 s depois do fim das ondas (`bossDelaySeconds`, valor de partida); a rolagem para | Os dois conjuntos de dentes são destruídos: vitória |

O encontro de **Combinação** (inimigos e gadget) ainda não existe. O `StageDirector` tem um campo `startAt` para começar
direto no Guardião ao testar.

## Progressão entre tentativas

A repetição deve reforçar o tema *One More Time*. Dois modelos estão em jogo:

![Os dois modelos de progressão entre tentativas](../img/progression.svg)

| Modelo | Como funciona | Situação |
|---|---|---|
| **A — Eco durante a vida** | O jogador grava e reproduz enquanto está vivo. A morte reinicia o encontro | **Recomendado para o protótipo** |
| **B — Ciclos acumulados** | Ao reiniciar, o jogador deixa um eco persistente da tentativa anterior | Evolução futura |

**No protótipo atual** o modelo A está implementado: ao morrer, a cena recarrega depois de 2 s **no encontro em que a morte aconteceu** (ondas ou Guardião), sem ecos e com a pontuação zerada (`RunCheckpoint`). Derrotar o Guardião é a vitória.

*Por quê:* o modelo A é mais previsível e mais fácil de balancear. O modelo B pode entrar depois, se
"tentar mais uma vez" também virar a estrutura narrativa do jogo.

## Perguntas em aberto

- [ ] Os dois conjuntos de dentes precisam cair para vencer (como está), ou um só basta? O designer falou em "pontos fixos de dano para finalização", os de cima e os de baixo.
- [ ] Nome definitivo do chefe — "O Guardião do Ciclo" é provisório.
- [ ] Confirmar o modelo A como sistema do protótipo (é o que está implementado).
- [ ] Peças no modelo: o `Enemie_Boss_BlockOut` tem a antena, o visor e os dentes (de cima e de baixo juntos) numa mesma malha. Os colisores e marcadores das peças são placeholder até a arte separar cada uma → [A01](../specs/art/A01-modelo-guardiao.md).
- [ ] Vida das peças, intervalo dos reforços (8 s e 15 s), tetos de reforços vivos, 28% de frenesi e 40% de aceleração por canhão: valores de partida, sem teste de jogo.
- [ ] A espiral dos olhos é a "nova mecânica de ataque" do designer, escolhida por nós (uma espiral de tiros). Ele aprova, ou prefere outra?
- [ ] Os canhões "mudam o padrão de ataque": hoje cada canhão sobe um nível de densidade (leques e anéis, depois as versões densas e o laser). É isso?
- [ ] O Synchronized Strike hoje só avisa e premia → [03](03-combate.md).
- [ ] O Guardião entrando fere a nave que estiver no caminho (dano de contato da blindagem). É justo, ou a blindagem só fere depois que ele para?
- [x] Movimento do Guardião: deriva curta vertical e horizontal implementada, mantendo os pontos fracos dentro de uma faixa legível.
- [x] Chefe com quatro fases ou duas? Ficam duas: Sobrevivência e Desmonte, com a luta mudando pelas peças que caem.
- [ ] O designer também sugeriu um chefe grande o bastante para não caber na tela, com a peça grande dividida em partes. Hoje ele cabe na tela, e as partes são as seis peças da tabela.
