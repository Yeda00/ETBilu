---
title: Controles e interface
chapter: "07"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §11; protótipo em Assets/_Project/Input e Assets/_Project/UI
---

# BILU'S INVASION | Controles e interface

Como o jogador pilota, grava e invoca ecos sem parar de desviar — e o que o HUD precisa mostrar.

## Controles propostos

![Mapa de teclas proposto](../img/controls.svg)

![Mapa do controle (gamepad)](../img/controls-gamepad.svg)

| Ação | Teclado e mouse | Controle |
|---|---|---|
| Movimentar nave | WASD ou setas | Stick esquerdo |
| Mirar | Mover o mouse; a linha e o retículo ciano mostram a direção do próximo tiro | Stick direito (solto, a nave mantém a última mira) |
| Disparo principal | Espaço ou botão esquerdo do mouse | RT |
| Gravar sequência | Segurar Shift | Segurar LT |
| Invocar eco | E | RB |
| Usar gadget | Q | LB |
| Pausar | Esc | Start |

Os atalhos são provisórios, e o jogador pode trocá-los na tela de controles (abaixo).

*Por quê:* o que importa é **separar a gravação da reprodução**, para o jogador não invocar o eco sem
querer enquanto tenta desviar de uma armadilha.

No protótipo, estas ações estão em `Assets/_Project/Input/OneMoreTimeControls.inputactions` (mapa `Gameplay`), lidas só pelo `PlayerInputReader`. A ação `Aim` lê a posição do ponteiro e o `ShipAim` a projeta no plano de jogo; o movimento continua independente da mira. Com um controle, a ação `AimStick` (stick direito) dá a direção da mira. O leitor sabe qual dispositivo o jogador usou por último (`UsingGamepad`): é ele que decide se a mira vem do mouse ou do stick e quais dicas aparecem no HUD. Q solta a mina temporal → [05](05-armadilhas-gadgets-puzzles.md). Os menus usam o mapa `Menu` do mesmo asset, lido só pelo `MenuInputReader`.

## HUD

![Mockup conceitual do HUD](../img/hud.svg)

*Mockup conceitual — não representa uma interface implementada.*

O HUD do protótipo (UI Toolkit: `Hud.uxml` e `Hud.uss`) já mostra integridade, memória temporal com o
tempo gravado, os três espaços de eco, a pontuação, o gadget com a recarga ("Q · pronta" ou "recarga 1,2 s")
e as mensagens de pausa, derrota ("TENTE NOVAMENTE", e o encontro recomeça) e vitória ("GUARDIÃO DERROTADO", com pontuação, abates e combos). Com o
Guardião em cena, um painel no topo mostra a vida do núcleo, os dois geradores, a fase e o estado do núcleo:
blindado, protegido pelos geradores, a janela aberta quando um gerador cai, ou vulnerável com o tempo que resta.

A linha de dicas no pé da tela mostra os controles do dispositivo usado por último (teclado ou controle), com as teclas como estão agora.

O **aviso de combo** aparece no alto da tela, no centro, por 1,4 s, como no mockup: o nome do combo e o que o
causou, com os pontos → [03](03-combate.md).

| Combo | Aviso |
|---|---|
| Crossfire | "CROSSFIRE" · "NAVE + ECO NO MESMO ALVO · +100" |
| Synchronized Strike | "SYNCHRONIZED STRIKE" · "NAVE + ECO NOS GERADORES · ESCUDO DESATIVADO · +1000" |

O mesmo combo de novo enquanto o aviso está na tela conta "×2", "×3"; um Crossfire não cobre um Synchronized
Strike que ainda está à vista. A duração fica em `HUD` → `HudController`.

O HUD mostra com clareza:

| Elemento | Por que o jogador precisa |
|---|---|
| Vida restante (integridade) | Saber quanto risco ainda pode correr |
| Estado e duração da gravação (memória temporal) | Planejar a sequência sem olhar o relógio |
| Quantidade de ecos ativos | Saber o que já está trabalhando por ele |
| Gadget equipado e recarga | Saber quando pode usar de novo |
| Vida do chefe e indicação de vulnerabilidade | Saber quando atacar |
| Feedback de combo temporal ativado | Entender o que funcionou → [03](03-combate.md) |

## Telas do jogo

O jogo abre no menu e só depois vai para a arena. O estilo é inspirado nos menus de *Star Fox 64*: fundo preto
com estrelas, barras de aço com texto azul-escuro, um mapa de missões e uma história que sobe pela tela. A nave
Bilu aparece em 3D atrás do título e do menu.

| Tela | O que tem | Como sai |
|---|---|---|
| Título | "BILU'S INVASION", "O PASSADO CONTINUA LUTANDO" e "PRESSIONE ENTER / START" piscando; música do título | Qualquer tecla, botão do controle ou clique: menu principal |
| Menu principal | JOGO PRINCIPAL · CONTROLES · SOM · CRÉDITOS · SAIR | Esc volta ao título |
| Som | Barras de MÚSICA e EFEITOS de 0 a 10, salvas entre partidas; o ajuste vale na hora | Esc ou Enter volta |
| Controles | Uma linha por ação, com uma célula para o teclado e outra para o controle; Enter (ou A) numa célula espera a nova tecla ou botão, Esc cancela. "RESTAURAR PADRÃO" volta tudo ao original | Esc volta |
| Créditos | Os créditos de áudio (tudo CC0). A equipe é _TBD_ | Esc volta |
| Mapa de missões | Sete casas ligadas por uma linha; só a primeira (Missão Nº 1) existe, as outras são "?" bloqueadas e respondem com um som de negação | Enter abre a história; Esc volta |
| História | Imagem pequena no alto e o texto da missão subindo, com música de abertura | Enter pula, ↓ acelera, Esc volta; no fim, a arena |
| Cartão de missão | Na arena, tela preta com "MISSÃO Nº 1", o local e o nome da missão, por cerca de 3 s; a fase só começa quando ele some | Sozinho. Não volta depois de uma morte |

Teclas do menu: ↑ ↓ ← → ou W A S D movem (repetem se a tecla ficar apertada), Enter ou Espaço confirmam, Esc ou
Backspace voltam. No controle: D-pad ou stick esquerdo movem, A ou Start confirmam, B volta. O mouse também funciona: passar sobre uma barra a seleciona e clicar confirma.

A ordem das telas, o cursor, a rolagem do texto e os volumes são regras do Core, com testes (`ScreenFlow`,
`MenuCursor`, `CrawlScroll`, `SoundLevels`). Os textos ficam em dois assets, para editar sem mexer em código:
`Data/Menu/MenuTexts` (as palavras de todas as telas) e `Data/Missions/Mission_01` (`MissionData`: número, local,
nome, texto da história, imagem e cena). O layout e o visual ficam em `UI/Menu.uxml`, `Menu.uss`,
`MissionCard.uxml` e `MissionCard.uss`.

> **Q:** o texto da história e o nome da Missão Nº 1 ("Terra · Sertão Seco", "Derrubar o Império") são
> rascunho, escritos a partir da premissa da [bíblia de arte](../art-bible.md). O dono precisa aprovar ou
> reescrever. A imagem da história é um recorte da arena, só para ocupar o lugar da arte final.

### Trocar os controles

Na tela de controles, o jogador troca a tecla de cada ação (e o botão do controle). Valem as regras:

- **Só o que faz sentido trocar.** As quatro direções (a primeira composição, WASD), atirar, gravar, invocar, mina e pausar. A mira (mouse e stick direito) e o movimento do controle (stick esquerdo) são fixos, e aparecem apagados. As setas e o clique esquerdo continuam valendo como segunda opção.
- **Nenhum botão faz duas coisas.** Se o jogador escolhe uma tecla que já era de outra ação, as duas trocam de lugar.
- **Ficam salvos** entre partidas (`ControlsStore`, em `PlayerPrefs`), e o jogo e o menu leem as mesmas teclas. A dica no pé do HUD acompanha.
- Esc (teclado) e Start (controle) cancelam a escolha; por isso não dá para trocar uma ação para esses botões pela tela, só voltando ao padrão.

As linhas da tela, os nomes dos botões em português e a regra de conflito são do Core, com testes (`ControlLayout`,
`BindingLabels`, `BindingConflicts`); `ControlsCatalog` e `ControlsStore` ficam em `Gameplay/Input`.

## Perguntas em aberto

- [x] Suporte a controle (gamepad): entrou no protótipo, com os botões da tabela acima e remapeamento.
- [ ] Remapear também o movimento do controle e a mira (mouse e stick): hoje são fixos.
- [ ] Mapeamento definitivo das teclas no Input System.
- [ ] Fonte pixelada para os menus e o HUD: o protótipo usa a fonte padrão do Unity.
- [ ] Tela de título definitiva: o protótipo mostra a nave de blockout.
- [ ] O aviso de combo usa laranja, como o mockup; laranja também é a cor de perigo → [08](08-direcao-visual-e-sonora.md). Fica assim, ou o combo ganha uma cor própria?
