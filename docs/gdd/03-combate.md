---
title: Combate
chapter: "03"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §5.1–5.4 — docs/source/GDD_One_More_Time_v0.1.pdf
---

# ONE MORE TIME | Combate

O que a nave do jogador faz, os tipos de dano, os combos temporais e o que a pontuação premia.

## Nave do jogador

A nave começa com um conjunto pequeno de ações:

- Movimento em **oito direções** dentro da área jogável.
- **Disparo principal** contínuo.
- Um **gadget** equipado → [05](05-armadilhas-gadgets-puzzles.md).
- A habilidade de **gravação e reprodução** temporal → [02](02-eco-temporal.md).
- Vida limitada e **invulnerabilidade breve** depois de receber dano.

O jogador atira enquanto se move. O foco é esquivar e se posicionar, não alternar o tempo todo entre
movimento e ataque.

## Tipos de dano

| Tipo | Origem |
|---|---|
| **Direto** | Tiros convencionais da nave |
| **De gadget** | Minas, pulsos ou ataques especiais |
| **Combinado** | Efeitos especiais ativados quando as ações da nave atual e do eco coincidem |

Só três categorias, para simplificar o protótipo.

## Combos temporais

Os combos são **eventos específicos** que o jogo reconhece, não multiplicadores genéricos.

![Os cinco combos temporais](../img/combos.svg)

| Combo | Condição | Resultado |
|---|---|---|
| **Crossfire** | Nave atual e eco acertam o mesmo alvo numa janela curta | Dano adicional |
| **Trap Bait** | O eco atrai inimigos para uma armadilha | Inimigos atingidos pela armadilha |
| **Double Pulse** | Dois pulsos atingem mecanismos conectados | Sobrecarga ou desativação |
| **Reflect Chain** | Um projétil ricocheteia em escudos posicionados em sequência | Dano elevado ao alvo |
| **Synchronized Strike** | Dois pontos fracos são atingidos quase ao mesmo tempo | Escudo do chefe desativado |

Cada combo tem **feedback visual e sonoro próprio** → [08](08-direcao-visual-e-sonora.md).

*Por quê:* o jogador precisa entender por que a combinação funcionou — é assim que ele aprende a repeti-la.

## Pontuação por habilidade

O jogo pode premiar:

- Combos temporais.
- Inimigos eliminados por armadilhas.
- Sequências sem sofrer dano.
- Sincronizações precisas.
- Destruição de projéteis ou mecanismos usando o ambiente.

A pontuação **não é necessária** para terminar a fase. Ela incentiva o domínio da mecânica e a rejogabilidade.

## Perguntas em aberto

- [ ] Valores de vida, dano e duração da invulnerabilidade: `_TBD_`.
- [ ] Tamanho da "janela curta" de Crossfire e Synchronized Strike: `_TBD_` (§16 pede janelas generosas no começo).
