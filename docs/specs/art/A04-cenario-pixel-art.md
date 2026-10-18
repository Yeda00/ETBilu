# A04 — Cenário 2D em pixel art

| Status | Dono |
|---|---|
| doing | Artista |

**Objetivo:** substituir o cenário placeholder por pixel art 2D atrás da jogabilidade.

## Decisões
- Ambientação: Terra depois da exploração total. Deserto tipo sertão, sem vegetação e sem água; um mar que secou, com praia morta (referência: *Duna*).
- Elementos: megaprédios ao longe cortando o céu; máquinas de extração enferrujando na praia seca.
- Forma: brutalista, de blocos. Rápida de produzir e de impacto forte.
- Referência visual: [`docs/art/ref/cenario-noite-roxa.png`](../../art/ref/cenario-noite-roxa.png). Noite roxa, doente e poluída.
- Cores de jogo: ver bíblia, Cor.
- Paleta (Lospec): _TBD_. Precisa de rampas de roxo, ferrugem e poeira, mais o azul-água do eco e o vermelho emissivo.
- Misto: céu, nuvens e megaprédios pintados em 2D; máquinas, blocos e rochas em 3D low-poly, trocando os props do `BackdropScroller`.
- Inimigos com pouco contraste com o fundo são aceitos como parte da dificuldade; tiros e avisos de ataque continuam com contraste alto.
- Tamanho do tile e área de pintura: _TBD_. Base: o mundo é renderizado com 270 px de altura (480 × 270 em 16:9).
- Camadas de parallax e quais entram na demo: _TBD_. Candidatas: céu · megaprédios distantes · dunas/leito do mar · máquinas de extração · chão.
- Como as camadas 2D são desenhadas (sprites na cena 3D ou câmera ortográfica separada): _TBD_, com o Yeda.

## Entradas
- Fonte do fundo 2D: `iCloudDrive\Art repository_\Source Files_\TiagoGame_environment_.aseprite`. Canvas de 1536 × 768 em RGB; camada `sky` em modo Overlay; personagem em `Pasted Image`.
- Cenário atual: `BackdropScroller` com props placeholder em `Art/Placeholder`.
- Encontros na ordem: ondas → Guardião (GDD 06).

## Props 3D: medidas
- Ficam de 16 a 70 unidades atrás do plano de jogo. Nessa distância 1 pixel da tela ≈ 0,1 a 0,24 unidade: detalhe menor que ~0,5 unidade some.
- Tamanho: de 8 a 20 unidades de altura. Pivô na base, Y para cima.
- Low-poly, 3 a 4 materiais de cor chapada.
- Export: mesmas configurações das naves, em `Exports/Environment/`.

## Riscos
- Inimigo escuro em céu roxo escuro: o pouco contraste é intencional, mas validar em D02 se a dificuldade não vira injustiça.
- Azul nos inimigos contra o azul-água do eco: azul dos inimigos escuro e opaco; o do eco claro e translúcido.
- Luzes azuis do cenário (janelas, como na referência) competem com o eco: poucas e apagadas.
- Prazo: priorizar as camadas grandes (céu, prédios, leito do mar) antes dos detalhes.

## Passos
1. Fechar paleta e contraste e registrar na bíblia.
2. Definir as camadas da demo e o tamanho de cada uma.
3. Produzir, importar e testar em jogo.

## Pronto quando
- [ ] Cenário novo em jogo, sem competir com nave, eco e perigos.

## Pedidos para a outra área
- Yeda: `BackdropScroller` foi feito para destroços no espaço: props flutuam (altura de −10 a 6), giram 360° e esticam até 3× na vertical. Para máquinas e blocos no chão: altura fixa no chão, sem esticar, giro limitado.
- Yeda: um chão (leito do mar seco) que role com a cena; hoje não existe.
- Yeda: como colocar o fundo pintado em 2D atrás da cena.
- Yeda: cor da névoa da cena em roxo, para os props distantes se fundirem com o céu.
- Yeda: se a regra de cor mudar, atualizar GDD 08 (legibilidade é mecânica).

## Registro
- 2026-10-18 · spec criada
- 2026-10-18 · ambientação, elementos e forma definidos pelo artista
- 2026-10-18 · regra de cor nova (Bilu Fusca, eco água, inimigos preto e vermelho, noite roxa) e referência adicionada
- 2026-10-18 · técnica mista; o 3D aproveita o `BackdropScroller` existente, sem código novo
- 2026-10-18 · fundo 2D vem de `BG-parahybaSurrealismo.aseprite` (551 × 270, já na altura do jogo). Camadas exportadas para `Exports/Environment/BG_Parahyba/`: PNG RGBA no tamanho do canvas, opacidade da camada aplicada, pixels iguais à fonte. Cores ainda do arquivo original.
- 2026-10-18 · uma camada de névoa vai quebrar a base das camadas de parallax (ideia do artista)
- 2026-10-18 · fundo recolorido com as cores da referência roxa (cada cor mantém sua posição de claro a escuro na camada e recebe a cor da camada equivalente da referência). Camadas renomeadas `fog_low` e `clouds_high`. O original segue no iCloud.
- 2026-10-18 · artista ajustou no Photoshop (janelas da torre em vermelho, névoa e torre juntas) e exportou 4 camadas para `Exports/Environment/`: `Sky_paralax00` (480 × 270, opaco), `FogCity_paralax01` (551 × 270), `clauds_Paralax02` (519 × 148), `Mountain_paralax03` (551 × 270). Alinhadas pelo canto superior esquerdo. Fonte: `Source/BG_Parahyba.aseprite` + PNGs em `Source/`. Faltam os loops.
- 2026-10-18 · fundo no Unity, sem código: texturas em `Art/Environment/Backgrounds` (PPU 270, Point, sem compressão, pivô no canto superior esquerdo) e dois prefabs em `Art/Environment`. `BG_Parahyba` vai no grupo `World`; `BG_Parahyba_Sky` vai como filho da `Main Camera` (céu parado). O parallax vem da câmera em perspectiva: cada camada fica a uma distância da câmera (céu 145, torre 130, nuvens 100, planalto 60) e é escalada para que 1 pixel da arte = 1 pixel do render. Camadas em modo Tiled até x = 3500. Limite: o far clip da câmera é 150.
- 2026-10-18 · torre (`FogCity`) presa à câmera, espelhada (prédio à direita) e sem repetir: está tão longe que fica sempre à vista. Um leve deslize exigiria script (pedido ao Yeda, se quiser).
- 2026-10-18 · nuvens divididas por tamanho, só recortando os pixels: `clauds_Paralax02_front` (5 grandes, distância 85, vento 1,2 u/s) e `_back` (9 pequenas, distância 115, vento 0,6 u/s). Vento da direita para a esquerda por animação em loop (componente Animation, sem código); cada loop anda exatamente um tile, então a volta não aparece. Velocidades são valores de partida.
- 2026-10-18 · vento 3× mais rápido, para ficar visível: frente 3,6 u/s, fundo 1,8 u/s.
