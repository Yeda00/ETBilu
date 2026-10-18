# A03 — Variantes de inimigos

| Status | Dono |
|---|---|
| done | Yeda |

**Objetivo:** inimigos novos a partir dos modelos M_1/2/3 e S_1/3.

## Decisões
- As variantes são inimigos novos.
- O Yeda implementa. A arte não mexe nesses modelos até o push dele.

## Entradas
- `Exports/Enemie_M_1_BlockOut.fbx`, `Enemie_M_2_BlockOut.fbx`, `Enemie_M_3_BlockOut.fbx`.
- `Exports/Enemie_S_1_BlockOut.fbx`, `Enemie_S_3_BlockOut.fbx`.

## Passos
1. Esperar o push do Yeda e dar pull.
2. Conferir os inimigos novos em jogo (cor de perigo, leitura).
3. Aplicar a convenção de nomes da [spec de arte](art.md).

## Pronto quando
- [ ] Inimigos novos em jogo, com os nomes finais.

## Pedidos para a outra área
- Yeda: avisar quando der push.

## Registro
- 2026-10-18 · spec criada
- 2026-10-18 · variantes são inimigos novos; implementação com o Yeda
- 2026-10-18 · Yeda implementou como variações de modelo sorteadas dentro dos tipos S e M (`ModelVariants`), não como tipos novos (`bb5fa22`). `Enemie_M_BlockOut` ficou sem uso (igual ao `M_2`). Artista confirma se é isso.
