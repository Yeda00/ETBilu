---
title: Direção visual e sonora
chapter: "08"
status: draft
owner: _TBD_
updated: 2026-10-18
source: GDD v0.1 (PDF) §12; protótipo em Assets/_Project (cena Arena_01, modelos em Art/Ships)
---

# ONE MORE TIME | Direção visual e sonora

Como o jogo se parece e soa, com uma regra: a tela e o som têm que explicar a mecânica temporal.

Paleta, assets e pipeline de arte ficam na [bíblia de arte](../art-bible.md).

## Direção visual

Estética de **arcade sci-fi**: silhuetas legíveis, efeitos de energia e contrastes fortes.

### 2.5D: mundo 3D, leitura 2D

O jogo é um **side-scrolling 2.5D**: o mundo e a jogabilidade são 3D, vistos por uma câmera lateral que lê como 2D. **Alguns elementos serão sprites 2D**, misturados ao mundo 3D.

*Por quê:* a leitura lateral mantém o bullet hell legível, e o 3D dá profundidade, luz e efeitos de energia.

**No protótipo atual:** câmera em **perspectiva** (campo de visão 40°, a 20 unidades do plano de jogo), a
jogabilidade num plano em z = 0 e cenário 3D atrás dele, com névoa para dar profundidade.

Naves e chefe usam os modelos 3D de blockout do designer, em `Art/Ships` (do arquivo `BI_Ships.blend`):

| Modelo | Quem usa | Como aparece |
|---|---|---|
| `Bilu_BlockOut` | Nave e eco | Virado para a direita, topo do disco inclinado 15° para a câmera; ciano na nave, violeta translúcido no eco |
| `Enemie_S_1_BlockOut`, `Enemie_S_BlockOut`, `Enemie_S_3_BlockOut` | Caça (S) | Três variações, uma sorteada por caça; viradas para a nave, com a mesma inclinação; laranja |
| `Enemie_M_1_BlockOut`, `Enemie_M_2_BlockOut`, `Enemie_M_3_BlockOut` | Artilheiro (M) | Três variações, sorteadas do mesmo jeito; vermelho |
| `Enemie_Boss_BlockOut` | Guardião do Ciclo | Visto de lado, virado para a nave, topo inclinado 25° e rosto 15° para a câmera; casco azul-escuro, visor vermelho → [06](06-chefe-e-runs.md) |

As variações misturam peças entre os inimigos e mudam a cabeça. No caça: só a cabine (`S_1`), a cabine com
dois canhões longos (`S`, a primeira exportação; não há um `S_2`) e a cabine com garras (`S_3`). No
artilheiro: só a cabeça (`M_1`), a cabeça com garras (`M_2`) e a cabeça com os canhões do caça (`M_3`) → [04](04-inimigos-e-ondas.md).

Ao subir ou descer, nave, eco e inimigos inclinam até 25°: o topo vira para a câmera quando sobem e se
afasta quando descem.

Inimigos e chefe seguem a regra de cor: laranja e vermelho são perigo. O resto — tiros, armadilhas e cenário —
ainda é placeholder feito de primitivas, até a arte chegar.

> **Q:** quais elementos são modelos 3D e quais são sprites 2D ainda não está definido (ex.: projéteis, efeitos, cenário). Naves e chefe já são modelos 3D (blockout). Marcado como `_TBD_` até o dono decidir.

### Visual pixelado

O mundo 3D é desenhado numa **altura baixa fixa** e ampliado sem suavizar, então cada pixel vira um bloco
visível — o visual do *Star Fox* do SNES, a referência pedida pelo time em 2026-10-18. O HUD é desenhado depois,
em resolução cheia, e fica nítido.

| | No protótipo |
|---|---|
| Altura do mundo | 270 px (o *Star Fox* usa 224), arredondada para cada pixel ocupar um número inteiro de pixels da tela: em 1080p, blocos de 4 × 4 |
| Onde mudar | Componente `PixelView` em `Systems` (cena `Arena_01`), campo *Target Height*: menor deixa mais pixelado. Dá para mexer durante o Play mode |
| Desligar | Desmarcar o componente `PixelView` |

Valor de partida, para testar.

### Cor

| Elemento | Cor | Regra |
|---|---|---|
| **Nave atual** | Azul ou ciano intenso | Contorno nítido; disparos bem visíveis |
| **Eco temporal** | Transparente, contorno luminoso | Rastro que sugere repetição; distinguível da nave atual mesmo com muitos projéteis |
| **Inimigos e perigos** | Laranja e vermelho | Sinal visual claro antes de cada ataque |

*Por quê:* com até três ecos e padrões de bullet hell, a cor é o que diz ao jogador quem ele é, quem
é o passado dele e o que o machuca.

### Feedback de combate no protótipo

O feedback segue uma hierarquia para preservar a leitura do bullet hell:

| Evento | Resposta implementada |
|---|---|
| Acerto comum | Flash branco curto no alvo, poucas faíscas na cor da origem e microimpulso de câmera |
| Tiro bloqueado | Faíscas da origem e anel azul de deflexão; não dispara efeito de dano |
| Dano na nave | Flash vermelho, faíscas ciano, anel de alerta e shake mais claro com pequeno pulso de lente |
| Inimigo destruído | Explosão de partículas e anel expansivo que persistem depois que o inimigo some |
| Artilheiro, gerador ou núcleo destruído | Mesma composição em escala, densidade e shake progressivamente maiores |
| Combo temporal | **Convergência Temporal**: faíscas violetas e dois anéis ciano/violeta no ponto do combo |

`CameraImpact` usa trauma com limite de deslocamento e retorno rápido. Hits comuns não acumulam um tremor
constante; dano do jogador, inimigos resistentes, partes do chefe e o núcleo recebem pesos diferentes.
`CombatVfxPool` reaproveita sistemas de partículas e anéis, evitando criar e destruir objetos a cada tiro.

## Direção sonora

O áudio também comunica a mecânica temporal.

| Momento | Som |
|---|---|
| Gravação | Pulso eletrônico que aumenta de frequência |
| Invocação do eco | Som de reversão seguido de um impacto digital |
| Reprodução | Versão filtrada dos sons da nave original |
| Combo temporal | Efeito sonoro exclusivo e curto |
| Chefe vulnerável | Mudança musical ou sinal sonoro marcante |

Os clipes já estão no projeto, em `Assets/_Project/Audio`, mas **ainda não estão ligados ao jogo**: nenhum
som toca. Todos são CC0, do OpenGameArt (fontes e licenças em `Audio/CREDITS.md`):

| Pasta | Conteúdo |
|---|---|
| `SFX/Mini8bit` | 210 efeitos retrô em OGG: tiro (*Pew*), explosões, dano (*Ouch*), *Space*, *Teleport*, *Powerup*, *Pickup*, *Menu* e outros |
| `SFX/Deva8bit` | 46 efeitos retrô em WAV: tiro, explosões, impactos, power-ups, erros |
| `SFX/KenneySciFi` | 73 efeitos sci-fi em OGG: lasers, explosões, motores, campo de força |
| `SFX/Time` | Loop de reversão de tempo, candidato à invocação do eco |
| `UI` | Dois bipes de menu (seleção e destaque) |
| `Music` | Quatro faixas chiptune em loop: *Stage 1*, *Stage 2*, *Boss Fight* e *Stage Select* |

A camada sonora da tabela acima continua planejada: não há som de gravação nem de combo, e nenhum clipe foi
escolhido para cada evento. Os eventos de dano, morte e combo já estão separados para receber áudio sem
alterar as regras de combate.

> **Q:** fazer os disparos do eco soarem levemente abafados, como se viessem de outra camada temporal.

## Perguntas em aberto

- [ ] Quem é dono da arte e do áudio?
- [ ] Referências visuais: *Cuphead* (câmera) e *Star Fox* do SNES (visual pixelado); outras `_TBD_`.
- [ ] O HUD fica nítido, como hoje, ou também pixelado, como no *Star Fox*?
- [ ] Quais elementos são modelos 3D e quais são sprites 2D: `_TBD_` (naves e chefe já são modelos 3D).
- [ ] A câmera lateral é fixa e ortográfica, ou tem alguma perspectiva? O protótipo usa perspectiva; falta confirmar.
- [ ] O designer sugeriu uma lente de 300 mm: campo de visão bem estreito, com a câmera muito mais longe, o que deixa a perspectiva quase plana. O protótipo usa 40° a 20 u; falta testar e decidir.
