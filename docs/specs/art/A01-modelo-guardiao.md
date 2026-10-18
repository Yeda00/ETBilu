# A01 — Atualizar modelo do Guardião

| Status | Dono |
|---|---|
| done | Yeda |

**Objetivo:** trocar o modelo do Guardião pela nova versão exportada.

## Decisões
- Mesmo nome de arquivo: troca direta, o prefab atualiza sozinho.

## Entradas
- `Exports/Enemie_Boss_BlockOut.fbx` (difere do que está no projeto em 18/10, 02:00).
- Destino: `Assets/_Project/Art/Ships/Enemie_Boss_BlockOut.fbx` → usado por `Prefabs/Boss/Guardian.prefab`.

## Passos
1. Sobrescrever só o `.fbx`.
2. Abrir `Guardian.prefab`: conferir forma, materiais (casco azul-escuro, visor vermelho) e escala.
3. Dar Play até o encontro do Guardião.

## Pronto quando
- [ ] Modelo novo no prefab, sem material perdido.
- [ ] Geradores e visor continuam onde a nave consegue acertar.

## Pedidos para a outra área
- Yeda: se as mandíbulas mudaram de tamanho ou posição, reposicionar colisores e geradores.

## Registro
- 2026-10-18 · spec criada
- 2026-10-18 · Yeda trocou o modelo e passou o Guardião para vista lateral (`bb5fa22`). Falta o artista conferir em jogo.
- 2026-10-18 · O Guardião agora tem peças destrutíveis (antena, olhos, dois canhões e dois conjuntos de dentes), pedidas pelo designer → [GDD 06](../../gdd/06-chefe-e-runs.md). Os colisores e marcadores são placeholder: a malha `Teeth` é uma só, e a `metalHair` é a antena. Pedido para a arte: separar dentes de cima e de baixo, antena e olhos, para trocarmos os marcadores pela arte.
