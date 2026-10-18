using UnityEngine;

namespace OneMoreTime.UI
{
    /// <summary>
    /// The ship shown behind the title and the main menu: it bobs and sways slowly, and slides to the right
    /// when the menu buttons take the left side of the screen.
    /// </summary>
    public sealed class ShowcaseSpin : MonoBehaviour
    {
        [SerializeField] Vector3 titlePosition = new(0f, -1.15f, 0f);
        [SerializeField] Vector3 menuPosition = new(3.0f, 0f, 0f);
        [SerializeField, Min(0f)] float bobHeight = 0.18f;
        [SerializeField, Min(0f)] float bobSpeed = 1.1f;
        [SerializeField, Min(0f)] float swayDegrees = 24f;
        [SerializeField, Min(0f)] float swaySpeed = 0.55f;
        [Tooltip("Base rotation of the model, so it faces the camera at an angle.")]
        [SerializeField] Vector3 baseEuler = new(8f, -35f, 0f);
        [SerializeField, Min(0.1f)] float slideSeconds = 0.5f;

        bool menuPose;
        Vector3 velocity;

        public void SetMenuPose(bool menu) => menuPose = menu;

        void Update()
        {
            float t = Time.unscaledTime;
            Vector3 target = menuPose ? menuPosition : titlePosition;
            Vector3 basePosition = Vector3.SmoothDamp(transform.localPosition - Bob(t), target, ref velocity, slideSeconds, Mathf.Infinity, Time.unscaledDeltaTime);
            transform.localPosition = basePosition + Bob(t);
            transform.localRotation = Quaternion.Euler(baseEuler + new Vector3(0f, Mathf.Sin(t * swaySpeed) * swayDegrees, Mathf.Sin(t * swaySpeed * 0.7f) * 4f));
        }

        Vector3 Bob(float t) => new(0f, Mathf.Sin(t * bobSpeed) * bobHeight, 0f);
    }
}
