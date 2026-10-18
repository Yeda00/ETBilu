using UnityEngine;
using UnityEngine.Rendering;

namespace OneMoreTime.Gameplay.Ship
{
    /// <summary>
    /// Lightweight, reusable fire feedback for both the player and echoes. It keeps the collider/root
    /// untouched, recoils only the visual pivot and reuses two runtime line renderers for the flash.
    /// </summary>
    [DefaultExecutionOrder(20)]
    public sealed class ShipFireFeedback : MonoBehaviour
    {
        [SerializeField] ShipWeapon weapon;
        [SerializeField] Transform visual;
        [Header("Recoil")]
        [SerializeField, Min(0f)] float recoilDistance = 0.14f;
        [SerializeField, Min(0.01f)] float recoilReturnTime = 0.075f;
        [SerializeField, Min(0f)] float maxRecoilDistance = 0.24f;
        [Header("Muzzle flash")]
        [SerializeField, Min(0.01f)] float flashDuration = 0.065f;
        [SerializeField, Min(0f)] float flashLength = 0.8f;
        [SerializeField] Color flashColor = new(0.35f, 0.95f, 1f, 1f);

        Vector3 neutralVisualPosition;
        Vector2 recoilOffset;
        Vector2 recoilVelocity;
        Vector2 flashOrigin;
        Vector2 flashDirection = Vector2.right;
        float flashRemaining;
        LineRenderer flashHalo;
        LineRenderer flashCore;
        Material flashMaterial;

        void Awake()
        {
            if (weapon == null)
                weapon = GetComponent<ShipWeapon>();
            if (visual == null)
                visual = GetComponent<ShipMotor>()?.Visual;
            if (visual == null)
                visual = transform.Find("Visual");
            if (visual != null)
                neutralVisualPosition = visual.localPosition;
            BuildFlash();
        }

        void OnEnable()
        {
            if (weapon != null)
                weapon.Fired += OnWeaponFired;
        }

        void OnDisable()
        {
            if (weapon != null)
                weapon.Fired -= OnWeaponFired;
            if (visual != null)
                visual.localPosition = neutralVisualPosition;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            recoilOffset = Vector2.SmoothDamp(
                recoilOffset,
                Vector2.zero,
                ref recoilVelocity,
                recoilReturnTime,
                Mathf.Infinity,
                dt);
            if (visual != null)
                visual.localPosition = neutralVisualPosition + (Vector3)recoilOffset;

            if (flashRemaining <= 0f)
                return;

            flashRemaining = Mathf.Max(0f, flashRemaining - dt);
            float life = flashDuration > 0f ? flashRemaining / flashDuration : 0f;
            UpdateFlash(life);
            if (flashRemaining <= 0f)
                SetFlashVisible(false);
        }

        void OnWeaponFired(Vector2 origin, Vector2 direction)
        {
            Play(origin, direction);
        }

        /// <summary>Plays one shot response. Origin is in playfield-local coordinates.</summary>
        public void Play(Vector2 origin, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
                direction = Vector2.right;
            direction.Normalize();

            recoilOffset -= direction * recoilDistance;
            recoilOffset = Vector2.ClampMagnitude(recoilOffset, maxRecoilDistance);
            recoilVelocity -= direction * (recoilDistance / Mathf.Max(0.01f, recoilReturnTime));

            flashOrigin = origin - (Vector2)transform.localPosition;
            flashDirection = direction;
            flashRemaining = flashDuration;
            SetFlashVisible(true);
            UpdateFlash(1f);
        }

        void BuildFlash()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                flashMaterial = new Material(shader)
                {
                    name = "Ship Muzzle Flash (Runtime)",
                    hideFlags = HideFlags.DontSave,
                };
                if (flashMaterial.HasProperty("_BaseColor"))
                    flashMaterial.SetColor("_BaseColor", Color.white);
                if (flashMaterial.HasProperty("_Color"))
                    flashMaterial.SetColor("_Color", Color.white);
            }

            flashHalo = CreateFlashLine("Muzzle Flash Halo", 0.28f, flashColor * new Color(1f, 1f, 1f, 0.45f));
            flashCore = CreateFlashLine("Muzzle Flash Core", 0.11f, Color.white);
            SetFlashVisible(false);
        }

        LineRenderer CreateFlashLine(string objectName, float width, Color color)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = 0f;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sortingOrder = 60;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
            if (flashMaterial != null)
                line.sharedMaterial = flashMaterial;
            return line;
        }

        void UpdateFlash(float life)
        {
            float eased = 1f - (1f - life) * (1f - life);
            float length = flashLength * (0.55f + 0.45f * eased);
            Vector2 tip = flashOrigin + flashDirection * length;
            flashHalo.SetPosition(0, flashOrigin);
            flashHalo.SetPosition(1, tip);
            flashCore.SetPosition(0, flashOrigin);
            flashCore.SetPosition(1, tip);
            flashHalo.widthMultiplier = eased;
            flashCore.widthMultiplier = eased;
        }

        void SetFlashVisible(bool visible)
        {
            if (flashHalo != null)
                flashHalo.enabled = visible;
            if (flashCore != null)
                flashCore.enabled = visible;
        }

        void OnDestroy()
        {
            if (flashMaterial != null)
                Destroy(flashMaterial);
        }
    }
}
