---
title: Direção visual e sonora
chapter: "08"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §12 — docs/source/GDD_One_More_Time_v0.1.pdf
---

# ONE MORE TIME | Direção visual e sonora

Como o jogo se parece e soa, com uma regra: a tela e o som têm que explicar a mecânica temporal.

## Direção visual

Estética de **arcade sci-fi**: silhuetas legíveis, efeitos de energia e contrastes fortes.

### 2.5D: mundo 3D, leitura 2D

O jogo é um **side-scrolling 2.5D**: o mundo e a jogabilidade são 3D, vistos por uma câmera lateral que lê como 2D. **Alguns elementos serão sprites 2D**, misturados ao mundo 3D.

*Por quê:* a leitura lateral mantém o bullet hell legível, e o 3D dá profundidade, luz e efeitos de energia.

> **Q:** quais elementos são modelos 3D e quais são sprites 2D ainda não está definido (ex.: nave, eco, inimigos, projéteis, efeitos, cenário). Marcado como `_TBD_` até o dono decidir.

### Cor

| Elemento | Cor | Regra |
|---|---|---|
| **Nave atual** | Azul ou ciano intenso | Contorno nítido; disparos bem visíveis |
| **Eco temporal** | Transparente, contorno luminoso | Rastro que sugere repetição; distinguível da nave atual mesmo com muitos projéteis |
| **Inimigos e perigos** | Laranja e vermelho | Sinal visual claro antes de cada ataque |

*Por quê:* com até três ecos e padrões de bullet hell, a cor é o que diz ao jogador quem ele é, quem
é o passado dele e o que o machuca.

## Direção sonora

O áudio também comunica a mecânica temporal.

| Momento | Som |
|---|---|
| Gravação | Pulso eletrônico que aumenta de frequência |
| Invocação do eco | Som de reversão seguido de um impacto digital |
| Reprodução | Versão filtrada dos sons da nave original |
| Combo temporal | Efeito sonoro exclusivo e curto |
| Chefe vulnerável | Mudança musical ou sinal sonoro marcante |

> **Q:** fazer os disparos do eco soarem levemente abafados, como se viessem de outra camada temporal.

## Perguntas em aberto

- [ ] Quem é dono da arte e do áudio?
- [ ] Referências visuais além de *Cuphead* (câmera): `_TBD_`.
- [ ] Quais elementos são modelos 3D e quais são sprites 2D: `_TBD_`.
- [ ] A câmera lateral é fixa e ortográfica, ou tem alguma perspectiva? `_TBD_`.
