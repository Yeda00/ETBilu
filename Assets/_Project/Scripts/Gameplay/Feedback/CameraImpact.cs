using UnityEngine;

namespace OneMoreTime.Gameplay.Feedback
{
    /// <summary>
    /// Trauma-based camera impulse tuned for bullet hell readability. Small repeated hits stay subtle;
    /// important damage and destruction can briefly add translation, roll and a restrained FOV kick.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class CameraImpact : MonoBehaviour
    {
        [SerializeField, Min(0f)] float maxOffset = 0.34f;
        [SerializeField, Min(0f)] float maxRoll = 0.65f;
        [SerializeField, Min(1f)] float noiseFrequency = 29f;
        [SerializeField, Min(0f)] float returnSharpness = 18f;

        Camera targetCamera;
        Vector3 neutralPosition;
        Quaternion neutralRotation;
        float neutralFov;
        float trauma;
        float decayPerSecond;
        float fovKick;
        Vector2 bias;
        float seed;

        void Awake()
        {
            targetCamera = GetComponent<Camera>();
            neutralPosition = transform.localPosition;
            neutralRotation = transform.localRotation;
            neutralFov = targetCamera.fieldOfView;
            seed = Random.Range(0f, 1000f);
        }

        public void Impulse(float strength, float duration, Vector2 directionalBias = default, float fieldOfViewKick = 0f)
        {
            strength = Mathf.Clamp01(strength);
            trauma = Mathf.Max(trauma, strength);
            decayPerSecond = Mathf.Max(decayPerSecond, strength / Mathf.Max(0.02f, duration));
            bias = Vector2.ClampMagnitude(bias + directionalBias, 1f);
            fovKick = Mathf.Max(fovKick, fieldOfViewKick);
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (trauma <= 0.001f)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, neutralPosition, 1f - Mathf.Exp(-returnSharpness * dt));
                transform.localRotation = Quaternion.Slerp(transform.localRotation, neutralRotation, 1f - Mathf.Exp(-returnSharpness * dt));
                targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, neutralFov, 1f - Mathf.Exp(-returnSharpness * dt));
                trauma = 0f;
                decayPerSecond = 0f;
                bias = Vector2.zero;
                fovKick = 0f;
                return;
            }

            float power = trauma * trauma;
            float clock = Time.unscaledTime * noiseFrequency;
            float x = Mathf.PerlinNoise(seed, clock) * 2f - 1f;
            float y = Mathf.PerlinNoise(seed + 31.7f, clock) * 2f - 1f;
            Vector2 noise = new Vector2(x, y) * (maxOffset * power);
            Vector2 directed = bias * (maxOffset * power * 0.35f);
            transform.localPosition = neutralPosition + (Vector3)(noise + directed);
            transform.localRotation = neutralRotation * Quaternion.Euler(0f, 0f, x * maxRoll * power);
            targetCamera.fieldOfView = neutralFov + fovKick * power;

            trauma = Mathf.Max(0f, trauma - decayPerSecond * dt);
            bias = Vector2.Lerp(bias, Vector2.zero, 1f - Mathf.Exp(-10f * dt));
        }

        void OnDisable()
        {
            transform.localPosition = neutralPosition;
            transform.localRotation = neutralRotation;
            if (targetCamera != null)
                targetCamera.fieldOfView = neutralFov;
        }
    }
}
