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

        InputAction move, aim, aimStick, fire, record, invokeEcho, gadget, pause;
        Vector2 lastPointer;

        public Vector2 Move => move != null ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        /// <summary>Pointer position in screen pixels. Gameplay systems project it onto their own plane.</summary>
        public Vector2 AimScreenPosition => aim != null
            ? aim.ReadValue<Vector2>()
            : new Vector2(Screen.width, Screen.height * 0.5f);
        public bool FireHeld => fire != null && fire.IsPressed();
        public bool RecordHeld => record != null && record.IsPressed();
        public bool RecordPressed => record != null && record.WasPressedThisFrame();
        public bool InvokeEchoPressed => invokeEcho != null && invokeEcho.WasPressedThisFrame();
        public bool GadgetPressed => gadget != null && gadget.WasPressedThisFrame();
        public bool PausePressed => pause != null && pause.WasPressedThisFrame();

        /// <summary>The right stick, for aiming with a gamepad. Zero without one.</summary>
        public Vector2 AimStick => aimStick != null ? aimStick.ReadValue<Vector2>() : Vector2.zero;
        /// <summary>True while the last thing the player touched was a gamepad, false after the keyboard or mouse.</summary>
        public bool UsingGamepad { get; private set; }
        /// <summary>The asset with the bindings, for the controls screen and the HUD hints.</summary>
        public InputActionAsset Controls => controls;

        void Awake()
        {
            if (controls == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} on {name} has no InputActionAsset.", this);
                enabled = false;
                return;
            }

            ControlsStore.Load(controls);   // the player's own keys, if they changed any
            var map = controls.FindActionMap(actionMap, throwIfNotFound: true);
            move = map.FindAction("Move", true);
            aim = map.FindAction("Aim", true);
            aimStick = map.FindAction("AimStick", false);
            fire = map.FindAction("Fire", true);
            record = map.FindAction("Record", true);
            invokeEcho = map.FindAction("InvokeEcho", true);
            gadget = map.FindAction("Gadget", true);
            pause = map.FindAction("Pause", true);
        }

        void Update() => TrackLastDevice();

        // Which device the player last used decides the aim (pointer or right stick) and the hints on the HUD.
        void TrackLastDevice()
        {
            var pad = Gamepad.current;
            if (pad != null && PadTouched(pad))
            {
                UsingGamepad = true;
                return;
            }

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool mouseUsed = false;
            if (mouse != null)
            {
                Vector2 pointer = mouse.position.ReadValue();
                mouseUsed = (pointer - lastPointer).sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame;
                lastPointer = pointer;
            }
            if (mouseUsed || (keyboard != null && keyboard.anyKey.wasPressedThisFrame))
                UsingGamepad = false;
        }

        static bool PadTouched(Gamepad pad)
        {
            if (pad.leftStick.ReadValue().sqrMagnitude > 0.1f || pad.rightStick.ReadValue().sqrMagnitude > 0.1f || pad.dpad.ReadValue().sqrMagnitude > 0f)
                return true;
            foreach (var control in pad.allControls)
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame)
                    return true;
            return false;
        }

        void OnEnable() => controls?.FindActionMap(actionMap)?.Enable();
        void OnDisable() => controls?.FindActionMap(actionMap)?.Disable();
    }
}
