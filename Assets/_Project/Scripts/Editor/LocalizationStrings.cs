using System.Collections.Generic;
using OneMoreTime.Core.Controls;

namespace OneMoreTime.EditorTools
{
    /// <summary>
    /// Every text of the game in each language, as the Build Localization menu writes it into the Game string table.
    /// The table (Assets/_Project/Localization) is the source of truth once it exists: the menu only adds what is
    /// missing and never overwrites, so edit the table in the Localization Tables window and use this file to seed
    /// a new key together with its code. Game text is English by default; Portuguese is the second language.
    /// </summary>
    public static class LocalizationStrings
    {
        public readonly struct Entry
        {
            public readonly string Key, English, Portuguese, Context;

            public Entry(string key, string english, string portuguese, string context = "")
            {
                Key = key;
                English = english;
                Portuguese = portuguese;
                Context = context;
            }
        }

        const string Hint = "Hint line at the foot of a screen. Arrows and ENTER / ESC / A / B are the keys of the player.";

        public static IEnumerable<Entry> All()
        {
            // ---- title screen and main menu
            yield return new("menu.logoTop", "BILU'S", "BILU'S", "First line of the logo. The name of the game; not translated.");
            yield return new("menu.logoBottom", "INVASION", "INVASION", "Second line of the logo. The name of the game; not translated.");
            yield return new("menu.tagline", "THE PAST KEEPS FIGHTING", "O PASSADO CONTINUA LUTANDO", "Slogan under the logo.");
            yield return new("menu.pressStart", "PRESS ENTER / START", "PRESSIONE ENTER / START", "Blinks on the title screen.");
            yield return new("menu.play", "MAIN GAME", "JOGO PRINCIPAL", "Main menu button: opens the mission map.");
            yield return new("menu.controls", "CONTROLS", "CONTROLES", "Main menu button.");
            yield return new("menu.sound", "SOUND", "SOM", "Main menu button.");
            yield return new("menu.language", "LANGUAGE", "IDIOMA", "Main menu button: opens the language screen.");
            yield return new("menu.credits", "CREDITS", "CRÉDITOS", "Main menu button.");
            yield return new("menu.quit", "QUIT", "SAIR", "Main menu button: closes the game.");

            // ---- sound
            yield return new("menu.sound.title", "SOUND", "SOM", "Title of the sound screen.");
            yield return new("menu.sound.music", "MUSIC", "MÚSICA", "Volume slider.");
            yield return new("menu.sound.effects", "EFFECTS", "EFEITOS", "Volume slider of the sound effects.");

            // ---- controls
            yield return new("menu.controls.title", "CONTROLS", "CONTROLES", "Title of the controls screen.");
            yield return new("menu.controls.keyboard", "KEYBOARD", "TECLADO", "Column header.");
            yield return new("menu.controls.gamepad", "GAMEPAD", "CONTROLE", "Column header.");
            yield return new("menu.controls.reset", "RESET TO DEFAULT", "RESTAURAR PADRÃO", "Button that puts every key and button back.");
            yield return new("menu.controls.waiting", "PRESS…", "PRESSIONE…", "Shown in the cell while it waits for the new key.");
            yield return new("control.Move.up", "MOVE UP", "MOVER PARA CIMA", "Line of the controls screen.");
            yield return new("control.Move.down", "MOVE DOWN", "MOVER PARA BAIXO", "Line of the controls screen.");
            yield return new("control.Move.left", "MOVE LEFT", "MOVER PARA A ESQUERDA", "Line of the controls screen.");
            yield return new("control.Move.right", "MOVE RIGHT", "MOVER PARA A DIREITA", "Line of the controls screen.");
            yield return new("control.Aim", "AIM", "MIRAR", "Line of the controls screen.");
            yield return new("control.Fire", "FIRE", "ATIRAR", "Line of the controls screen.");
            yield return new("control.Record", "RECORD ECHO (HOLD)", "GRAVAR ECO (SEGURAR)", "Line of the controls screen. The echo is the time-ghost of the ship.");
            yield return new("control.Gadget", "TEMPORAL MINE", "MINA TEMPORAL", "Line of the controls screen. The gadget of the ship.");
            yield return new("control.Dodge", "DODGE", "ESQUIVA", "Line of the controls screen.");
            yield return new("control.Pause", "PAUSE", "PAUSAR", "Line of the controls screen.");

            // ---- key and button names (BindingLabels.English is the English text)
            foreach (var pair in BindingLabels.English)
                yield return new(pair.Key, pair.Value, PortugueseBinding(pair.Key), "Name of a key, a button or a note beside it on the controls screen. Keep it short.");

            // ---- credits
            yield return new("menu.credits.title", "CREDITS", "CRÉDITOS", "Title of the credits screen.");
            yield return new("menu.credits.body",
                "DEVELOPMENT TEAM\n\n" +
                "JOÃO DUMINGUZ\n" +
                "Art Director · 3D Artist · Game Designer\n" +
                "Visual direction, 3D models and co-design of the game.\n\n" +
                "LUCAS GONÇALVES\n" +
                "Developer · Game Designer\n" +
                "Programming, gameplay systems and co-design of the game.\n\n\n" +
                "MUSIC\n" +
                "NES Shooter Music · SketchyLogic\n" +
                "8-bit Epic Space Shooter · HydroGene\n" +
                "Pulsar · celestialghost8\n" +
                "4 Chiptunes (Adventure) · Juhani Junkala\n\n" +
                "SOUND EFFECTS\n" +
                "8-Bit Sound Effect Pack · Deva (@Shades)\n" +
                "8 bit Mini pack · phoenix1291\n" +
                "Sci-Fi Sounds · Kenney\n" +
                "Reversing Time · isaiah658\n" +
                "8bit Menu · Fupi\n\n" +
                "All CC0, from OpenGameArt.org",
                "EQUIPE DE DESENVOLVIMENTO\n\n" +
                "JOÃO DUMINGUZ\n" +
                "Diretor de Arte · Artista 3D · Game Designer\n" +
                "Direção visual, modelos 3D e co-criação do game design.\n\n" +
                "LUCAS GONÇALVES\n" +
                "Desenvolvedor · Game Designer\n" +
                "Programação, sistemas de gameplay e co-criação do game design.\n\n\n" +
                "MÚSICA\n" +
                "NES Shooter Music · SketchyLogic\n" +
                "8-bit Epic Space Shooter · HydroGene\n" +
                "Pulsar · celestialghost8\n" +
                "4 Chiptunes (Adventure) · Juhani Junkala\n\n" +
                "EFEITOS SONOROS\n" +
                "8-Bit Sound Effect Pack · Deva (@Shades)\n" +
                "8 bit Mini pack · phoenix1291\n" +
                "Sci-Fi Sounds · Kenney\n" +
                "Reversing Time · isaiah658\n" +
                "8bit Menu · Fupi\n\n" +
                "Todos em CC0, pelo OpenGameArt.org",
                "Names of artists and packs stay as they are; only the headings and the last line are translated.");

            // ---- language screen and mission map
            yield return new("menu.language.title", "LANGUAGE", "IDIOMA", "Title of the language screen. Each language is listed by its own name, which is not in this table.");
            yield return new("menu.missions.title", "MISSIONS", "MISSÕES", "Title of the mission map.");
            yield return new("menu.mission.number", "MISSION NO. {0}", "MISSÃO Nº {0}", "{0} = number of the mission. Also the first line of the mission card.");
            yield return new("menu.locked", "LOCKED", "BLOQUEADA", "A mission slot that does not exist yet.");
            yield return new("menu.locked.hint", "NOT AVAILABLE YET", "AINDA NÃO DISPONÍVEL", "Under LOCKED.");

            // ---- hints
            yield return new("menu.hint.list", "↑ ↓ SELECT  ·  ENTER OR A CONFIRMS  ·  ESC OR B BACK", "↑ ↓ ESCOLHE  ·  ENTER OU A CONFIRMA  ·  ESC OU B VOLTA", Hint);
            yield return new("menu.hint.sound", "↑ ↓ SELECT  ·  ← → ADJUST  ·  ESC OR B BACK", "↑ ↓ ESCOLHE  ·  ← → AJUSTA  ·  ESC OU B VOLTA", Hint);
            yield return new("menu.hint.map", "← → SELECT  ·  ENTER OR A CONFIRMS  ·  ESC OR B BACK", "← → ESCOLHE  ·  ENTER OU A CONFIRMA  ·  ESC OU B VOLTA", Hint);
            yield return new("menu.hint.back", "ESC OR B BACK", "ESC OU B VOLTA", Hint);
            yield return new("menu.hint.controls", "↑ ↓ ← → SELECT  ·  ENTER OR A CHANGES  ·  ESC OR B BACK", "↑ ↓ ← → ESCOLHE  ·  ENTER OU A MUDA  ·  ESC OU B VOLTA", Hint);
            yield return new("menu.hint.waiting", "PRESS THE NEW BUTTON  ·  ESC CANCELS", "APERTE O NOVO BOTÃO  ·  ESC CANCELA", Hint);
            yield return new("menu.hint.fixed", "THIS CONTROL CAN'T BE CHANGED", "ESTE CONTROLE NÃO MUDA", "Shown when the player tries to change a fixed control (the mouse aim, for instance).");
            yield return new("menu.hint.intro", "ENTER OR A SKIPS  ·  ↓ SPEEDS UP", "ENTER OU A PULA  ·  ↓ ACELERA", "Hint under the scrolling story.");

            // ---- HUD
            yield return new("hud.integrity", "INTEGRITY", "INTEGRIDADE", "HUD caption: the health bar of the ship.");
            yield return new("hud.memory", "TEMPORAL MEMORY", "MEMÓRIA TEMPORAL", "HUD caption: how long the ship can record the echo.");
            yield return new("hud.gadget", "GADGET · TEMPORAL MINE", "GADGET · MINA TEMPORAL", "HUD caption.");
            yield return new("hud.boss", "THE CYCLE'S GUARDIAN", "O GUARDIÃO DO CICLO", "HUD caption: name of the boss (provisional).");
            yield return new("hud.score", "SCORE", "PONTUAÇÃO", "HUD caption.");
            yield return new("hud.echoes", "ACTIVE ECHOES", "ECOS ATIVOS", "HUD caption: the list of echo slots.");
            yield return new("hud.echoSlot", "Echo {0:00}", "Eco {0:00}", "{0} = slot number, with two digits.");
            yield return new("hud.memory.time", "{0:0.0} / {1:0.0} s", "{0:0.0} / {1:0.0} s", "{0} = seconds recorded, {1} = maximum, in seconds.");
            yield return new("hud.memory.cooldown", "{0:0.0} s", "{0:0.0} s", "{0} = seconds until the anchor can be planted again.");
            yield return new("hud.memory.ready", "READY", "PRONTA", "The echo can be recorded.");
            yield return new("hud.memory.state.recording", "release SHIFT · return to anchor", "solte SHIFT · retornar à âncora", "Recording. SHIFT is the default key; the anchor is the point the ship returns to.");
            yield return new("hud.memory.state.recalibrating", "recalibrating anchor", "recalibrando âncora", "The anchor is on cooldown.");
            yield return new("hud.memory.state.idle", "hold SHIFT · record echo", "segure SHIFT · gravar eco", "SHIFT is the default key.");
            yield return new("hud.memory.state.playing", "echo replaying", "eco reproduzindo", "An echo is playing and no anchor is available.");
            yield return new("hud.slot.playing", "replaying route", "reproduzindo rota", "State of an echo slot in use. Short: it sits in a small box.");
            yield return new("hud.slot.available", "anchor available", "âncora disponível", "State of a free echo slot. Short: it sits in a small box.");
            yield return new("hud.gadget.ready", "Q · ready", "Q · pronta", "Q is the default key.");
            yield return new("hud.gadget.recharge", "recharge {0:0.0} s", "recarga {0:0.0} s", "{0} = seconds left.");
            yield return new("hud.boss.phase1", "PHASE 1 · SURVIVAL", "FASE 1 · SOBREVIVÊNCIA", "Boss phase.");
            yield return new("hud.boss.phase2", "PHASE 2 · DISMANTLE", "FASE 2 · DESMONTE", "Boss phase: the player takes the boss apart piece by piece.");
            yield return new("hud.boss.entering", "the Guardian approaches", "o Guardião se aproxima", "Boss state.");
            yield return new("hud.boss.defeated", "Guardian destroyed", "Guardião destruído", "Boss state.");
            yield return new("hud.boss.shielded", "everything shielded", "tudo blindado", "Boss state in phase 1: nothing can be hurt yet.");
            yield return new("hud.boss.teethBoth", "UPPER AND LOWER TEETH EXPOSED", "DENTES DE CIMA E DE BAIXO EXPOSTOS", "Boss state: the weak points are open.");
            yield return new("hud.boss.teethUpper", "UPPER TEETH EXPOSED", "DENTES DE CIMA EXPOSTOS", "Boss state: the weak points are open.");
            yield return new("hud.boss.teethLower", "LOWER TEETH EXPOSED", "DENTES DE BAIXO EXPOSTOS", "Boss state: the weak points are open.");
            yield return new("hud.boss.dismantleHint", "eyes open the upper teeth · cannons, the lower ones", "olhos abrem os dentes de cima · canhões, os de baixo", "Boss state: tells which part opens which teeth.");
            yield return new("hud.combo.crossfire", "CROSSFIRE", "CROSSFIRE", "Name of a combo; kept in English.");
            yield return new("hud.combo.crossfire.hint", "SHIP + ECHO ON THE SAME TARGET", "NAVE + ECO NO MESMO ALVO", "How the Crossfire combo is made.");
            yield return new("hud.combo.strike", "SYNCHRONIZED STRIKE", "SYNCHRONIZED STRIKE", "Name of a combo; kept in English.");
            yield return new("hud.combo.strike.hint", "SHIP + ECHO ON THE CANNONS", "NAVE + ECO NOS CANHÕES", "How the Synchronized Strike combo is made.");
            yield return new("hud.pause", "PAUSE", "PAUSA", "Title of the pause message.");
            yield return new("hud.pause.hint", "Esc to resume", "Esc para continuar", "Under PAUSE.");
            yield return new("hud.defeat", "TRY AGAIN", "TENTE NOVAMENTE", "The ship was destroyed.");
            yield return new("hud.defeat.hint", "the encounter restarts…", "o encontro recomeça…", "Under TRY AGAIN.");
            yield return new("hud.cleared.thanks", "THANK YOU FOR PLAYING!", "OBRIGADO POR JOGAR!", "Main title of the end-of-game victory card.");
            yield return new("hud.cleared", "GUARDIAN DEFEATED", "GUARDIÃO DERROTADO", "The boss fell: end of the stage.");
            yield return new("hud.cleared.stats", "score {0} · {1} kills · {2} {3}", "pontuação {0} · {1} abates · {2} {3}", "{0} = score, {1} = enemies destroyed, {2} = number of combos, {3} = the word 'combo' or 'combos' (hud.cleared.combo / .combos).");
            yield return new("hud.cleared.return", "Returning to main menu in {0}s", "Voltando ao menu principal em {0}s", "Countdown on the end-of-game victory card. {0} = seconds remaining.");
            yield return new("hud.cleared.combo", "combo", "combo", "{3} of hud.cleared.stats when there is one combo.");
            yield return new("hud.cleared.combos", "combos", "combos", "{3} of hud.cleared.stats when there are zero or several combos.");
            yield return new("hud.hint.keyboard",
                "{0} move · {1} aim · {2}/{3} shoot · {4} hold: record · {5} dodge · {6} mine · {7} pause",
                "{0} mover · {1} mirar · {2}/{3} atirar · {4} segurar: gravar · {5} esquiva · {6} mina · {7} pausar",
                "Hint line at the foot of the HUD with a keyboard. Each {n} is a key as the player set it: 0 move keys, 1 the word MOUSE, 2 fire key, 3 the word CLICK, 4 record, 5 dodge, 6 mine, 7 pause.");
            yield return new("hud.hint.gamepad",
                "{0} move · {1} aim · {2} shoot · {3} hold: record · {4} dodge · {5} mine · {6} pause",
                "{0} mover · {1} mirar · {2} atirar · {3} segurar: gravar · {4} esquiva · {5} mina · {6} pausar",
                "Hint line at the foot of the HUD with a gamepad. Each {n} is a button: 0 left stick, 1 right stick, 2 fire, 3 record, 4 dodge, 5 mine, 6 pause.");

            // ---- mission 01
            yield return new("mission.01.place", "Earth · Dry Backlands", "Terra · Sertão Seco", "Mission card and map: the place, under the mission number.");
            yield return new("mission.01.title", "Overthrow the Empire", "Derrubar o Império", "Mission card and map: the name of the mission.");
            yield return new("mission.01.story.1",
                "October 2010. Bilu visited Earth on a peace mission, bringing words of wisdom that were meant to change the planet's fate forever.",
                "Outubro de 2010. Bilu visitou a Terra numa missão de paz, trazendo palavras sábias que deveriam mudar o destino do planeta para sempre.",
                "Story crawl, paragraph 1. Bilu is the hero, an alien; 2010 is the year of his first visit.");
            yield return new("mission.01.story.2",
                "42 years later, driven by a mix of rage, disappointment and revolt, Bilu returns to Earth. But this time peace is no longer an option.",
                "42 anos depois, tomado por uma mistura de ira, decepção e revolta, Bilu volta à Terra. Mas desta vez a paz não é mais uma opção.",
                "Story crawl, paragraph 2.");
            yield return new("mission.01.story.3",
                "Consumed by tyranny and totalitarianism, Earth was squeezed down to the last drop. After a series of climate catastrophes, the sea dried up, and the planet became an arid, decaying desert.",
                "Consumida pela tirania e pelo totalitarismo, a Terra foi espremida até a última gota. Depois de uma série de catástrofes climáticas, o mar secou, e o planeta virou um deserto árido e decadente.",
                "Story crawl, paragraph 3.");
            yield return new("mission.01.story.4",
                "The resources that were left were looted and sent on a desperate mission to the long-dreamed-of colony on Mars. Which, ironically, went wrong: it seems they needed university professors after all...",
                "Os recursos que restavam foram saqueados e levados numa missão desesperada para a tão sonhada colônia em Marte. Que, ironicamente, deu errado: pelo visto, eles precisavam de professores universitários, afinal...",
                "Story crawl, paragraph 4.");
            yield return new("mission.01.story.5",
                "And the so-called 1% drifted off into infinity, on a luxury cruise with a destination as certain as the Titanic's.",
                "E o tal 1% saiu à deriva pelo infinito, num cruzeiro de luxo com um destino tão certo quanto o do Titanic.",
                "Story crawl, paragraph 5.");
            yield return new("mission.01.story.6",
                "Earth became a joke with no punchline: the ruins of a decaying empire, defended by the gears of a system with no boss that no longer knows how to stop.",
                "A Terra virou uma piada sem graça: as ruínas de um império decadente, defendido pelas engrenagens de um sistema sem patrão que já não sabe como parar.",
                "Story crawl, paragraph 6.");
            yield return new("mission.01.story.7",
                "Bilu returns to deliver the message again. And this time it is going down their throats.",
                "Bilu volta para entregar a mensagem de novo. E desta vez ela vai descer goela abaixo.",
                "Story crawl, paragraph 7.");
            yield return new("mission.01.story.8",
                "Once was not enough. So, one more time:",
                "Uma vez não bastou. Então, mais uma vez:",
                "Story crawl, paragraph 8. 'One more time' is the old name of the game.");
            yield return new("mission.01.story.9",
                "SEEK KNOWLEDGE!!!!",
                "BUSQUEM CONHECIMENTO!!!!",
                "Story crawl, last line, shouted. It is the message Bilu delivers: the pt text is BUSQUEM CONHECIMENTO!!!!");
        }

        static string PortugueseBinding(string key) => key switch
        {
            "bind.space" => "ESPAÇO",
            "bind.numpadEnter" => "ENTER (NUM)",
            "bind.num" => "NUM",
            "bind.mouse" => "MOUSE",
            "bind.mouseLeft" => "CLIQUE ESQ.",
            "bind.mouseRight" => "CLIQUE DIR.",
            "bind.mouseMiddle" => "CLIQUE MEIO",
            "bind.stickLeft" => "STICK ESQ.",
            "bind.stickRight" => "STICK DIR.",
            "bind.click" => "CLIQUE",
            "bind.plusClick" => "+ CLIQUE",
            "bind.plusClickRight" => "+ CLIQUE DIR.",
            _ => BindingLabels.English[key],
        };

        /// <summary>The keys of mission 1's story, in order.</summary>
        public static readonly string[] Mission01Story =
        {
            "mission.01.story.1", "mission.01.story.2", "mission.01.story.3", "mission.01.story.4", "mission.01.story.5",
            "mission.01.story.6", "mission.01.story.7", "mission.01.story.8", "mission.01.story.9",
        };
    }
}
