using UnityEngine;
using UnityEngine.Rendering;

namespace OneMoreTime.Gameplay.Feedback
{
    /// <summary>Small allocation-free pool for combat sparks and expanding impact rings.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatVfxPool : MonoBehaviour
    {
        [SerializeField, Range(8, 48)] int burstPoolSize = 20;
        [SerializeField, Range(4, 32)] int ringPoolSize = 16;

        const int RingSegments = 28;

        sealed class RingSlot
        {
            public LineRenderer Line;
            public readonly Vector3[] Points = new Vector3[RingSegments];
            public Vector3 Center;
            public Color Color;
            public float Age;
            public float Duration;
            public float Radius;
            public float Width;
            public bool Active;
        }

        ParticleSystem[] bursts;
        RingSlot[] rings;
        Material particleMaterial;
        Material lineMaterial;
        int nextBurst;
        int nextRing;

        void Awake()
        {
            BuildMaterials();
            BuildBursts();
            BuildRings();
        }

        public void Burst(Vector3 position, Color color, float scale, int count)
        {
            if (bursts == null || bursts.Length == 0)
                return;
            ParticleSystem particles = bursts[nextBurst++ % bursts.Length];
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.transform.position = position;

            var main = particles.main;
            main.startColor = color;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f + scale * 0.05f, 0.28f + scale * 0.12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * scale, 4.8f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f * scale, 0.14f * scale);
            var shape = particles.shape;
            shape.radius = 0.06f * scale;
            particles.Emit(Mathf.Clamp(count, 1, 64));
        }

        public void Ring(Vector3 position, Color color, float radius, float duration, float width)
        {
            if (rings == null || rings.Length == 0)
                return;
            RingSlot slot = rings[nextRing++ % rings.Length];
            slot.Center = position;
            slot.Color = color;
            slot.Age = 0f;
            slot.Duration = Mathf.Max(0.04f, duration);
            slot.Radius = radius;
            slot.Width = width;
            slot.Active = true;
            slot.Line.enabled = true;
            UpdateRing(slot, 0f);
        }

        public void TemporalConvergence(Vector3 position)
        {
            Burst(position, new Color(0.55f, 0.35f, 1f, 1f), 1.4f, 28);
            Ring(position, new Color(0.2f, 0.95f, 1f, 1f), 2.2f, 0.34f, 0.11f);
            Ring(position, new Color(0.72f, 0.35f, 1f, 0.9f), 3f, 0.5f, 0.075f);
        }

        public void TemporalAnchorPlanted(Vector3 position)
        {
            Burst(position, new Color(0.2f, 0.95f, 1f, 1f), 0.75f, 14);
            Ring(position, new Color(0.35f, 0.95f, 1f, 0.95f), 0.9f, 0.24f, 0.075f);
            Ring(position, new Color(0.72f, 0.35f, 1f, 0.75f), 1.3f, 0.34f, 0.045f);
        }

        public void TemporalReturn(Vector3 departure, Vector3 anchor, int bulletsDispelled)
        {
            Burst(departure, new Color(0.72f, 0.35f, 1f, 0.9f), 0.9f, 18);
            Ring(departure, new Color(0.72f, 0.35f, 1f, 0.75f), 1.35f, 0.2f, 0.065f);
            Burst(anchor, new Color(0.25f, 0.95f, 1f, 1f), 1.55f, Mathf.Clamp(30 + bulletsDispelled * 2, 30, 52));
            Ring(anchor, new Color(0.2f, 0.95f, 1f, 1f), 2.3f, 0.28f, 0.12f);
            Ring(anchor, new Color(0.72f, 0.35f, 1f, 0.9f), 3.35f, 0.46f, 0.075f);
        }

        public void BossAttackTelegraph(Vector3 position)
        {
            Burst(position, new Color(1f, 0.48f, 0.08f, 1f), 0.55f, 9);
            Ring(position, new Color(1f, 0.35f, 0.06f, 0.9f), 1.15f, 0.42f, 0.075f);
        }

        public void EnemyLaserTelegraph(Vector3 position)
        {
            Burst(position, new Color(1f, 0.72f, 0.12f, 1f), 0.65f, 12);
            Ring(position, new Color(1f, 0.55f, 0.08f, 0.9f), 1.25f, 0.42f, 0.08f);
        }

        public void EnemyLaserFired(Vector3 position)
        {
            Burst(position, new Color(1f, 0.18f, 0.05f, 1f), 1.05f, 22);
            Ring(position, new Color(1f, 0.3f, 0.06f, 0.95f), 1.7f, 0.24f, 0.11f);
        }

        public void BossDashTelegraph(Vector3 position)
        {
            Ring(position, new Color(1f, 0.22f, 0.08f, 0.95f), 2.2f, 0.5f, 0.1f);
            Ring(position, new Color(1f, 0.65f, 0.12f, 0.72f), 3f, 0.62f, 0.055f);
        }

        public void BossDash(Vector3 position)
        {
            Burst(position, new Color(1f, 0.32f, 0.08f, 1f), 1.4f, 30);
            Ring(position, new Color(1f, 0.55f, 0.12f, 0.9f), 2.5f, 0.3f, 0.09f);
        }

        public void BossCoreOpened(Vector3 position)
        {
            Burst(position, new Color(1f, 0.78f, 0.16f, 1f), 2f, 44);
            Ring(position, new Color(1f, 0.8f, 0.18f, 1f), 3.2f, 0.38f, 0.14f);
            Ring(position, new Color(0.55f, 0.35f, 1f, 0.95f), 4.2f, 0.56f, 0.08f);
        }

        public void BossPhaseShift(Vector3 position, bool finalPhase)
        {
            Color color = finalPhase ? new Color(1f, 0.12f, 0.06f, 1f) : new Color(1f, 0.5f, 0.1f, 1f);
            Burst(position, color, finalPhase ? 2.2f : 1.5f, finalPhase ? 52 : 32);
            Ring(position, color, finalPhase ? 4.6f : 3.4f, 0.5f, finalPhase ? 0.16f : 0.1f);
        }

        void Update()
        {
            if (rings == null)
                return;
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < rings.Length; i++)
            {
                RingSlot slot = rings[i];
                if (!slot.Active)
                    continue;
                slot.Age += dt;
                float t = Mathf.Clamp01(slot.Age / slot.Duration);
                UpdateRing(slot, t);
                if (t >= 1f)
                {
                    slot.Active = false;
                    slot.Line.enabled = false;
                }
            }
        }

        void UpdateRing(RingSlot slot, float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            float radius = Mathf.Lerp(slot.Radius * 0.12f, slot.Radius, eased);
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                slot.Points[i] = slot.Center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            }
            slot.Line.SetPositions(slot.Points);
            slot.Line.widthMultiplier = slot.Width * (1f - t);
            Color faded = slot.Color;
            faded.a *= 1f - t;
            slot.Line.startColor = faded;
            slot.Line.endColor = faded;
        }

        void BuildMaterials()
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (particleShader != null)
            {
                particleMaterial = new Material(particleShader) { name = "Combat Sparks (Runtime)", hideFlags = HideFlags.DontSave };
                SetWhite(particleMaterial);
            }
            if (lineShader != null)
            {
                lineMaterial = new Material(lineShader) { name = "Combat Rings (Runtime)", hideFlags = HideFlags.DontSave };
                SetWhite(lineMaterial);
            }
        }

        static void SetWhite(Material material)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        }

        void BuildBursts()
        {
            bursts = new ParticleSystem[burstPoolSize];
            for (int i = 0; i < bursts.Length; i++)
            {
                var go = new GameObject($"Impact Burst {i + 1:00}");
                go.transform.SetParent(transform, false);
                var particles = go.AddComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.playOnAwake = false;
                main.loop = false;
                main.duration = 0.6f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 72;
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var emission = particles.emission;
                emission.enabled = false;
                var shape = particles.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                var color = particles.colorOverLifetime;
                color.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.35f), new GradientAlphaKey(0f, 1f) });
                color.color = gradient;
                var size = particles.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
                var renderer = particles.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortingOrder = 80;
                if (particleMaterial != null)
                    renderer.sharedMaterial = particleMaterial;
                bursts[i] = particles;
            }
        }

        void BuildRings()
        {
            rings = new RingSlot[ringPoolSize];
            for (int i = 0; i < rings.Length; i++)
            {
                var go = new GameObject($"Impact Ring {i + 1:00}");
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.loop = true;
                line.positionCount = RingSegments;
                line.numCornerVertices = 2;
                line.numCapVertices = 2;
                line.alignment = LineAlignment.View;
                line.shadowCastingMode = ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sortingOrder = 75;
                if (lineMaterial != null)
                    line.sharedMaterial = lineMaterial;
                line.enabled = false;
                rings[i] = new RingSlot { Line = line };
            }
        }

        void OnDestroy()
        {
            if (particleMaterial != null) Destroy(particleMaterial);
            if (lineMaterial != null) Destroy(lineMaterial);
        }
    }
}
