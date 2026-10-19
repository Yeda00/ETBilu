# A02 — Atualizar modelo da Bilu

| Status | Dono |
|---|---|
| done | Yeda |

**Objetivo:** trocar o modelo da nave e do eco pela nova versão.

## Decisões
- Usar já o nome final, conforme a convenção da [spec de arte](art.md) (nome: _TBD_).
- Renomear o arquivo atual junto com o `.meta`, depois sobrescrever o `.fbx`: os prefabs continuam ligados.

## Entradas
- `Exports/Bilu_BlockOut.fbx`.
- Hoje: `Assets/_Project/Art/Ships/BI_Ships_BlockOut.fbx` → `Prefabs/Ship/PlayerShip.prefab` e `Prefabs/Echo/EchoGhost.prefab`.

## Passos
1. Aplicar o caminho escolhido.
2. Conferir em `Visual/Model` dos dois prefabs: rotação, escala, materiais (ciano na nave, violeta translúcido no eco).
3. Dar Play: gravar e invocar um eco.

## Pronto quando
- [ ] Nave e eco com o modelo novo e as cores certas.
- [ ] Nave e eco continuam distinguíveis em jogo.

## Pedidos para a outra área
- Yeda: atualizar o nome do modelo em `CLAUDE.md` e GDD 08.

## Registro
- 2026-10-18 · spec criada
- 2026-10-18 · nomes finais desde o blockout
- 2026-10-18 · Yeda adotou `Bilu_BlockOut.fbx` na nave e no eco (`bb5fa22`); `BI_Ships_BlockOut` ficou sem uso. Nome final segue _TBD_. Cores ainda são as antigas (ciano e violeta).
- 2026-10-18 · artista texturizou a nave (`Exports/BiluShip_Model/Bilu_Ship.fbx` + mapas de cor e emissivo, 512 px). Entrou como `Art/Ships/Bilu_Ship.fbx`, com as configurações de importação do `Bilu_BlockOut`; os objetos foram renomeados no Blender (`.001`…), então a troca foi feita por ferramenta (*One More Time › Art › Swap In Bilu Ship Model*), não por cima do arquivo. Materiais do FBX ligados a `M_BiluShip` (cor + emissivo) e `M_BiluShip_Glow` (slot `BlueEmissive`, ciano liso). O eco segue violeta translúcido. `Bilu_BlockOut` ficou sem uso.
