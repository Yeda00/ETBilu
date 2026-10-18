---
title: Inimigos e ondas
chapter: "04"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §5.5; protótipo em Assets/_Project (Enemy_S, Enemy_W, Enemy_M, Enemy, EnemyGun, ModelVariants, SwoopMotion, SerpentineMotion, HoverMotion, FireCycle; Data/Waves/Encounter01_PrimeiroCiclo.asset)
---

# ONE MORE TIME | Inimigos e ondas

Quem a nave enfrenta, como cada inimigo conversa com o eco, e como as ondas entram na tela.

Os inimigos surgem durante o avanço da fase, entrando pela direita (e às vezes por cima ou por baixo).
Em vez de spawn aleatório, cada encontro segue uma **lista de ondas com tempo e posição definidos**.

*Por quê:* o jogador aprende o padrão e consegue planejar o que gravar no eco.

## Tipos de inimigo

![Os tipos de inimigo e seus movimentos](../img/enemies.svg)

| Inimigo | Comportamento | Papel com o eco |
|---|---|---|
| **Caça** (S, pequeno) | Entra em fila pela direita, em linha reta. Num ponto da tela mergulha para a altura em que a nave está naquele momento, avisa com um brilho e dispara **um** tiro mirado; depois segue e sai pela esquerda | Como mergulha atrás da nave, dá para puxar a fila para a linha de tiro do eco |
| **Interceptador** (W, pequeno) | Cruza a arena numa senoide, alternando a altura dos membros da formação. Atira rajadas de **2 tiros** enquanto atravessa | Fecha corredores retos e obriga a nave a reposicionar a mira; o eco consegue cobrir a faixa oposta |
| **Artilheiro** (M, médio) | Entra, freia e segura uma posição à direita, oscilando de forma ampla. A cada rajada avisa com um brilho e dispara um **leque de 5 tiros** mirados; depois de um tempo acelera e sai pela esquerda | Resistente e móvel: alvo natural de Crossfire (nave e eco no mesmo alvo) enquanto sustenta pressão na tela |
| **Torreta** *(planejada)* | Parada no cenário, atira na direção da nave | Exige que nave e eco ataquem de posições diferentes |

O GDD v0.1 previa um **drone** (linha reta, sem atirar) e um **ondulante** (zigue-zague em grupos). Em
2026-10-18 o time pediu inimigos melhores e depois uma curva mais frenética. O caça substituiu o drone;
o interceptador recuperou o papel ondulante com tiro; e o artilheiro cria pressão persistente, todos com
os modelos de blockout do designer → [08](08-direcao-visual-e-sonora.md).

Cada família visual tem **três variações de modelo**, e cada inimigo sorteia uma ao surgir. Caça e
interceptador compartilham a família pequena. A variação muda só o
visual: caminho, tiro, vida e pontos são os do tipo, e as ondas continuam por script. A hitbox e o ponto de
onde sai o tiro acompanham o modelo sorteado.

*Por quê:* uma fila de caças iguais parece um carimbo; com peças e cabeças diferentes, a onda parece um
esquadrão, sem mudar o que o jogador aprende sobre cada tipo.

Para a jam, a recomendação do v0.1 era **começar com um tipo** e adicionar os outros só se o núcleo já
estivesse divertido; o protótipo tem três.

*Por quê:* os três cobrem pressões complementares: o caça investe contra a linha atual da nave, o
interceptador ocupa alturas em movimento e o artilheiro sustenta projéteis enquanto os outros cruzam a tela.

## Valores de partida no protótipo

| | Caça (S) | Interceptador (W) | Artilheiro (M) | Onde mudar |
|---|---|---|---|---|
| Vida | 2 | 3 | 18 | prefab → `Health` |
| Pontos | 100 | 150 | 300 | prefab → `Enemy` |
| Modelo | sorteado entre 3 variações pequenas | sorteado entre as mesmas 3 variações pequenas | sorteado entre 3 variações médias | prefab → `ModelVariants` |
| Movimento | 7,2 u/s; mergulha a 30% da meia largura à direita do centro, em 0,75 s, no máximo 5 u para cima ou para baixo | 5,8 u/s; senoide com deslocamento vertical máximo de 1,4 u a 0,55 Hz; cada membro recebe uma fase diferente | entra em 1,25 s até 4,5 u da borda direita; segura 6,5 s oscilando 0,8 u (0,45 Hz); sai acelerando a 6 u/s² | prefab → `Enemy` |
| Tiro | 1 tiro mirado, 0,28 s depois do início do mergulho | rajada de 2 tiros, 0,12 s entre eles e 8° de abertura; primeira em 0,55 s, depois a cada 1,35 s | leque de 5 tiros mirados, 10° entre eles; primeira rajada 0,65 s depois de parar, depois a cada 1,45 s | prefab → `EnemyGun` |
| Aviso antes do tiro | 0,28 s | 0,35 s | 0,4 s | prefab → `EnemyGun` |
| Tiro inimigo | 9,5 u/s, 1 de dano (o mesmo do Guardião) | | | `Shot_Enemy` |
| Encostar na nave | 1 de dano | 1 de dano | 1 de dano | `ContactDamage` |

Valores para testar, não definitivos. As regras dos caminhos e do tiro ficam no Core (`SwoopMotion`,
`SerpentineMotion`, `HoverMotion`, `FireCycle`), com testes.

## Regras de surgimento

- As ondas são definidas por script: tempo de entrada, ponto de entrada, tipo e quantidade.
- Cada onda **sinaliza a entrada** com um aviso visual curto (no protótipo, um marcador piscando na borda direita, 0,9 s antes).
- Cada rajada inimiga é **avisada**: um brilho amarelo pisca na ponta do inimigo antes do tiro.
- Inimigo só atira com a ponta **dentro da tela**: nada atira de onde o jogador não vê.
- Inimigos que saem pela esquerda são removidos **sem penalidade**.
- A composição autoral controla a densidade: as sobreposições mais fortes combinam no máximo dois papéis de pressão por vez. Um limite técnico rígido ainda é futuro.
- Matar inimigos **nunca é obrigatório** para avançar; o scroll continua.

Dois artilheiros na mesma onda ficam em alturas diferentes (duas fileiras); se a onda tiver mais de um na
mesma fileira, cada um para mais à direita que o anterior, mantendo a distância com que entrou.

## Exemplo — Encontro 1 (primeiro ciclo)

![Linha do tempo das ondas do encontro 1](../img/wave-timeline.svg)

| Tempo | Onda | Objetivo de design |
|---|---|---|
| 0:00 – 0:15 | Caças no alto (0:03), embaixo (0:07), interceptadores (0:11) e primeiro cerco em dois níveis (0:15) | Entrar em ação rápido e ensinar os três vetores de movimento sem misturá-los de imediato |
| 0:19 – 0:29 | Primeiro artilheiro (0:19), interceptadores por baixo (0:22), caças no alto (0:26) e dupla de artilheiros (0:29) | Começar a sobrepor alvo resistente com ameaça móvel; criar alvos para Crossfire |
| 0:33 – 0:43 | Tesoura de interceptadores (0:33), artilheiro de contenção (0:37), escolta sobreposta (0:38,5) e parede móvel (0:43) | Cobrar leitura de prioridade, mudança de altura e uso do eco para cobrir outra faixa |
| 0:47 – 0:54 | Artilheiros em pinça (0:47), caças em duas linhas (0:49) e clímax de interceptadores em duas linhas (0:54) | Pico de densidade antes do corredor, sem remover os avisos de entrada e de rajada |

Está em `Encounter01_PrimeiroCiclo.asset`: **15 entradas e 66 inimigos em 54 s** (antes eram 9 entradas e
35 inimigos em 73 s). O encontro termina 2,5 s depois da última onda, quando não resta
inimigo vivo; aí começa o corredor de armadilhas → [06](06-chefe-e-runs.md). A torreta ainda não entra.

Os tempos são valores de partida para testar. As ondas dos encontros 2 e 3 devem reaproveitar os mesmos
inimigos, combinados com as armadilhas e o gadget → [06](06-chefe-e-runs.md).

## Perguntas em aberto

- [ ] Limite rígido de inimigos simultâneos: `_TBD_`; hoje a densidade é controlada pela timeline autoral.
- [ ] Torreta: falta implementar.
- [ ] O caça mira e mergulha para onde a nave está: as ondas continuam por script, mas parte do movimento responde ao jogador. Isso combina com "o jogador aprende o padrão e planeja o que gravar", ou o mergulho deve ir para alturas fixas?
- [ ] Vida, pontos, tempos de tiro e de aviso: primeira rodada frenética validada tecnicamente; falta playtest externo.
- [ ] Ondas dos encontros 2 e 3: só o encontro 1 tem linha do tempo.
