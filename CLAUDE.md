# Contexto para agentes de IA neste repositório

ONE MORE TIME é um protótipo de game jam em Unity 6 (6000.3.25f1), URP, Input System.
Antes de qualquer mudança, leia [`docs/gdd.md`](docs/gdd.md) — o GDD de uma página — e,
se a tarefa tocar uma área específica, o capítulo correspondente em [`docs/gdd/`](docs/gdd/).

## Regras

1. **O GDD descreve o jogo como ele é.** Quando uma mudança no código ou nos assets fizer o
   jogo diferir do que o GDD diz (regra, número, controle, inimigo, armadilha, combo, chefe,
   HUD, escopo, arquitetura), atualize o capítulo afetado em `docs/gdd/` **no mesmo commit**.
   Se a mudança aparecer no resumo de uma página, atualize `docs/gdd.md` também.
   Código e GDD divergentes: o código é a verdade e o GDD é o bug.

2. **Regere as páginas visuais.** Depois de mudar um Markdown, atualize o HTML equivalente
   (`docs/gdd.html` para o resumo, `docs/gdd-full.html` para os capítulos) com o mesmo texto,
   para que as duas versões não divirjam.

3. **Mudança ainda em discussão não entra no GDD.** Uma proposta não confirmada pelo dono
   vai como `> **Q:**` no capítulo ou como pergunta em aberto, nunca como fato.

4. **Nada inventado.** O que não está decidido é `_TBD_` e vai para "Perguntas em aberto".
   Números de design são valores de partida, e isso deve ficar dito quando couber.

5. **Ilustrações acompanham a mecânica.** Se a mudança alterar algo que uma ilustração
   mostra (ex.: teclas, HUD, ondas, combos), atualize o SVG correspondente em `docs/img/`.

6. **Texto de jogo em português brasileiro,** como aparece no jogo. Nomes de código em inglês.

7. **Novo capítulo** começa de [`docs/gdd/_template.md`](docs/gdd/_template.md).

8. **Peça confirmação antes de push, merge ou exclusão.** Commits locais podem ser feitos
   quando o usuário pedir.

## Como aplicar a regra 1 numa tarefa

Ao terminar uma mudança de jogo, antes do commit:
- liste os capítulos afetados;
- confira se o que o GDD diz ainda vale;
- se não valer, edite o capítulo e o resumo, regenere os HTML e inclua tudo no mesmo commit.
