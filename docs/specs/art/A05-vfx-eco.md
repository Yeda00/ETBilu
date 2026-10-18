# A05 — Visual do eco: rastro e glow

| Status | Dono |
|---|---|
| todo | Artista |

**Objetivo:** separar o eco do Bilu (os dois são azuis) com translucidez, rastro e glow.

## Decisões
- Eco azul-esverdeado de água, translúcido; Bilu azul-calcinha opaco.
- Rastro e glow: forma _TBD_.

## Entradas
- `Prefabs/Echo/EchoGhost.prefab`, material do eco.

## Passos
1. Ajustar a cor e a transparência do material do eco.
2. Testar rastro (Trail Renderer) e glow (emissão + bloom do URP).
3. Conferir com três ecos e muitos tiros na tela.

## Pronto quando
- [ ] Bilu e eco distinguíveis num relance, também no momento mais cheio.

## Pedidos para a outra área
- Yeda: o rastro fica fora de `Visual/Model`; combinar onde colocá-lo no prefab. Se o bloom não estiver ativo no URP, ativar.

## Registro
- 2026-10-18 · spec criada
