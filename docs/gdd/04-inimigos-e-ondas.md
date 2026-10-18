---
title: Inimigos e ondas
chapter: "04"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §5.5; protótipo em Assets/_Project (Enemy_S, Enemy_W, Enemy_M, Enemy, EnemyProfile, EnemyGun, ModelVariants, SwoopMotion, SerpentineMotion, HoverMotion, ChaseMotion, BlastFuse, Interception, FireCycle; Data/Waves/Encounter01_PrimeiroCiclo.asset)
---

# BILU'S INVASION | Inimigos e ondas

Quem a nave enfrenta, como cada inimigo conversa com o eco, e como as ondas entram na tela.

Os inimigos surgem durante o avanço da fase, entrando pela direita (e às vezes por cima ou por baixo).
Em vez de spawn aleatório, cada encontro segue uma **lista de ondas com tempo e posição definidos**.

*Por quê:* o jogador aprende o padrão e consegue planejar o que gravar no eco.

## Tipos de inimigo

![Os tipos de inimigo e seus movimentos](../img/enemies.svg)

| Inimigo | Comportamento | Papel com o eco |
|---|---|---|
| **Caça** (S, pequeno) | Entra em fila pela direita, em linha reta. Num ponto da tela mergulha para a altura em que a nave está naquele momento, avisa com um brilho e atira; depois segue e sai pela esquerda. **Cada um dos três modelos é um comportamento diferente** (abaixo) | Como mergulha atrás da nave, dá para puxar a fila para a linha de tiro do eco |
| **Interceptador** (W, pequeno) | Cruza a arena numa senoide, alternando a altura dos membros da formação. Atira rajadas de **2 tiros** enquanto atravessa. Usa só os modelos 2 e 3 do caça, nunca o do kamikaze | Fecha corredores retos e obriga a nave a reposicionar a mira; o eco consegue cobrir a faixa oposta |
| **Artilheiro** (M, médio) | Entra, freia e segura uma posição à direita; depois de um tempo acelera e sai pela esquerda. **Cada um dos três modelos é um comportamento diferente** (abaixo) | Resistente e móvel: alvo natural de Crossfire (nave e eco no mesmo alvo) enquanto sustenta pressão na tela |
| **Torreta** *(planejada)* | Parada no cenário, atira na direção da nave | Exige que nave e eco ataquem de posições diferentes |

O GDD v0.1 previa um **drone** (linha reta, sem atirar) e um **ondulante** (zigue-zague em grupos). Em
2026-10-18 o time pediu inimigos melhores e depois uma curva mais frenética. O caça substituiu o drone;
o interceptador recuperou o papel ondulante com tiro; e o artilheiro cria pressão persistente, todos com
os modelos de blockout do designer → [08](08-direcao-visual-e-sonora.md).

## Um comportamento por modelo

Cada família visual tem **três modelos**, e **o modelo decide o que o inimigo faz**: o jogador reconhece a
variação pela forma e aprende o que esperar dela. Em 2026-10-18 o designer pediu isso para aumentar a dinâmica dos
inimigos e fazer o jogador reconhecer a variação de mecânica. As ondas escolhem o modelo de cada onda (a coluna
`variant` do `WaveSchedule`; 0 sorteia um por inimigo), e a hitbox e o ponto de onde sai o tiro acompanham o modelo.
Cada modelo guarda seu perfil em `Enemy` → `profiles` (`EnemyProfile`), na mesma ordem de `ModelVariants`.

| Modelo | Nome | O que faz | Tiro |
|---|---|---|---|
| **S_1** | **Kamikaze** | Vai atrás da nave a 6,4 u/s, virando no máximo 85°/s, então quem se mexe o faz errar. **Explode** a 1 u da nave ou sozinho 5,5 s depois de entrar; antes disso o brilho pisca. 2 de vida | Não atira |
| **S_2** | **Veloz** | O mais rápido: mergulha a 9,8 u/s. 2 de vida | 1 tiro **pequeno e rápido** (12,5 u/s) a cada 2,6 s |
| **S_3** | **Rajada curta** | Um pouco mais lento (6,8 u/s). 3 de vida | **Rajada de 3 tiros**, 0,26 s entre eles, a cada 1,7 s, mais cadenciada que a do S_2 |
| **M_1** | **Tanque** | Entra e **fica parado como um obstáculo** por 11 s, quase sem oscilar. 30 de vida. Quando a nave chega a 3,4 u, o brilho acende por 0,9 s e ele **explode**: 2 de dano num raio de 3,8 u | Não atira |
| **M_2** | **Rajada** | Menos veloz que o S_3 e mais que o M_3: oscila 1,4 u a 0,5 Hz. 18 de vida | **Rajadas de 4 tiros pesados**, 0,13 s entre eles, a cada 1,7 s |
| **M_3** | **Escopeta** | O mais lento: oscila 0,7 u a 0,3 Hz. 18 de vida. **Esquiva**: se um tiro da nave ou do eco vai acertá-lo, desvia 2,4 u girando (recarga de 2,2 s) | **Escopeta**: 7 tiros em leque (7° entre eles) de uma vez, a cada 2,3 s |

A ordem de velocidade é S_2 > S_3 > M_2 > M_3. Os tiros ganham tamanho e cor próprios: amarelo e pequeno (S_2), laranja
e lento (S_3), vermelho e grande (M_2), magenta e curto (M_3). Todos tiram 1 de vida; a diferença está na cadência,
no padrão e na leitura. Uma explosão que pega a nave tira 1 (kamikaze) ou 2 (tanque), e **a esquiva atravessa**; quem
explode sozinho não conta como abate nem dá pontos. Matar o tanque de longe é a forma segura de passar.

*Por quê:* uma fila de caças iguais parece um carimbo; com comportamentos diferentes, a onda vira um esquadrão a ser
lido, e cada família ensina uma pergunta ao jogador: fugir (kamikaze), atravessar (tanque), desviar (rajada e escopeta).
O que o jogador aprende sobre cada modelo vale em toda a fase, e no chefe → [06](06-chefe-e-runs.md).

Para a jam, a recomendação do v0.1 era **começar com um tipo** e adicionar os outros só se o núcleo já
estivesse divertido; o protótipo tem três famílias e seis comportamentos.

*Por quê:* as famílias cobrem pressões complementares: o caça investe contra a linha atual da nave, o
interceptador ocupa alturas em movimento e o artilheiro sustenta projéteis enquanto os outros cruzam a tela.

## Valores de partida no protótipo

| | S_1 Kamikaze | S_2 Veloz | S_3 Rajada curta | Interceptador (W) | M_1 Tanque | M_2 Rajada | M_3 Escopeta |
|---|---|---|---|---|---|---|---|
| Vida | 2 | 2 | 3 | 3 | 30 | 18 | 18 |
| Pontos | 120 | 100 | 150 | 150 | 250 | 300 | 350 |
| Movimento | persegue a 6,4 u/s, vira 85°/s | mergulha a 9,8 u/s em 0,6 s | mergulha a 6,8 u/s em 0,9 s | senoide de 1,4 u a 0,55 Hz, 5,8 u/s | entra em 1,4 s a 5,5 u da borda; fica 11 s | entra em 1,25 s; fica 6,5 s; oscila 1,4 u a 0,5 Hz | entra em 1,6 s; fica 7,5 s; oscila 0,7 u a 0,3 Hz |
| Tiro | — | 1 tiro, 2,6 s | 3 tiros a cada 0,26 s, 1,7 s | 2 tiros, 8° | — | 4 tiros a cada 0,13 s, 1,7 s | 7 tiros em leque de 7°, 2,3 s |
| Aviso antes do tiro | — | 0,25 s | 0,4 s | 0,35 s | 0,9 s (a explosão) | 0,45 s | 0,55 s |
| Explosão | 1 de dano, raio 1,7 u | — | — | — | 2 de dano, raio 3,8 u | — | — |
| Onde mudar | `Enemy_S` → `Enemy` → `profiles[0]` | `profiles[1]` | `profiles[2]` | `Enemy_W` → `Enemy` | `Enemy_M` → `profiles[0]` | `profiles[1]` | `profiles[2]` |

Valores para testar, não definitivos. O menu *One More Time › Bootstrap › Build Enemy Variants* escreve esses perfis, os
quatro prefabs de tiro (`Shot_Enemy_Fast`, `_Slow`, `_Heavy`, `_Pellet`) e o modelo de cada onda do encontro 1; não
sobrescreve o que já existe. As regras dos caminhos, do tiro, da explosão e da esquiva ficam no Core (`SwoopMotion`,
`SerpentineMotion`, `HoverMotion`, `ChaseMotion`, `FireCycle`, `BlastFuse`, `Interception`), com testes. O tiro dos
demais (interceptador, Guardião) é o `Shot_Enemy`: 9,5 u/s, 1 de dano. Encostar numa nave que não explode tira 1 de dano (`ContactDamage`).

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
| 0:00 – 0:15 | Caças rápidos (S_2) no alto (0:03) e embaixo (0:07), interceptadores (0:11) e primeiro cerco de caças de rajada (S_3) em dois níveis (0:15) | Entrar em ação rápido e ensinar os três vetores de movimento sem misturá-los de imediato |
| 0:19 – 0:29 | Primeiro artilheiro de rajada (M_2, 0:19), interceptadores por baixo (0:22), **kamikazes (S_1)** no alto (0:26) e dupla de **escopeteiros (M_3)** (0:29) | Começar a sobrepor alvo resistente com ameaça móvel; ensinar a fugir do kamikaze e a atirar no escopeteiro quando ele não pode desviar; criar alvos para Crossfire |
| 0:33 – 0:43 | Tesoura de interceptadores (0:33), **tanque (M_1)** de contenção (0:37), escolta de caças rápidos (0:38,5) e parede móvel (0:43) | Cobrar leitura de prioridade, mudança de altura e uso do eco para cobrir outra faixa; o tanque é o obstáculo que explode |
| 0:47 – 0:54 | Artilheiros de rajada em pinça (0:47), kamikazes em duas linhas (0:49) e clímax de interceptadores em duas linhas (0:54) | Pico de densidade antes do Guardião, sem remover os avisos de entrada e de rajada |

Está em `Encounter01_PrimeiroCiclo.asset`: **15 entradas e 64 inimigos em 54 s** (o último cerco de caças
virou 3 kamikazes por linha). O encontro termina 2,5 s depois da última onda, quando não resta
inimigo vivo; depois de 2 s de respiro entra o Guardião → [06](06-chefe-e-runs.md). A torreta ainda não entra.

Os tempos são valores de partida para testar. Um encontro de Combinação, se entrar, deve reaproveitar os mesmos
inimigos, combinados com o gadget → [06](06-chefe-e-runs.md).

## Perguntas em aberto

- [ ] Limite rígido de inimigos simultâneos: `_TBD_`; hoje a densidade é controlada pela timeline autoral.
- [ ] Torreta: falta implementar.
- [ ] O caça mira e mergulha para onde a nave está: as ondas continuam por script, mas parte do movimento responde ao jogador. Isso combina com "o jogador aprende o padrão e planeja o que gravar", ou o mergulho deve ir para alturas fixas?
- [ ] Vida, pontos, tempos de tiro, de aviso e de explosão: primeira rodada validada tecnicamente; falta playtest externo.
- [ ] Kamikaze: o raio da explosão (1,7 u) e o tempo de 5,5 s até explodir sozinho são valores de partida. Ele deve poder ser desviado só com a esquiva, ou também por quem se mexe bem?
- [ ] Tanque: ele explode quando a nave chega perto, mas fica 11 s na tela. É obstáculo demais para o corredor de tiro, ou entra como aposta (matar de longe ou passar longe)?
- [ ] Escopeteiro: só a nave e o eco o fazem esquivar. A esquiva dele (2,4 u, recarga de 2,2 s) deixa a janela generosa o bastante para o eco acertá-lo?
- [ ] Ondas de um encontro de Combinação, se entrar: só o encontro 1 tem linha do tempo.
