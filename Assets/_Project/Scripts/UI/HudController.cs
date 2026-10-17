using System.Globalization;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace OneMoreTime.UI
{
    /// <summary>
    /// Placeholder HUD (GDD 07): integrity, temporal memory, active echoes, score and the session
    /// message. Layout and look live in UI/Hud.uxml and UI/Hud.uss, so the artist can restyle
    /// without touching code.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] Health player;
        [SerializeField] EchoAbility echo;
        [SerializeField] GameSession session;

        static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        VisualElement integrityFill, memoryFill, recDot, message;
        Label memoryValue, memoryState, score, messageTitle, messageHint;
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
                    ShowMessage("ENCONTRO CONCLUÍDO", $"pontuação {session.Score:000000} · {session.Kills} abates");
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
