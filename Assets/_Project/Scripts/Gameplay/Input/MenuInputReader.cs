using UnityEngine;
using UnityEngine.InputSystem;

namespace OneMoreTime.Gameplay.Input
{
    /// <summary>
    /// Reads the menu keys from the "Menu" map of the same asset as the <see cref="PlayerInputReader"/>: these
    /// two components are the only ones that read the Input System. Directions repeat while held, so a slider
    /// can be held down. Runs before the menu code, so each property holds this frame's answer.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class MenuInputReader : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] string actionMap = "Menu";
        [SerializeField, Min(0.05f)] float repeatDelay = 0.4f;
        [SerializeField, Min(0.02f)] float repeatInterval = 0.09f;

        sealed class Repeater
        {
            public InputAction Action;
            public float Held;
            public float NextAt;
            public bool Triggered;

            public void Tick(float dt, float delay, float interval)
            {
                Triggered = false;
                if (Action.WasPressedThisFrame())
                {
                    Held = 0f;
                    NextAt = delay;
                    Triggered = true;
                }
                else if (Action.IsPressed())
                {
                    Held += dt;
                    if (Held >= NextAt)
                    {
                        NextAt += interval;
                        Triggered = true;
                    }
                }
                else
                    Held = 0f;
            }
        }

        Repeater up, down, left, right;
        InputAction submit, cancel, fast, anyKey;

        public bool Up => up != null && up.Triggered;
        public bool Down => down != null && down.Triggered;
        public bool Left => left != null && left.Triggered;
        public bool Right => right != null && right.Triggered;
        public bool SubmitPressed => submit != null && submit.WasPressedThisFrame();
        public bool CancelPressed => cancel != null && cancel.WasPressedThisFrame();
        public bool AnyKeyPressed => anyKey != null && anyKey.WasPressedThisFrame();
        public bool FastHeld => fast != null && fast.IsPressed();

        void Awake()
        {
            if (controls == null)
            {
                Debug.LogError($"{nameof(MenuInputReader)} on {name} has no InputActionAsset.", this);
                enabled = false;
                return;
            }

            var map = controls.FindActionMap(actionMap, throwIfNotFound: true);
            up = new Repeater { Action = map.FindAction("Up", true) };
            down = new Repeater { Action = map.FindAction("Down", true) };
            left = new Repeater { Action = map.FindAction("Left", true) };
            right = new Repeater { Action = map.FindAction("Right", true) };
            submit = map.FindAction("Submit", true);
            cancel = map.FindAction("Cancel", true);
            fast = map.FindAction("Fast", true);
            anyKey = map.FindAction("AnyKey", true);
        }

        void OnEnable() => controls?.FindActionMap(actionMap)?.Enable();
        void OnDisable() => controls?.FindActionMap(actionMap)?.Disable();

        void Update()
        {
            // Unscaled time: menus must keep working if the time scale is ever zero.
            float dt = Time.unscaledDeltaTime;
            up?.Tick(dt, repeatDelay, repeatInterval);
            down?.Tick(dt, repeatDelay, repeatInterval);
            left?.Tick(dt, repeatDelay, repeatInterval);
            right?.Tick(dt, repeatDelay, repeatInterval);
        }
    }
}
