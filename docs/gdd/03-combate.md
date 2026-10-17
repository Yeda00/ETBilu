---
title: Combate
chapter: "03"
status: draft
owner: _TBD_
updated: 2026-10-17
source: GDD v0.1 (PDF) §5.1–5.4; protótipo em Assets/_Project (prefabs PlayerShip, Drone, Ondulante)
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

## Valores de partida no protótipo

| Item | Valor | Onde mudar |
|---|---|---|
| Integridade da nave | 3 | `PlayerShip` → `Health` |
| Invulnerabilidade após dano | 1,2 s | `PlayerShip` → `Health` |
| Velocidade da nave | 9 u/s | `PlayerShip` → `ShipMotor` |
| Disparo principal | 10 tiros/s, dano 1 | `PlayerShip` → `ShipWeapon`, `Shot_Ship` |
| Drone | 2 de vida, 100 pontos | `Drone` |
| Ondulante | 1 de vida, 100 pontos | `Ondulante` |
| Inimigo que bate na nave | 1 de dano | `ContactDamage` |

Valores para testar, não definitivos.

## Tipos de dano

| Tipo | Origem |
|---|---|
| **Direto** | Tiros convencionais da nave |
| **De gadget** | Minas, pulsos ou ataques especiais |
| **Combinado** | Efeitos especiais ativados quando as ações da nave atual e do eco coincidem |

Só três categorias, para simplificar o protótipo. O código também marca o dano de **contato** (um inimigo
que bate na nave), que o GDD v0.1 não lista.

O protótipo já separa os tiros da nave dos tiros do eco (`ShotSource`), o que os combos vão precisar.

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

No protótipo atual a pontuação é só **100 por inimigo abatido**, como placeholder; os prêmios acima ainda não existem.

## Perguntas em aberto

- [ ] Valores de vida, dano e invulnerabilidade: os de partida estão na tabela acima; falta validar jogando.
- [ ] O dano de contato entra como quarta categoria, ou é parte do dano direto?
- [ ] Combos temporais: nenhum implementado ainda.
- [ ] Tamanho da "janela curta" de Crossfire e Synchronized Strike: `_TBD_` (§16 pede janelas generosas no começo).
