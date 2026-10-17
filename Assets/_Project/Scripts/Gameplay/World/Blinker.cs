using UnityEngine;

namespace OneMoreTime.Gameplay.World
{
    /// <summary>Blinks its renderers — used by the wave warning at the screen edge (GDD 04).</summary>
    public sealed class Blinker : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float blinksPerSecond = 6f;

        Renderer[] renderers;
        float t;

        void Awake() => renderers = GetComponentsInChildren<Renderer>();

        void Update()
        {
            t += Time.deltaTime;
            bool on = Mathf.Repeat(t * blinksPerSecond, 1f) < 0.6f;
            foreach (var r in renderers)
                r.enabled = on;
        }
    }
}
