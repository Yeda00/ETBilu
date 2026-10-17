using System;
using System.Collections.Generic;
using OneMoreTime.Core.Echo;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Gadgets;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Echo
{
    /// <summary>
    /// The ghost ship. Replays a recording on its own clock from the recorded start point, fires
    /// each recorded shot and drops each recorded mine once, and never decides anything (GDD 02).
    /// It has no Health and no collider, so it neither blocks the ship nor takes damage. Switches
    /// still see it: they check positions, not physics (see SwitchPad).
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

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        readonly List<EchoEvent> due = new();
        EchoPlayback playback;
        Playfield playfield;
        ProjectilePool pool;
        float fadeSeconds;
        MaterialPropertyBlock block;
        Color[] baseColors;

        public event Action<EchoGhost> Finished;
        public float Progress => playback?.Progress ?? 1f;

        public void Play(EchoRecording recording, Playfield field, ProjectilePool projectiles, float fade)
        {
            playback = new EchoPlayback(recording);
            playfield = field;
            pool = projectiles;
            fadeSeconds = fade;
            CacheColors();
            ApplyPose();
            Step(0f);
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
            ApplyPose();
            foreach (var e in due)
            {
                if (e.type == EchoEventType.Shot)
                    pool.Spawn(projectile, playfield.LocalToWorld(e.position), playfield.DirectionToWorld(e.direction), Faction.Player, ShotSource.Echo);
                else if (e.type == EchoEventType.Gadget && mine != null)
                    Instantiate(mine, playfield.LocalToWorld(e.position), Quaternion.identity, pool.transform).Arm(Faction.Player, ShotSource.Echo);
            }
            ApplyFade();
        }

        void ApplyPose()
        {
            var pose = playback.Pose;
            transform.localPosition = new Vector3(pose.position.x, pose.position.y, 0f);
            if (visual != null)
                visual.localRotation = Quaternion.Euler(pose.rotation, 0f, 0f);
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
            for (int i = 0; i < fadeRenderers.Length; i++)
            {
                if (fadeRenderers[i] == null)
                    continue;
                var c = baseColors[i];
                c.a *= k;
                fadeRenderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                fadeRenderers[i].SetPropertyBlock(block);
            }
        }
    }
}
