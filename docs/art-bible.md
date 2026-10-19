# BILU'S INVASION | Bíblia de arte

Como o jogo se parece. Só o que já está decidido; cresce conforme as [specs de arte](specs/art/art.md) fecham decisões.
Mecânica e regras de jogo ficam no [GDD](gdd.md).

## Tom

Comédia, nascida de uma piada. Em outubro de 2010 o ET Bilu pediu à humanidade que buscasse conhecimento (o meme "busquem conhecimento"). Ela não buscou: a Terra virou um império fascista governado pela ignorância, que esgotou o planeta. Em 2052, 42 anos depois, o Bilu, puto, invade para derrubar o império e entregar a mensagem mais uma vez. Texto da abertura: `Data/Missions/Mission_01` ([GDD 07](gdd/07-controles-e-interface.md)).

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

## Logo

`Art/UI/Logo_BilusInvasion.png`: o letreiro desenhado pelo artista (BILU'S em forma de domo, INVASION abrindo em pirâmide), finalizado à mão no Aseprite. BILU'S vem em faixas de roxo, violeta e azul, com contorno ciano e extrusão escura; INVASION, em vermelho com planos de luz coral e sombra vinho. Pixel art de 215 × 189 px e 12 cores, mostrada com 640 px de largura (cerca de 3×) na tela de título, para não cobrir a nave. O sketch, as bases P&B e de cor (`.aseprite`) e as versões em alta resolução ficam com o artista, na pasta `Logo` do projeto de produção.

## Modelos

| Arquivo | Uso |
|---|---|
| `Bilu_Ship` | Nave e eco. Texturizado no Substance Painter: `Textures/Bilu_Ship_ColorMap` e `_EmissiveMap` (512 px), materiais `M_BiluShip` e `M_BiluShip_Glow` |
| `Enemie_S_1`, `Enemie_S`, `Enemie_S_3_BlockOut` | Caça (S): uma sorteada por inimigo |
| `Enemie_M_1`, `_2`, `_3_BlockOut` | Artilheiro (M): uma sorteada por inimigo |
| `Enemie_Boss_BlockOut` | Guardião do Ciclo |

Pipeline e regras de importação: [spec de arte](specs/art/art.md).
