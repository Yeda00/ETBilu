using System.Globalization;
using OneMoreTime.Core.Boss;
using OneMoreTime.Core.Controls;
using OneMoreTime.Core.Combos;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Session;
using OneMoreTime.Gameplay.Ship;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneMoreTime.UI
{
    /// <summary>
    /// Placeholder HUD (GDD 07): integrity, temporal memory, active echoes, gadget recharge, score,
    /// the Guardian's health and vulnerability, the combo notice and the session message. Layout and
    /// look live in UI/Hud.uxml and UI/Hud.uss, so the artist can restyle without touching code.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] Health player;
        [SerializeField] EchoAbility echo;
        [SerializeField] GameSession session;
        [SerializeField] ShipGadget gadget;
        [SerializeField] StageDirector stage;
        [SerializeField] ComboTracker combos;
        [Tooltip("Gives the controls for the hint line at the foot of the screen, keyboard or gamepad. Optional.")]
        [SerializeField] PlayerInputReader input;
        [Tooltip("Seconds the combo notice stays on screen.")]
        [SerializeField, Min(0.1f)] float comboNoticeSeconds = 1.4f;

        static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        VisualElement integrityFill, memoryFill, recDot, message, gadgetFill, bossPanel, bossFill, comboNotice;
        Label controlsHint;
        bool hintShown;
        ControlDevice hintDevice;
        Label memoryValue, memoryState, score, messageTitle, messageHint, gadgetState, bossState, bossPhase, comboTitle, comboHint;
        VisualElement[] generatorPips;
        VisualElement[] slots;
        Label[] slotStates;
        float comboTimer;
        ComboType comboShown;
        int comboRepeat;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            integrityFill = root.Q("integrity-fill");
            memoryFill = root.Q("memory-fill");
            recDot = root.Q("rec-dot");
            memoryValue = root.Q<Label>("memory-value");
            memoryState = root.Q<Label>("memory-state");
            score = root.Q<Label>("score");
            message = root.Q("message");
            messageTitle = root.Q<Label>("message-title");
            messageHint = root.Q<Label>("message-hint");
            gadgetFill = root.Q("gadget-fill");
            gadgetState = root.Q<Label>("gadget-state");
            bossPanel = root.Q("boss");
            bossFill = root.Q("boss-fill");
            bossState = root.Q<Label>("boss-state");
            bossPhase = root.Q<Label>("boss-phase");
            generatorPips = new[] { root.Q("generator-1"), root.Q("generator-2") };
            comboNotice = root.Q("combo");
            comboTitle = root.Q<Label>("combo-title");
            comboHint = root.Q<Label>("combo-hint");
            controlsHint = root.Q<Label>("controls-hint");
            hintShown = false;
            comboTimer = 0f;

            slots = new VisualElement[3];
            slotStates = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = root.Q($"echo-slot-{i + 1}");
                slotStates[i] = slots[i]?.Q<Label>(className: "slot-state");
            }

            if (combos != null)
                combos.Triggered += OnCombo;
        }

        void OnDisable()
        {
            if (combos != null)
                combos.Triggered -= OnCombo;
        }

        void Update()
        {
            if (integrityFill == null)
                return;

            if (player != null)
                SetFill(integrityFill, player.Normalized);

            if (echo != null)
                UpdateEcho();

            if (gadget != null && gadgetFill != null)
                UpdateGadget();

            if (bossPanel != null)
                UpdateBoss(stage != null ? stage.Boss : null);

            if (session != null)
                UpdateSession();

            UpdateControlsHint();

            if (comboTimer > 0f)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                    comboNotice.AddToClassList("hidden");
            }
        }

        // A Synchronized Strike on screen is not covered by a Crossfire; the same combo again counts up.
        void OnCombo(ComboReport combo)
        {
            if (comboNotice == null || comboTitle == null || comboHint == null)
                return;
            bool showing = comboTimer > 0f;
            if (showing && combo.Type < comboShown)
                return;
            comboRepeat = showing && combo.Type == comboShown ? comboRepeat + 1 : 1;
            comboShown = combo.Type;
            comboTimer = comboNoticeSeconds;

            string title = combo.Type == ComboType.SynchronizedStrike ? "SYNCHRONIZED STRIKE" : "CROSSFIRE";
            string hint = combo.Type == ComboType.SynchronizedStrike ? "NAVE + ECO NOS GERADORES · ESCUDO DESATIVADO" : "NAVE + ECO NO MESMO ALVO";
            comboTitle.text = comboRepeat > 1 ? $"{title} ×{comboRepeat}" : title;
            comboHint.text = combo.Points > 0 ? $"{hint} · +{combo.Points}" : hint;
            comboNotice.EnableInClassList("strike", combo.Type == ComboType.SynchronizedStrike);
            comboNotice.RemoveFromClassList("hidden");
        }

        // The hint line follows the device the player last used, with the keys as they are set now.
        void UpdateControlsHint()
        {
            if (input == null || controlsHint == null)
                return;
            var device = input.UsingGamepad ? ControlDevice.Gamepad : ControlDevice.Keyboard;
            if (hintShown && device == hintDevice)
                return;
            hintShown = true;
            hintDevice = device;
            controlsHint.text = ControlsCatalog.HintLine(input.Controls, device);
        }

        void UpdateEcho()
        {
            float max = echo.MaxRecordSeconds;
            float seconds = echo.IsRecording ? echo.RecordedSeconds : echo.HasRecordingReady ? echo.ReadyDuration : 0f;
            SetFill(memoryFill, max > 0f ? seconds / max : 0f);
            memoryValue.text = string.Format(PtBr, "{0:0.0} / {1:0.0} s", seconds, max);

            recDot.EnableInClassList("on", echo.IsRecording);
            memoryFill.EnableInClassList("ready", !echo.IsRecording && echo.HasRecordingReady);
            memoryState.text = echo.IsRecording
                ? "SHIFT · gravando sequência"
                : echo.HasRecordingReady ? "E · invocar eco" : "Segure SHIFT para gravar";

            var active = echo.ActiveEchoes;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    continue;
                bool used = i < active.Count;
                bool exists = i < echo.MaxActiveEchoes;
                slots[i].EnableInClassList("playing", used);
                slots[i].EnableInClassList("locked", !exists);
                if (slotStates[i] != null)
                    slotStates[i].text = !exists ? "—" : used ? "reproduzindo" : "disponível";
            }
        }

        void UpdateGadget()
        {
            SetFill(gadgetFill, 1f - gadget.CooldownNormalized);
            gadgetFill.EnableInClassList("charging", !gadget.IsReady);
            gadgetState.text = gadget.IsReady ? "Q · pronta" : string.Format(PtBr, "recarga {0:0.0} s", gadget.CooldownRemaining);
        }

        void UpdateBoss(GuardianBoss boss)
        {
            bossPanel.EnableInClassList("hidden", boss == null);
            if (boss == null)
                return;

            SetFill(bossFill, boss.CoreNormalized);
            bossFill.EnableInClassList("vulnerable", boss.CoreVulnerable);
            for (int i = 0; i < generatorPips.Length; i++)
                generatorPips[i]?.EnableInClassList("down", !boss.IsGeneratorUp(i));

            bossPhase.text = boss.IsEntering ? "" : boss.Phase switch
            {
                GuardianPhase.Survival => "FASE 1 · SOBREVIVÊNCIA",
                GuardianPhase.Preparation => "FASE 2 · PREPARAÇÃO",
                GuardianPhase.Synchronization => "FASE 3 · SINCRONIZAÇÃO",
                _ => "",
            };

            if (boss.IsEntering)
                bossState.text = "o Guardião se aproxima";
            else if (boss.Phase == GuardianPhase.Defeated)
                bossState.text = "núcleo destruído";
            else if (boss.CoreVulnerable)
                bossState.text = string.Format(PtBr, "NÚCLEO VULNERÁVEL · {0:0.0} s", boss.VulnerableRemaining);
            else if (boss.WindowRemaining > 0f)
                bossState.text = string.Format(PtBr, "gerador caído · janela {0:0.0} s", boss.WindowRemaining);
            else
                bossState.text = boss.Phase == GuardianPhase.Survival ? "núcleo blindado" : "núcleo protegido pelos geradores";
        }

        void UpdateSession()
        {
            score.text = session.Score.ToString("000000", PtBr);
            switch (session.State)
            {
                case SessionState.Paused:
                    ShowMessage("PAUSA", "Esc para continuar");
                    break;
                case SessionState.Defeated:
                    ShowMessage("TENTE NOVAMENTE", "o encontro recomeça…");
                    break;
                case SessionState.Cleared:
                    ShowMessage("GUARDIÃO DERROTADO", $"pontuação {session.Score:000000} · {session.Kills} abates · {session.ComboCount} combo{(session.ComboCount == 1 ? "" : "s")}");
                    break;
                default:
                    message.AddToClassList("hidden");
                    break;
            }
        }

        void ShowMessage(string title, string hint)
        {
            messageTitle.text = title;
            messageHint.text = hint;
            message.RemoveFromClassList("hidden");
        }

        static void SetFill(VisualElement fill, float normalized)
        {
            fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }
    }
}
