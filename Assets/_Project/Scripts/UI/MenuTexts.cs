using System;
using UnityEngine;

namespace OneMoreTime.UI
{
    /// <summary>
    /// The menu words before localization (Brazilian Portuguese only). Nothing reads this any more: the words
    /// live in the Game string table (Assets/_Project/Localization), in English and Portuguese. Kept only until
    /// the owner confirms the asset can go; do not edit it.
    /// </summary>
    [CreateAssetMenu(menuName = "One More Time/Menu Texts", fileName = "MenuTexts")]
    public sealed class MenuTexts : ScriptableObject
    {
        [Serializable]
        public struct ControlLabel
        {
            [Tooltip("Key of the line of the controls screen, like Move.up or Fire.")]
            public string key;
            public string label;
        }

        [Header("Title screen")]
        public string tagline = "O PASSADO CONTINUA LUTANDO";
        public string pressStart = "PRESSIONE ENTER / START";

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
        public string keyboardHeader = "TECLADO";
        public string gamepadHeader = "CONTROLE";
        public string resetLabel = "RESTAURAR PADRÃO";
        public string waitingLabel = "PRESSIONE…";
        public ControlLabel[] controlLabels =
        {
            new() { key = "Move.up", label = "MOVER PARA CIMA" },
            new() { key = "Move.down", label = "MOVER PARA BAIXO" },
            new() { key = "Move.left", label = "MOVER PARA A ESQUERDA" },
            new() { key = "Move.right", label = "MOVER PARA A DIREITA" },
            new() { key = "Aim", label = "MIRAR" },
            new() { key = "Fire", label = "ATIRAR" },
            new() { key = "Record", label = "GRAVAR ECO (SEGURAR)" },
            new() { key = "Gadget", label = "MINA TEMPORAL" },
            new() { key = "Dodge", label = "ESQUIVA" },
            new() { key = "Pause", label = "PAUSAR" },
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
        public string introHint = "ENTER OU A PULA  ·  ↓ ACELERA";

        [Header("Hints at the foot of each screen")]
        public string listHint = "↑ ↓ ESCOLHE  ·  ENTER OU A CONFIRMA  ·  ESC OU B VOLTA";
        public string soundHint = "↑ ↓ ESCOLHE  ·  ← → AJUSTA  ·  ESC OU B VOLTA";
        public string mapHint = "← → ESCOLHE  ·  ENTER OU A CONFIRMA  ·  ESC OU B VOLTA";
        public string backHint = "ESC OU B VOLTA";
        public string controlsHint = "↑ ↓ ← → ESCOLHE  ·  ENTER OU A MUDA  ·  ESC OU B VOLTA";
        public string waitingHint = "APERTE O NOVO BOTÃO  ·  ESC CANCELA";
        public string fixedControlHint = "ESTE CONTROLE NÃO MUDA";

        public string LabelFor(string key)
        {
            if (controlLabels != null)
                foreach (var entry in controlLabels)
                    if (entry.key == key)
                        return entry.label;
            return key;
        }
    }
}
