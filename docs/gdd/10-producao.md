---
title: Produção — escopo da jam
chapter: "10"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §14, §15 e §16; estado do protótipo em 2026-10-17 (armadilhas, gadget, chefe e os dois primeiros combos)
---

# ONE MORE TIME | Produção — escopo da jam

O que entra na jam, o que fica de fora, quando o protótipo está pronto para teste e o que pode dar errado.

O objetivo é **validar se a mecânica central é divertida**, não construir todo o conteúdo previsto para o jogo final.

## Escopo

| Obrigatório — MVP | Opcional — só depois de validar o MVP | Fora do escopo inicial |
|---|---|---|
| Nave com movimento e disparo | Até três ecos simultâneos | Rebobinar o mundo inteiro |
| Uma arena side-scrolling 2.5D | Um combo temporal com feedback próprio | Campanha extensa |
| Gravação de cinco segundos | Um puzzle de sincronização | Vários chefes |
| Reprodução de movimento e disparos | Um segundo gadget | Progressão roguelite complexa |
| Pelo menos um tipo de inimigo (drone), com ondas por script | Pontuação por combos e esquivas | Árvore de habilidades |
| Duas armadilhas | | Muitos gadgets com sistemas de física independentes |
| Um gadget funcional | | |
| Um chefe com uma janela de vulnerabilidade temporal | | |
| Interface mínima e condição de vitória e derrota | | |

## Estado do MVP

| Item do MVP | Estado |
|---|---|
| Nave com movimento e disparo | ✅ |
| Uma arena side-scrolling 2.5D | ✅ cena `Arena_01`; naves e chefe com os modelos de blockout, o resto em placeholder |
| Gravação de cinco segundos | ✅ |
| Reprodução de movimento e disparos | ✅ |
| Inimigos com ondas por script | ✅ caça (S) e artilheiro (M), que atiram → [04](04-inimigos-e-ondas.md) |
| Duas armadilhas | ✅ laser gate (com ciclo e preso a um interruptor) e barreira móvel → [05](05-armadilhas-gadgets-puzzles.md) |
| Um gadget funcional | ✅ mina temporal, na tecla Q, reproduzida pelo eco → [05](05-armadilhas-gadgets-puzzles.md) |
| Um chefe com janela de vulnerabilidade temporal | ✅ Guardião do Ciclo, fases 1 a 3 → [06](06-chefe-e-runs.md) |
| Interface mínima e condição de vitória e derrota | ✅ vitória = derrotar o Guardião; derrota recomeça o encontro atual |

Dos opcionais, já entraram **dois combos temporais com aviso no HUD** (Crossfire e Synchronized Strike) e a
**pontuação por combos** → [03](03-combate.md), e o **áudio**: efeitos e música CC0 ligados ao jogo → [08](08-direcao-visual-e-sonora.md).

## Critérios de aceitação do protótipo

O protótipo está pronto para teste quando:

1. [x] O jogador grava e reproduz uma sequência de cinco segundos.
2. [x] O eco repete movimentos e disparos sem depender do controle atual.
3. [x] A reprodução continua funcionando quando os inimigos se movem ou morrem.
4. [x] Uma armadilha pode ser manipulada por uma ação registrada. (O eco segura o interruptor do corredor enquanto a nave passa.)
5. [x] O jogador combina o eco com a nave atual para criar pelo menos um ataque especial. (Crossfire: nave e eco no mesmo alvo dão dano combinado. Synchronized Strike: um gerador do Guardião pela nave e o outro pelo eco derrubam o escudo do núcleo. Os dois têm aviso no HUD → [03](03-combate.md).)
6. [x] O chefe pode ser derrotado usando a mecânica temporal. (O eco derruba um gerador enquanto a nave derruba o outro.)
7. [ ] O jogador entende visualmente qual nave é a atual e qual é o eco. (Com o modelo de blockout, a nave ciano e o eco violeta translúcido já se distinguem; falta testar com jogadores.)
8. [ ] A mecânica continua legível nos momentos com mais projéteis.

> **O teste mais importante:** ver alguém jogar **sem explicar a solução**. A pessoa precisa perceber
> sozinha que pode gravar uma ação e aproveitar a repetição para fazer outra.

## Riscos de design e mitigação

| Risco | Mitigação |
|---|---|
| O jogador não entende para que serve o eco | Um encontro inicial em que a solução exige claramente duas ações simultâneas |
| Os ecos deixam a tela confusa | Limitar a quantidade, reduzir a opacidade e distinguir os efeitos visuais |
| A gravação exige precisão excessiva | Janelas de sincronização generosas no começo |
| Os combos viram só bônus de dano | Interações com armadilhas, escudos e pontos fracos |
| O sistema fica complexo de implementar | Reproduzir só dados e eventos do jogador, sem rebobinar a cena |
| O chefe parece injusto | Sinalizar os ataques, as janelas de vulnerabilidade e o resultado de cada combo |

## Perguntas em aberto

- [ ] Data e regras da game jam: `_TBD_`.
- [ ] Os critérios 4, 5 e 6 foram verificados pelo Unity CLI, não com jogadores. Falta o teste sem explicação.
- [ ] Equipe e quem faz o quê: `_TBD_`.
