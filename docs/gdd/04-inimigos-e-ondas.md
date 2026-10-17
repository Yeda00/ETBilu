---
title: Inimigos e ondas
chapter: "04"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §5.5; protótipo em Assets/_Project/Data/Waves/Encounter01_PrimeiroCiclo.asset
---

# ONE MORE TIME | Inimigos e ondas

Quem a nave enfrenta, como cada inimigo conversa com o eco, e como as ondas entram na tela.

Os inimigos surgem durante o avanço da fase, entrando pela direita (e às vezes por cima ou por baixo).
Em vez de spawn aleatório, cada encontro segue uma **lista de ondas com tempo e posição definidos**.

*Por quê:* o jogador aprende o padrão e consegue planejar o que gravar no eco.

## Tipos de inimigo

![Os três tipos de inimigo e seus movimentos](../img/enemies.svg)

| Inimigo | Comportamento | Papel com o eco |
|---|---|---|
| **Drone** (reto) | Entra pela direita e voa em linha reta; não atira | Alvo fácil para Crossfire; ensina a gravação |
| **Ondulante** | Zigue-zague senoidal, em grupos de 3 a 5 | Pode ser conduzido para armadilhas (Trap Bait) |
| **Torreta** | Parada no cenário, atira na direção da nave | Exige que nave e eco ataquem de posições diferentes |

Para a jam, a recomendação é **começar com um tipo** e adicionar os outros dois só se o núcleo já estiver divertido.

## Regras de surgimento

- As ondas são definidas por script: tempo de entrada, ponto de entrada, tipo e quantidade.
- Cada onda **sinaliza a entrada** com um aviso visual curto (seta ou brilho na borda da tela).
- Inimigos que saem pela esquerda são removidos **sem penalidade**.
- O número de inimigos simultâneos é limitado, para a tela continuar legível com os ecos ativos.
- Matar inimigos **nunca é obrigatório** para avançar; o scroll continua.

## Exemplo — Encontro 1 (primeiro ciclo)

![Linha do tempo das ondas do encontro 1](../img/wave-timeline.svg)

| Tempo | Onda | Objetivo de design |
|---|---|---|
| 0:00 – 0:10 | Sem inimigos; só obstáculos básicos | Aprender movimento e tiro |
| 0:10 – 0:25 | 3 drones em fila, vindo do centro | Primeiros alvos, sem pressão |
| 0:25 – 0:40 | Drones em dois níveis de altura | Incentivar gravar uma rota e deixar o eco cobrir um nível |
| 0:40 – 1:00 | Primeiro grupo de ondulantes | Ler o padrão e atacar com nave e eco ao mesmo tempo |
| 1:00 – 1:20 | Torreta + drones | Introduzir a ideia de dividir tarefas entre nave e eco |

**No protótipo atual** o encontro 1 está em `Encounter01_PrimeiroCiclo.asset`, com estas diferenças:

- Drone e ondulante existem; a **torreta ainda não**, então 1:00–1:20 usa só drones.
- Os primeiros 10 s não têm obstáculos (ainda não existem): é voo livre.
- Há ondas extras em 0:33 (drones em dois níveis), 0:50 (ondulantes) e 1:10 (drones), para o encontro não ficar vazio.
- O aviso de entrada é um marcador piscando na borda direita, 0,8 s antes da onda.
- O encontro termina 4 s depois da última onda, quando não resta inimigo vivo; aí começa o corredor de armadilhas → [06](06-chefe-e-runs.md).

Os tempos são valores de partida para testar. As ondas dos encontros 2 e 3 reaproveitam os mesmos
três inimigos, combinados com as armadilhas e o gadget → [06](06-chefe-e-runs.md).

## Perguntas em aberto

- [ ] Limite de inimigos simultâneos na tela: `_TBD_` (o protótipo ainda não limita).
- [ ] Torreta: falta implementar.
- [ ] Ondas dos encontros 2 e 3: só o encontro 1 tem linha do tempo no v0.1.
