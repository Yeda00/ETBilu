---
title: Chefe e estrutura das runs
chapter: "06"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §9 e §10; protótipo em Assets/_Project (GameSession)
---

# ONE MORE TIME | Chefe e estrutura das runs

O chefe do protótipo, fase a fase, e como os encontros de uma run ensinam e combinam as mecânicas.

## O Guardião do Ciclo *(nome provisório)*

O protótipo tem **um único chefe**, desenhado para mostrar o potencial da mecânica temporal: uma máquina
de combate que protege uma passagem temporal. O núcleo fica blindado durante a maior parte do encontro.

![O Guardião do Ciclo e suas quatro fases](../img/boss.svg)

| Fase | Nome | O que acontece | O jogador aprende |
|---|---|---|---|
| 1 | **Sobrevivência** | Projéteis em padrões simples; um laser varre a tela | Mover-se, achar janelas seguras e gravar uma rota |
| 2 | **Preparação** | Dois geradores laterais protegem o núcleo | Gravar tiros contra um gerador e atacar o outro enquanto o eco repete |
| 3 | **Sincronização** | Os dois geradores precisam cair numa janela curta; aí o núcleo fica vulnerável | Coordenar os disparos da nave com os do eco, vivo durante o padrão de projéteis |
| 4 | **Última chance** | Com pouca vida, mais ataques e janelas de vulnerabilidade menores | Aproveitar os ecos já criados para fechar o combate |

> **Critério de sucesso:** o chefe é vencível com uma boa sequência temporal, mas **não exige combinação
> pixel-perfect**. O desafio vem da leitura de padrões e do planejamento.

## Estrutura das runs

Runs curtas, com encontros que introduzem e depois combinam as mecânicas.

| Encontro | Conteúdo | Objetivo de design |
|---|---|---|
| 1. **Primeiro ciclo** | Inimigos simples e obstáculos básicos → [04](04-inimigos-e-ondas.md) | Ensinar movimento, tiro e gravação |
| 2. **Armadilha** | Laser e interruptor → [05](05-armadilhas-gadgets-puzzles.md) | Ensinar a interação entre eco e cenário |
| 3. **Combinação** | Inimigos, armadilha e gadget | Ensinar combos temporais |
| 4. **Guardião** | O chefe | Testar o domínio da mecânica |

No protótipo, os encontros podem ser segmentos de **uma única fase**, sem mapa de progressão.

## Progressão entre tentativas

A repetição deve reforçar o tema *One More Time*. Dois modelos estão em jogo:

![Os dois modelos de progressão entre tentativas](../img/progression.svg)

| Modelo | Como funciona | Situação |
|---|---|---|
| **A — Eco durante a vida** | O jogador grava e reproduz enquanto está vivo. A morte reinicia o encontro | **Recomendado para o protótipo** |
| **B — Ciclos acumulados** | Ao reiniciar, o jogador deixa um eco persistente da tentativa anterior | Evolução futura |

**No protótipo atual** o modelo A está implementado: ao morrer, o encontro recomeça depois de 2 s, sem ecos. O chefe ainda não existe; o encontro termina quando a última onda é vencida.

*Por quê:* o modelo A é mais previsível e mais fácil de balancear. O modelo B pode entrar depois, se
"tentar mais uma vez" também virar a estrutura narrativa do jogo.

## Perguntas em aberto

- [ ] O chefe tem quatro fases (§9) ou duas (tabela do §10, "chefe com duas fases de combate")?
- [ ] Nome definitivo do chefe — "O Guardião do Ciclo" é provisório.
- [ ] Confirmar o modelo A como sistema do protótipo (é o que está implementado).
- [ ] O Guardião do Ciclo: falta implementar.
