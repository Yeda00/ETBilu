# BILU'S INVASION | Bíblia de arte

Como o jogo se parece. Só o que já está decidido; cresce conforme as [specs de arte](specs/art/art.md) fecham decisões.
Mecânica e regras de jogo ficam no [GDD](gdd.md).

## Tom

Comédia, nascida de uma piada. O ET Bilu pediu à humanidade que buscasse conhecimento. Ela não buscou: a Terra virou um império fascista governado pela ignorância, que esgotou o planeta. O Bilu, puto, invade para derrubar o império.

## Estética

| | |
|---|---|
| Estilo | Arcade sci-fi, 2.5D: naves e chefe em 3D, câmera lateral |
| Visual | Pixelado: o mundo é renderizado com 270 px de altura e ampliado sem suavizar (`PixelView`); o HUD fica nítido |
| Inimigos | Mechas inspirados em *Gurren Lagann* |
| Cenário | Pixel art 2D: sertão sem vegetação nem água, mar seco, megaprédios distantes, máquinas de extração enferrujadas → [A04](specs/art/A04-cenario-pixel-art.md) |
| Forma | Brutalista, de blocos |
| Técnica do cenário | Misto: fundo pintado em 2D (céu, nuvens, megaprédios), props 3D low-poly no meio (máquinas, blocos, rochas) |
| Modelos | Cores chapadas na demo; color atlas ou textura pixel art pintada à mão depois → [A06](specs/art/A06-texturas-modelos.md) |

## Cor

| Elemento | Cor |
|---|---|
| Bilu | Azul-calcinha (Fusca clássico brasileiro), opaco, detalhes verde e amarelo |
| Eco | Azul-esverdeado, cor de água: contraste com o mundo sem água. Translúcido, com rastro e glow para se separar do Bilu |
| Inimigos | Preto metálico, puxado para o cinza e o vermelho; vermelho emissivo; detalhes azuis que os destacam |
| Cenário | Roxo de noite doente e poluída |

Referência do cenário: [`art/ref/cenario-noite-roxa.png`](art/ref/cenario-noite-roxa.png).
Substitui a regra do GDD 08 (ciano, violeta, laranja); falta atualizar o GDD.

Paleta: _TBD_ ([A04](specs/art/A04-cenario-pixel-art.md)).

## Modelos

| Arquivo | Uso |
|---|---|
| `Bilu_BlockOut` | Nave e eco |
| `Enemie_S_1`, `Enemie_S`, `Enemie_S_3_BlockOut` | Caça (S): uma sorteada por inimigo |
| `Enemie_M_1`, `_2`, `_3_BlockOut` | Artilheiro (M): uma sorteada por inimigo |
| `Enemie_Boss_BlockOut` | Guardião do Ciclo |

Pipeline e regras de importação: [spec de arte](specs/art/art.md).
