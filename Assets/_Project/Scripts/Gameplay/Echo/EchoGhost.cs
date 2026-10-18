using System;
using System.Collections.Generic;
using OneMoreTime.Core.Echo;
using OneMoreTime.Core.Ship;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Gadgets;
using OneMoreTime.Gameplay.Ship;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Echo
{
    /// <summary>
    /// The ghost ship. Replays a recording on its own clock from the recorded start point, fires
    /// each recorded shot and drops each recorded mine once, and never decides anything (GDD 02).
    /// It has no Health and no collider, so it neither blocks the ship nor takes damage.
    /// </summary>
    public sealed class EchoGhost : MonoBehaviour
    {
        [SerializeField] Projectile projectile;
        [Tooltip("Mine dropped when the recording used the gadget.")]
        [SerializeField] TimeMine mine;
        [Tooltip("Visual child that banks like the recorded ship.")]
        [SerializeField] Transform visual;
        [Tooltip("Renderers faded at the start and end of the playback.")]
        [SerializeField] Renderer[] fadeRenderers;
        [Tooltip("Recoil and muzzle flash replayed with every recorded shot.")]
        [SerializeField] ShipFireFeedback shotFeedback;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        readonly List<EchoEvent> due = new();
        EchoPlayback playback;
        Playfield playfield;
        ProjectilePool pool;
        float fadeSeconds;
        MaterialPropertyBlock block;
        Color[] baseColors;
        bool previewing;
        float rollSeconds;
        float rollClock;
        bool rolling;

        public event Action<EchoGhost> Finished;
        public float Progress => playback?.Progress ?? 1f;

        void Awake()
        {
            if (shotFeedback == null)
                shotFeedback = GetComponent<ShipFireFeedback>();
        }

        /// <param name="dodgeSeconds">How long the ship's dodge roll lasts; the echo spins for as long at each recorded dodge.</param>
        public void Play(EchoRecording recording, Playfield field, ProjectilePool projectiles, float fade, float dodgeSeconds = 0.4f)
        {
            previewing = false;
            rollSeconds = Mathf.Max(0.05f, dodgeSeconds);
            playback = new EchoPlayback(recording);
            playfield = field;
            pool = projectiles;
            fadeSeconds = fade;
            CacheColors();
            ApplyPose();
            Step(0f);
        }

        /// <summary>Shows a harmless, delayed afterimage while the route is being recorded.</summary>
        public void BeginPreview(EchoSample pose)
        {
            playback = null;
            previewing = true;
            CacheColors();
            SetPreviewPose(pose);
            ApplyOpacity(0.34f);
        }

        public void SetPreviewPose(EchoSample pose)
        {
            if (!previewing)
                return;
            ApplyPose(pose);
        }

        void Update()
        {
            if (playback == null)
                return;
            Step(Time.deltaTime);
            if (playback.IsFinished)
            {
                Finished?.Invoke(this);
                Destroy(gameObject);
            }
        }

        void Step(float dt)
        {
            due.Clear();
            playback.Advance(dt, due);
            foreach (var e in due)
            {
                if (e.type == EchoEventType.Shot)
                {
                    pool.Spawn(projectile, playfield.LocalToWorld(e.position), playfield.DirectionToWorld(e.direction), Faction.Player, ShotSource.Echo);
                    shotFeedback?.Play(e.position, e.direction);
                }
                else if (e.type == EchoEventType.Gadget && mine != null)
                    Instantiate(mine, playfield.LocalToWorld(e.position), Quaternion.identity, pool.transform).Arm(Faction.Player, ShotSource.Echo, pool);
                else if (e.type == EchoEventType.Dodge)
                {
                    rolling = true;
                    rollClock = 0f;
                }
            }
            if (rolling)
            {
                rollClock += dt;
                if (rollClock >= rollSeconds)
                    rolling = false;
            }
            ApplyPose();
            ApplyFade();
        }

        void ApplyPose()
        {
            ApplyPose(playback.Pose);
        }

        void ApplyPose(EchoSample pose)
        {
            transform.localPosition = new Vector3(pose.position.x, pose.position.y, 0f);
            if (visual != null)
            {
                Vector2 aim = pose.aimDirection.sqrMagnitude > 0f ? pose.aimDirection : Vector2.right;
                float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                float roll = rolling ? DodgeModel.RollAngle(rollClock / rollSeconds) : 0f;
                visual.localRotation = ShipPose.Rotation(pose.rotation, angle, roll);
            }
        }

        void CacheColors()
        {
            if (fadeRenderers == null || fadeRenderers.Length == 0)
                return;
            block ??= new MaterialPropertyBlock();
            baseColors = new Color[fadeRenderers.Length];
            for (int i = 0; i < fadeRenderers.Length; i++)
            {
                var m = fadeRenderers[i] != null ? fadeRenderers[i].sharedMaterial : null;
                baseColors[i] = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
            }
        }

        void ApplyFade()
        {
            if (baseColors == null || fadeSeconds <= 0f)
                return;
            float t = playback.Time, d = playback.Duration;
            float k = Mathf.Clamp01(Mathf.Min(t, d - t) / fadeSeconds);
            ApplyOpacity(k);
        }

        void ApplyOpacity(float opacity)
        {
            if (baseColors == null)
                return;
            for (int i = 0; i < fadeRenderers.Length; i++)
            {
                if (fadeRenderers[i] == null)
                    continue;
                var c = baseColors[i];
                c.a *= Mathf.Clamp01(opacity);
                fadeRenderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                fadeRenderers[i].SetPropertyBlock(block);
            }
        }
    }
}
