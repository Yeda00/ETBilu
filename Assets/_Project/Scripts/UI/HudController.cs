using OneMoreTime.Core.Boss;
using OneMoreTime.Core.Controls;
using OneMoreTime.Core.Combos;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Language;
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

        // The captions in Hud.uxml: the second class of each one, and the key of its words.
        static readonly (string cssClass, string key)[] Captions =
        {
            ("integrity", "hud.integrity"), ("memory", "hud.memory"), ("gadget", "hud.gadget"),
            ("boss", "hud.boss"), ("score", "hud.score"), ("echo", "hud.echoes"),
        };

        VisualElement integrityFill, memoryFill, recDot, message, gadgetFill, bossPanel, bossFill, comboNotice;
        Label controlsHint;
        bool hintShown;
        ControlDevice hintDevice;
        Label memoryValue, memoryState, score, messageTitle, messageHint, gadgetState, bossState, bossPhase, comboTitle, comboHint;
        VisualElement[] partPips;   // in GuardianPart order
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
            partPips = new[]
            {
                root.Q("part-antenna"), root.Q("part-eyes"), root.Q("part-cannon-1"), root.Q("part-cannon-2"),
                root.Q("part-teeth-1"), root.Q("part-teeth-2"),
            };
            comboNotice = root.Q("combo");
            comboTitle = root.Q<Label>("combo-title");
            comboHint = root.Q<Label>("combo-hint");
            controlsHint = root.Q<Label>("controls-hint");
            hintShown = false;
            comboTimer = 0f;

            foreach (var (cssClass, key) in Captions)
            {
                var caption = root.Query<Label>(className: "caption").Where(l => l.ClassListContains(cssClass)).First();
                if (caption != null)
                    caption.text = L10n.Get(key);
            }

            slots = new VisualElement[3];
            slotStates = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = root.Q($"echo-slot-{i + 1}");
                slotStates[i] = slots[i]?.Q<Label>(className: "slot-state");
                var slotName = slots[i]?.Q<Label>(className: "slot-name");
                if (slotName != null)
                    slotName.text = L10n.Format("hud.echoSlot", i + 1);
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

            bool strike = combo.Type == ComboType.SynchronizedStrike;
            string title = L10n.Get(strike ? "hud.combo.strike" : "hud.combo.crossfire");
            string hint = L10n.Get(strike ? "hud.combo.strike.hint" : "hud.combo.crossfire.hint");
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
            float seconds = echo.IsRecording ? echo.RecordedSeconds : 0f;
            float fill = echo.IsRecording
                ? (max > 0f ? seconds / max : 0f)
                : echo.CooldownRemaining > 0f ? 1f - echo.CooldownNormalized : echo.CanPlantAnchor ? 1f : 0f;
            SetFill(memoryFill, fill);
            memoryValue.text = echo.IsRecording
                ? L10n.Format("hud.memory.time", seconds, max)
                : echo.CooldownRemaining > 0f ? L10n.Format("hud.memory.cooldown", echo.CooldownRemaining) : L10n.Get("hud.memory.ready");

            recDot.EnableInClassList("on", echo.IsRecording);
            memoryFill.EnableInClassList("ready", !echo.IsRecording && echo.CanPlantAnchor);
            memoryState.text = L10n.Get(echo.IsRecording ? "hud.memory.state.recording"
                : echo.CooldownRemaining > 0f ? "hud.memory.state.recalibrating"
                : echo.CanPlantAnchor ? "hud.memory.state.idle" : "hud.memory.state.playing");

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
                    slotStates[i].text = !exists ? "—" : L10n.Get(used ? "hud.slot.playing" : "hud.slot.available");
            }
        }

        void UpdateGadget()
        {
            SetFill(gadgetFill, 1f - gadget.CooldownNormalized);
            gadgetFill.EnableInClassList("charging", !gadget.IsReady);
            gadgetState.text = gadget.IsReady ? L10n.Get("hud.gadget.ready") : L10n.Format("hud.gadget.recharge", gadget.CooldownRemaining);
        }

        void UpdateBoss(GuardianBoss boss)
        {
            bossPanel.EnableInClassList("hidden", boss == null);
            if (boss == null)
                return;

            // The bar is what is left of the teeth, the weak points; the pips are the parts and the teeth.
            SetFill(bossFill, boss.TeethNormalized);
            bool teethOpen = boss.IsPartExposed(GuardianPart.TeethUpper) || boss.IsPartExposed(GuardianPart.TeethLower);
            bossFill.EnableInClassList("vulnerable", teethOpen);
            for (int i = 0; i < partPips.Length; i++)
            {
                var part = (GuardianPart)i;
                partPips[i]?.EnableInClassList("down", boss.IsPartDown(part));
                partPips[i]?.EnableInClassList("guarded", !boss.IsPartDown(part) && !boss.IsPartExposed(part));
            }

            bossPhase.text = boss.IsEntering ? "" : boss.Phase switch
            {
                GuardianPhase.Survival => L10n.Get("hud.boss.phase1"),
                GuardianPhase.Dismantle => L10n.Get("hud.boss.phase2"),
                _ => "",
            };

            if (boss.IsEntering)
                bossState.text = L10n.Get("hud.boss.entering");
            else if (boss.Phase == GuardianPhase.Defeated)
                bossState.text = L10n.Get("hud.boss.defeated");
            else if (boss.Phase == GuardianPhase.Survival)
                bossState.text = L10n.Get("hud.boss.shielded");
            else
            {
                bool upper = boss.IsPartExposed(GuardianPart.TeethUpper), lower = boss.IsPartExposed(GuardianPart.TeethLower);
                bossState.text = L10n.Get(upper && lower ? "hud.boss.teethBoth"
                    : upper ? "hud.boss.teethUpper"
                    : lower ? "hud.boss.teethLower"
                    : "hud.boss.dismantleHint");
            }
        }

        void UpdateSession()
        {
            score.text = session.Score.ToString("000000");
            switch (session.State)
            {
                case SessionState.Paused:
                    ShowMessage(L10n.Get("hud.pause"), L10n.Get("hud.pause.hint"));
                    break;
                case SessionState.Defeated:
                    ShowMessage(L10n.Get("hud.defeat"), L10n.Get("hud.defeat.hint"));
                    break;
                case SessionState.Cleared:
                    ShowMessage(L10n.Get("hud.cleared"), L10n.Format("hud.cleared.stats", session.Score.ToString("000000"), session.Kills, session.ComboCount,
                        L10n.Get(session.ComboCount == 1 ? "hud.cleared.combo" : "hud.cleared.combos")));
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
