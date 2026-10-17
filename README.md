# ONE MORE TIME

Um shooter 2.5D de nave (mundo 3D, câmera lateral) em que o jogador grava e reproduz suas próprias ações passadas para lutar
ao lado de versões anteriores de si mesmo. Protótipo para game jam, em Unity 6 (6000.3), URP.

## Jogar o protótipo

Abra o projeto no Unity 6 (6000.3.25f1), abra `Assets/_Project/Scenes/Arena_01.unity` e dê Play.
Tudo é placeholder enquanto a arte não chega. A arena tem ondas de inimigos, um corredor de
armadilhas (o eco pode segurar o interruptor) e o Guardião do Ciclo; derrotá-lo é a vitória.

| Tecla | Ação |
|---|---|
| WASD ou setas | Mover a nave |
| Espaço ou clique | Atirar |
| Segurar Shift | Gravar até 5 s |
| E | Invocar o eco da gravação |
| Q | Soltar a mina temporal |
| Esc | Pausar |

Testes: Window › General › Test Runner › EditMode, ou `unity command run_tests --mode editor`.

## Documentação

| Documento | O que é |
|---|---|
| [`docs/gdd.md`](docs/gdd.md) | GDD de uma página — comece por aqui |
| [`docs/gdd/`](docs/gdd/) | GDD completo, um capítulo por arquivo (01–10) |
| [`docs/gdd.html`](docs/gdd.html) | Versão visual do GDD de uma página, com as ilustrações |
| [`docs/gdd-full.html`](docs/gdd-full.html) | Versão visual do GDD completo |
| [`docs/img/`](docs/img/) | Ilustrações das mecânicas (SVG), usadas no Markdown e no HTML |
| [`docs/source/`](docs/source/) | O GDD v0.1 original em PDF, fonte de tudo |

O Markdown é a fonte; as páginas HTML são geradas a partir dele e atualizadas no mesmo PR.
Capítulos novos começam de [`docs/gdd/_template.md`](docs/gdd/_template.md), que também guarda as convenções.

Para abrir as páginas visuais localmente:

```bash
python3 -m http.server 8765 --directory docs
```

e acesse http://localhost:8765/gdd.html.
