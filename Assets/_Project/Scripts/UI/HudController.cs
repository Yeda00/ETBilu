using System.Globalization;
using OneMoreTime.Core.Boss;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Session;
using OneMoreTime.Gameplay.Ship;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneMoreTime.UI
{
    /// <summary>
    /// Placeholder HUD (GDD 07): integrity, temporal memory, active echoes, gadget recharge, score,
    /// the Guardian's health and vulnerability, and the session message. Layout and look live in UI/Hud.uxml and UI/Hud.uss, so the artist can restyle
    /// without touching code.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] Health player;
        [SerializeField] EchoAbility echo;
        [SerializeField] GameSession session;
        [SerializeField] ShipGadget gadget;
        [SerializeField] StageDirector stage;

        static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        VisualElement integrityFill, memoryFill, recDot, message, gadgetFill, bossPanel, bossFill;
        Label memoryValue, memoryState, score, messageTitle, messageHint, gadgetState, bossState, bossPhase;
        VisualElement[] generatorPips;
        VisualElement[] slots;
        Label[] slotStates;

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

            slots = new VisualElement[3];
            slotStates = new Label[3];
            for (int i = 0; i < 3; i++)
            {
                slots[i] = root.Q($"echo-slot-{i + 1}");
                slotStates[i] = slots[i]?.Q<Label>(className: "slot-state");
            }
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
                    ShowMessage("ONE MORE TIME", "o encontro recomeça…");
                    break;
                case SessionState.Cleared:
                    ShowMessage("GUARDIÃO DERROTADO", $"pontuação {session.Score:000000} · {session.Kills} abates");
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
