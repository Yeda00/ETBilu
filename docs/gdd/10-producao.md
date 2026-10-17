---
title: Produção — escopo da jam
chapter: "10"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §14, §15 e §16 — docs/source/GDD_One_More_Time_v0.1.pdf
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

## Critérios de aceitação do protótipo

O protótipo está pronto para teste quando:

1. [ ] O jogador grava e reproduz uma sequência de cinco segundos.
2. [ ] O eco repete movimentos e disparos sem depender do controle atual.
3. [ ] A reprodução continua funcionando quando os inimigos se movem ou morrem.
4. [ ] Uma armadilha pode ser manipulada por uma ação registrada.
5. [ ] O jogador combina o eco com a nave atual para criar pelo menos um ataque especial.
6. [ ] O chefe pode ser derrotado usando a mecânica temporal.
7. [ ] O jogador entende visualmente qual nave é a atual e qual é o eco.
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
- [ ] Equipe e quem faz o quê: `_TBD_`.
