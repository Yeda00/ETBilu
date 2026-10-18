using System;
using System.Collections.Generic;
using OneMoreTime.Core.Echo;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Feedback;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Ship;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Echo
{
    /// <summary>
    /// Press Record to plant an anchor and hold it while the ship is captured. Letting go — or filling
    /// the window — snaps the ship back to the anchor with a short spin and no damage, while a harmless
    /// preview becomes a combat echo and replays the route. The world itself never rewinds.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class EchoAbility : MonoBehaviour
    {
        [SerializeField] EchoConfig config;
        [SerializeField] PlayerInputReader input;
        [SerializeField] ShipMotor motor;
        [SerializeField] ShipWeapon weapon;
        [SerializeField] ShipGadget gadget;
        [SerializeField] Playfield playfield;
        [SerializeField] ProjectilePool pool;
        [SerializeField] EchoGhost echoPrefab;

        readonly List<EchoGhost> active = new();
        readonly List<PreviewPose> previewHistory = new();
        EchoRecorder recorder;
        EchoGhost preview;
        Health health;
        CameraImpact cameraImpact;
        CombatVfxPool effects;
        Vector2 anchor;
        float cooldownRemaining;
        float lastRouteSampleTime;
        LineRenderer routeLine;
        LineRenderer anchorRing;
        Material timelineMaterial;

        struct PreviewPose
        {
            public float Time;
            public EchoSample Pose;
        }

        public event Action Changed;
        public event Action<Vector3> AnchorPlanted;
        public event Action<Vector3, int> Returned;

        public bool IsRecording => recorder != null && recorder.IsRecording;
        public float RecordedSeconds => recorder?.Elapsed ?? 0f;
        public float MaxRecordSeconds => config != null ? config.maxRecordSeconds : 3.5f;
        public bool HasRecordingReady => false;
        public float ReadyDuration => 0f;
        public int MaxActiveEchoes => config != null ? config.maxActiveEchoes : 1;
        public IReadOnlyList<EchoGhost> ActiveEchoes => active;
        public Vector2 AnchorPosition => anchor;
        public float CooldownRemaining => cooldownRemaining;
        public float CooldownNormalized => config != null && config.cooldownSeconds > 0f
            ? Mathf.Clamp01(cooldownRemaining / config.cooldownSeconds)
            : 0f;
        public bool CanPlantAnchor => !IsRecording && cooldownRemaining <= 0f && active.Count < MaxActiveEchoes;

        void Awake()
        {
            if (config == null)
                config = ScriptableObject.CreateInstance<EchoConfig>();
            recorder = new EchoRecorder(config.maxRecordSeconds, config.samplesPerSecond);
            health = GetComponent<Health>();
            cameraImpact = FindFirstObjectByType<CameraImpact>();
            effects = FindFirstObjectByType<CombatVfxPool>();
        }

        void OnEnable()
        {
            if (weapon != null)
                weapon.Fired += OnShipFired;
            if (gadget != null)
                gadget.Used += OnGadgetUsed;
            if (motor != null)
                motor.Dodged += OnShipDodged;
        }

        void OnDisable()
        {
            if (weapon != null)
                weapon.Fired -= OnShipFired;
            if (gadget != null)
                gadget.Used -= OnGadgetUsed;
            if (motor != null)
                motor.Dodged -= OnShipDodged;
            recorder?.Cancel();
            if (preview != null && !active.Contains(preview))
                Destroy(preview.gameObject);
            preview = null;
            DestroyTimelineVisuals();
        }

        void Update()
        {
            if (input == null || motor == null || playfield == null)
                return;

            if (cooldownRemaining > 0f)
            {
                float before = cooldownRemaining;
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
                if (before > 0f && cooldownRemaining <= 0f)
                    Changed?.Invoke();
            }

            if (input.RecordPressed && !IsRecording)
                TryPlantAnchor();

            // Holding is what records: letting go (or E, kept as a temporary alias) takes the ship back.
            if (IsRecording && (!input.RecordHeld || input.InvokeEchoPressed))
            {
                if (recorder.Elapsed < config.minRecordSeconds)
                    CancelRecording();
                else
                    ReturnAndReplay();
            }

            if (!IsRecording)
                return;

            Vector2 aim = weapon != null ? weapon.AimDirection : Vector2.right;
            recorder.Tick(Time.deltaTime, motor.LocalPosition, motor.Bank, aim);
            UpdatePreview(aim);
            UpdateTimelineVisuals();
            if (recorder.IsFull)
                ReturnAndReplay();
        }

        public bool TryPlantAnchor()
        {
            if (!CanPlantAnchor || echoPrefab == null)
                return false;

            anchor = motor.LocalPosition;
            Vector2 aim = weapon != null ? weapon.AimDirection : Vector2.right;
            recorder.Begin(anchor, motor.Bank, aim);
            previewHistory.Clear();
            previewHistory.Add(new PreviewPose { Time = 0f, Pose = new EchoSample(0f, anchor, motor.Bank, aim) });

            preview = Instantiate(echoPrefab, playfield.transform);
            preview.name = "Eco Temporal · prévia";
            preview.BeginPreview(previewHistory[0].Pose);
            CreateTimelineVisuals();
            Vector3 anchorWorld = playfield.LocalToWorld(anchor);
            effects?.TemporalAnchorPlanted(anchorWorld);
            cameraImpact?.Impulse(0.2f, 0.08f, default, 0.18f);
            AnchorPlanted?.Invoke(anchorWorld);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Drops a recording that was let go too soon: the anchor goes away, with no echo and no cooldown.</summary>
        void CancelRecording()
        {
            recorder.Cancel();
            if (preview != null)
                Destroy(preview.gameObject);
            preview = null;
            previewHistory.Clear();
            DestroyTimelineVisuals();
            Changed?.Invoke();
        }

        /// <summary>Compatibility entry point: the old Invoke command now means return to the planted anchor.</summary>
        public bool TryInvoke() => ReturnAndReplay();

        public bool ReturnAndReplay()
        {
            if (!IsRecording || preview == null || active.Count >= config.maxActiveEchoes)
                return false;

            EchoRecording recording = recorder.End();
            if (recording == null)
            {
                if (preview != null)
                    Destroy(preview.gameObject);
                preview = null;
                previewHistory.Clear();
                DestroyTimelineVisuals();
                Changed?.Invoke();
                return false;
            }

            Vector3 departureWorld = transform.position;
            Vector2 returnDirection = anchor - motor.LocalPosition;
            Vector3 anchorWorld = playfield.LocalToWorld(anchor);
            transform.localPosition = new Vector3(anchor.x, anchor.y, transform.localPosition.z);
            health?.GrantInvulnerability(config.returnInvulnerabilitySeconds);
            motor.PlayRoll(config.returnInvulnerabilitySeconds);
            int bulletsDispelled = pool != null ? pool.DispelEnemyProjectiles(anchorWorld, config.returnBulletClearRadius) : 0;
            effects?.TemporalReturn(departureWorld, anchorWorld, bulletsDispelled);
            Vector2 bias = returnDirection.sqrMagnitude > 0f ? returnDirection.normalized * 0.45f : Vector2.zero;
            cameraImpact?.Impulse(0.7f, 0.18f, bias, 1.05f);

            EchoGhost ghost = preview;
            preview = null;
            ghost.name = "Eco da Âncora";
            ghost.Finished += OnGhostFinished;
            active.Add(ghost);
            ghost.Play(recording, playfield, pool, config.fadeSeconds, motor.DodgeSeconds);

            cooldownRemaining = config.cooldownSeconds;
            previewHistory.Clear();
            DestroyTimelineVisuals();
            Returned?.Invoke(anchorWorld, bulletsDispelled);
            Changed?.Invoke();
            return true;
        }

        void UpdatePreview(Vector2 aim)
        {
            var current = new EchoSample(recorder.Elapsed, motor.LocalPosition, motor.Bank, aim);
            previewHistory.Add(new PreviewPose { Time = recorder.Elapsed, Pose = current });
            float targetTime = recorder.Elapsed - config.previewDelaySeconds;
            int chosen = 0;
            while (chosen + 1 < previewHistory.Count && previewHistory[chosen + 1].Time <= targetTime)
                chosen++;
            preview?.SetPreviewPose(previewHistory[chosen].Pose);
            if (chosen > 1)
                previewHistory.RemoveRange(0, chosen - 1);
        }

        void CreateTimelineVisuals()
        {
            DestroyTimelineVisuals();
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                return;
            timelineMaterial = new Material(shader) { name = "Temporal Anchor (runtime)" };
            if (timelineMaterial.HasProperty("_BaseColor"))
                timelineMaterial.SetColor("_BaseColor", Color.white);

            routeLine = CreateLine("Rota temporal", false, 0.075f, new Color(0.35f, 0.92f, 1f, 0.18f), new Color(0.72f, 0.35f, 1f, 0.7f));
            anchorRing = CreateLine("Âncora temporal", true, 0.09f, new Color(0.35f, 0.92f, 1f, 0.9f), new Color(0.72f, 0.35f, 1f, 0.9f));
            if (anchorRing != null)
            {
                const int segments = 32;
                anchorRing.transform.localPosition = new Vector3(anchor.x, anchor.y, -0.08f);
                anchorRing.positionCount = segments;
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    anchorRing.SetPosition(i, new Vector3(Mathf.Cos(angle) * 0.48f, Mathf.Sin(angle) * 0.48f, 0f));
                }
            }

            routeLine.positionCount = 1;
            routeLine.SetPosition(0, new Vector3(anchor.x, anchor.y, -0.06f));
            lastRouteSampleTime = 0f;
        }

        LineRenderer CreateLine(string objectName, bool loop, float width, Color start, Color end)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(playfield.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.widthMultiplier = width;
            line.sharedMaterial = timelineMaterial;
            line.startColor = start;
            line.endColor = end;
            return line;
        }

        void UpdateTimelineVisuals()
        {
            if (routeLine != null && (recorder.Elapsed - lastRouteSampleTime >= 0.045f || routeLine.positionCount == 1))
            {
                Vector2 p = motor.LocalPosition;
                int index = routeLine.positionCount;
                routeLine.positionCount = index + 1;
                routeLine.SetPosition(index, new Vector3(p.x, p.y, -0.06f));
                lastRouteSampleTime = recorder.Elapsed;
            }

            if (anchorRing != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 9f) * 0.08f;
                anchorRing.transform.localScale = Vector3.one * pulse;
            }
        }

        void DestroyTimelineVisuals()
        {
            if (routeLine != null)
                Destroy(routeLine.gameObject);
            if (anchorRing != null)
                Destroy(anchorRing.gameObject);
            if (timelineMaterial != null)
                Destroy(timelineMaterial);
            routeLine = null;
            anchorRing = null;
            timelineMaterial = null;
        }

        void OnShipFired(Vector2 origin, Vector2 direction)
        {
            if (IsRecording)
                recorder.AddEvent(EchoEventType.Shot, origin, direction);
        }

        void OnGadgetUsed(Vector2 origin, Vector2 direction)
        {
            if (IsRecording)
                recorder.AddEvent(EchoEventType.Gadget, origin, direction);
        }

        void OnShipDodged(Vector2 origin, Vector2 direction)
        {
            if (IsRecording)
                recorder.AddEvent(EchoEventType.Dodge, origin, direction);
        }

        void OnGhostFinished(EchoGhost ghost)
        {
            ghost.Finished -= OnGhostFinished;
            active.Remove(ghost);
            Changed?.Invoke();
        }
    }
}
