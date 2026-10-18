using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace OneMoreTime.Gameplay.Ship
{
    /// <summary>
    /// Aims with the pointer projected onto the playfield, or with the gamepad's right stick; turns the ship visual toward it and draws a compact
    /// world-space direction indicator. Physics and the ship root stay axis-aligned on the 2D plane.
    /// </summary>
    [DefaultExecutionOrder(5)]
    public sealed class ShipAim : MonoBehaviour
    {
        [SerializeField] ShipMotor motor;
        [SerializeField, Min(0f)] float visualSharpness = 22f;
        [SerializeField, Min(0f)] float indicatorSharpness = 30f;
        [SerializeField, Min(0.1f)] float indicatorDistance = 3.2f;
        [SerializeField, Min(0.05f)] float indicatorRadius = 0.24f;
        [SerializeField] Color indicatorColor = new(0.2f, 0.95f, 1f, 1f);
        [Header("Feel")]
        [SerializeField, Range(0f, 0.1f)] float idlePulseAmount = 0.025f;
        [SerializeField, Min(0f)] float idlePulseFrequency = 2.2f;
        [SerializeField, Range(0f, 1f)] float shotPulseScale = 0.42f;
        [SerializeField, Min(0f)] float shotPulseDecay = 14f;

        const float MinAimDistance = 0.08f;
        const float StickAimThreshold = 0.3f;
        const int RingSegments = 20;

        Vector2 direction = Vector2.right;
        Vector2 indicatorDirection = Vector2.right;
        float visualAngle;
        float shotPulse;
        Transform reticle;
        LineRenderer guide;
        LineRenderer ring;
        Material indicatorMaterial;

        public Vector2 Direction => direction;
        public float Angle => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        /// <summary>Gives the reticle a short confirmation pulse without changing the actual aim.</summary>
        public void Pulse()
        {
            shotPulse = 1f;
        }

        void Awake()
        {
            visualAngle = Angle;
            BuildIndicator();
        }

        void Update()
        {
            if (motor == null)
                return;

            PlayerInputReader input = motor.Input;
            Playfield playfield = motor.Playfield;
            Camera view = playfield != null ? playfield.View : null;
            if (input == null || playfield == null || view == null)
                return;

            if (input.UsingGamepad)
            {
                // The right stick gives the direction; released, the ship keeps the last one.
                Vector2 stick = input.AimStick;
                if (stick.sqrMagnitude > StickAimThreshold * StickAimThreshold)
                    direction = stick.normalized;
            }
            else
            {
                var plane = new Plane(playfield.transform.forward, playfield.transform.position);
                var ray = view.ScreenPointToRay(input.AimScreenPosition);
                if (plane.Raycast(ray, out float enter))
                {
                    Vector2 target = playfield.WorldToLocal(ray.GetPoint(enter));
                    Vector2 delta = target - motor.LocalPosition;
                    if (delta.sqrMagnitude >= MinAimDistance * MinAimDistance)
                        direction = delta.normalized;
                }
            }

            float targetAngle = Angle;
            float blend = visualSharpness <= 0f ? 1f : 1f - Mathf.Exp(-visualSharpness * Time.deltaTime);
            visualAngle = Mathf.LerpAngle(visualAngle, targetAngle, blend);
            if (motor.Visual != null)
                motor.Visual.localRotation = Quaternion.Euler(motor.Bank, 0f, visualAngle);

            float indicatorBlend = indicatorSharpness <= 0f ? 1f : 1f - Mathf.Exp(-indicatorSharpness * Time.deltaTime);
            Vector2 smoothedDirection = Vector2.Lerp(indicatorDirection, direction, indicatorBlend);
            indicatorDirection = smoothedDirection.sqrMagnitude > 0.0001f ? smoothedDirection.normalized : direction;
            shotPulse *= Mathf.Exp(-shotPulseDecay * Time.deltaTime);
            UpdateIndicator();
        }

        void BuildIndicator()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                indicatorMaterial = new Material(shader)
                {
                    name = "Aim Indicator (Runtime)",
                    hideFlags = HideFlags.DontSave,
                };
                if (indicatorMaterial.HasProperty("_BaseColor"))
                    indicatorMaterial.SetColor("_BaseColor", indicatorColor);
                if (indicatorMaterial.HasProperty("_Color"))
                    indicatorMaterial.SetColor("_Color", indicatorColor);
            }

            guide = CreateLine("Aim Guide", 0.035f, false);
            guide.positionCount = 2;

            reticle = new GameObject("Aim Reticle").transform;
            reticle.SetParent(transform, false);
            ring = CreateLine("Ring", 0.055f, true, reticle);
            ring.positionCount = RingSegments;
            for (int i = 0; i < RingSegments; i++)
            {
                float a = i * Mathf.PI * 2f / RingSegments;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * indicatorRadius);
            }

            UpdateIndicator();
        }

        LineRenderer CreateLine(string objectName, float width, bool loop, Transform parent = null)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent != null ? parent : transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = 50;
            line.startColor = indicatorColor;
            line.endColor = indicatorColor;
            if (indicatorMaterial != null)
                line.sharedMaterial = indicatorMaterial;
            return line;
        }

        void UpdateIndicator()
        {
            if (guide != null)
            {
                float guideKick = shotPulse * 0.11f;
                guide.SetPosition(0, (Vector3)(indicatorDirection * (1.25f + guideKick)));
                guide.SetPosition(1, (Vector3)(indicatorDirection * (indicatorDistance - indicatorRadius * 1.4f + guideKick)));
                guide.startWidth = 0.035f + shotPulse * 0.025f;
                guide.endWidth = 0.035f + shotPulse * 0.012f;
            }
            if (reticle != null)
            {
                float breathe = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * idlePulseFrequency) * idlePulseAmount;
                float scale = 1f + breathe + shotPulse * shotPulseScale;
                reticle.localPosition = (Vector3)(indicatorDirection * (indicatorDistance + shotPulse * 0.11f));
                reticle.localScale = Vector3.one * scale;
            }
            if (ring != null)
            {
                ring.startWidth = 0.055f + shotPulse * 0.035f;
                ring.endWidth = ring.startWidth;
            }
        }

        void OnDestroy()
        {
            if (indicatorMaterial != null)
                Destroy(indicatorMaterial);
        }
    }
}
