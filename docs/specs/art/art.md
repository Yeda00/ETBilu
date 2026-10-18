# Arte — spec principal

Orienta a produção da arte e alimenta a [bíblia de arte](../../art-bible.md). Dono: o artista. O agente implementa no Unity; decisões são do artista.

## Processo

1. Blender (`BI_Ships.blend`) → export FBX para `G:\Meu Drive\3D_Production\Projects\Bilus_Invasion\Exports`.
2. Copiar para `Assets/_Project/Art/…` no Unity.
3. Conferir no prefab e no Play mode.
4. Registrar na sub-spec; o que virou regra vai para a bíblia.

## Limites

- A arte mexe só em `Assets/_Project/Art/**`, materiais e no filho `Visual/Model` dos prefabs.
- Colisores, scripts, dados de ondas e layout da cena: pedido para o Yeda na sub-spec.
- Mesmo nome de FBX → sobrescrever só o `.fbx`; nunca editar ou apagar o `.meta`.
- Não renomear objetos dentro do modelo: os prefabs aplicam os materiais pelo nome.
- Ajuste de rotação, escala e centro vai em `Visual/Model`, nunca em `Visual` (o código reescreve a rotação dele).
- Nunca apagar `PlayerShip`, `EchoGhost` ou `Guardian`: o bootstrap refaz a versão sem modelo.

## Nomes

- Nome final desde o blockout; o estágio (blockout, final) fica na bíblia, não no arquivo.
- Convenção: _TBD_.
- Renomear um modelo no Unity, ou o `.fbx` e o `.meta` juntos: o GUID se mantém e os prefabs não quebram.
- Renomear depois do push do Yeda com os inimigos novos ([A03](A03-variantes-inimigos.md)), para não conflitar.

## Marcos

Os de [design](../design/design.md): arte congelada às 16:00.

## Sub-specs

| ID | Tarefa | Status |
|---|---|---|
| [A01](A01-modelo-guardiao.md) | Atualizar modelo do Guardião | done (Yeda) |
| [A02](A02-modelo-bilu.md) | Atualizar modelo da Bilu | done (Yeda) |
| [A03](A03-variantes-inimigos.md) | Variantes de inimigos | done (Yeda) |
| [A04](A04-cenario-pixel-art.md) | Cenário (misto 2D + 3D) | doing |
| [A05](A05-vfx-eco.md) | Visual do eco: rastro e glow | todo |
| [A06](A06-texturas-modelos.md) | Texturas dos modelos | todo · pós-demo |

## Registro de decisões

- 2026-10-18 · Spec e bíblia criadas; bíblia só com o que já está decidido.
- 2026-10-18 · Nomes finais desde o blockout. Variantes de inimigos ficam com o Yeda.
- 2026-10-18 · Cenário (A04) é a prioridade da arte.
- 2026-10-18 · Commit `bb5fa22` do Yeda fecha A01, A02 e A03. Falta conferir em jogo.
