using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime.Gameplay.Input
{
    /// <summary>
    /// The one place that reads the Input System. Everything else asks this component, so the
    /// bindings (GDD 07) live in a single asset: Input/OneMoreTimeControls.inputactions.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] string actionMap = "Gameplay";

        InputAction move, aim, fire, record, invokeEcho, gadget, pause;

        public Vector2 Move => move != null ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        /// <summary>Pointer position in screen pixels. Gameplay systems project it onto their own plane.</summary>
        public Vector2 AimScreenPosition => aim != null
            ? aim.ReadValue<Vector2>()
            : new Vector2(Screen.width, Screen.height * 0.5f);
        public bool FireHeld => fire != null && fire.IsPressed();
        public bool RecordHeld => record != null && record.IsPressed();
        public bool InvokeEchoPressed => invokeEcho != null && invokeEcho.WasPressedThisFrame();
        public bool GadgetPressed => gadget != null && gadget.WasPressedThisFrame();
        public bool PausePressed => pause != null && pause.WasPressedThisFrame();

        void Awake()
        {
            if (controls == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} on {name} has no InputActionAsset.", this);
                enabled = false;
                return;
            }

            var map = controls.FindActionMap(actionMap, throwIfNotFound: true);
            move = map.FindAction("Move", true);
            aim = map.FindAction("Aim", true);
            fire = map.FindAction("Fire", true);
            record = map.FindAction("Record", true);
            invokeEcho = map.FindAction("InvokeEcho", true);
            gadget = map.FindAction("Gadget", true);
            pause = map.FindAction("Pause", true);
        }

        void OnEnable() => controls?.FindActionMap(actionMap)?.Enable();
        void OnDisable() => controls?.FindActionMap(actionMap)?.Disable();
    }
}
