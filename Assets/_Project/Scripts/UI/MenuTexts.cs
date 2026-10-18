using System;
using UnityEngine;

namespace OneMoreTime.UI
{
    /// <summary>
    /// Every word of the menu screens, in one asset (Data/Menu/MenuTexts). Edit here to change what the menu
    /// says; the layout and look live in UI/Menu.uxml and UI/Menu.uss. Game text is Brazilian Portuguese.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Time/Menu Texts", fileName = "MenuTexts")]
    public sealed class MenuTexts : ScriptableObject
    {
        [Serializable]
        public struct ControlEntry
        {
            public string action;
            public string keys;
        }

        [Header("Title screen")]
        public string logoTop = "ONE MORE";
        public string logoBottom = "TIME";
        public string tagline = "O PASSADO CONTINUA LUTANDO";
        public string pressStart = "PRESSIONE ENTER";

        [Header("Main menu")]
        public string playLabel = "JOGO PRINCIPAL";
        public string controlsLabel = "CONTROLES";
        public string soundLabel = "SOM";
        public string creditsLabel = "CRÉDITOS";
        public string quitLabel = "SAIR";

        [Header("Sound screen")]
        public string soundTitle = "SOM";
        public string musicLabel = "MÚSICA";
        public string effectsLabel = "EFEITOS";

        [Header("Controls screen")]
        public string controlsTitle = "CONTROLES";
        public ControlEntry[] controls =
        {
            new() { action = "MOVER A NAVE", keys = "W A S D  ou  SETAS" },
            new() { action = "MIRAR", keys = "MOUSE" },
            new() { action = "ATIRAR", keys = "ESPAÇO  ou  BOTÃO ESQUERDO" },
            new() { action = "GRAVAR O ECO", keys = "SEGURAR SHIFT" },
            new() { action = "INVOCAR O ECO", keys = "E" },
            new() { action = "MINA TEMPORAL", keys = "Q" },
            new() { action = "PAUSAR", keys = "ESC" },
        };

        [Header("Credits screen")]
        public string creditsTitle = "CRÉDITOS";
        [TextArea(4, 14)]
        public string credits =
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
            "Todos em CC0, pelo OpenGameArt.org";

        [Header("Mission map")]
        public string missionsTitle = "MISSÕES";
        [Tooltip("{0} is the mission number.")]
        public string missionNumberFormat = "MISSÃO Nº {0}";
        public string lockedLabel = "BLOQUEADA";
        public string lockedHint = "AINDA NÃO DISPONÍVEL";

        [Header("Story screen")]
        public string introHint = "ENTER PULA  ·  ↓ ACELERA";

        [Header("Hints at the foot of each screen")]
        public string listHint = "↑ ↓ ESCOLHE  ·  ENTER CONFIRMA  ·  ESC VOLTA";
        public string soundHint = "↑ ↓ ESCOLHE  ·  ← → AJUSTA  ·  ESC VOLTA";
        public string mapHint = "← → ESCOLHE  ·  ENTER CONFIRMA  ·  ESC VOLTA";
        public string backHint = "ESC VOLTA";
    }
}
