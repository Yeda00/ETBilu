using System;
using System.Collections.Generic;
using OneMoreTime.Core.Echo;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Ship;
using OneMoreTime.Gameplay.World;
using UnityEngine;

namespace OneMoreTime.Gameplay.Echo
{
    /// <summary>
    /// Hold Record to capture up to 5 s of the ship; press Invoke to release an echo that replays
    /// it while you keep flying (GDD 02). Recording and invoking are separate buttons on purpose (GDD 07).
    /// Runs after the motor and weapon so it samples this frame's final pose.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class EchoAbility : MonoBehaviour
    {
        [SerializeField] EchoConfig config;
        [SerializeField] PlayerInputReader input;
        [SerializeField] ShipMotor motor;
        [SerializeField] ShipWeapon weapon;
        [SerializeField] Playfield playfield;
        [SerializeField] ProjectilePool pool;
        [SerializeField] EchoGhost echoPrefab;

        readonly List<EchoGhost> active = new();
        EchoRecorder recorder;
        EchoRecording ready;
        bool waitForRelease;   // a full recording must not restart while Record is still held

        public event Action Changed;

        public bool IsRecording => recorder != null && recorder.IsRecording;
        public float RecordedSeconds => recorder?.Elapsed ?? 0f;
        public float MaxRecordSeconds => config != null ? config.maxRecordSeconds : 5f;
        public bool HasRecordingReady => ready != null;
        public float ReadyDuration => ready?.Duration ?? 0f;
        public int MaxActiveEchoes => config != null ? config.maxActiveEchoes : 3;
        public IReadOnlyList<EchoGhost> ActiveEchoes => active;

        void Awake()
        {
            if (config == null)
                config = ScriptableObject.CreateInstance<EchoConfig>();
            recorder = new EchoRecorder(config.maxRecordSeconds, config.samplesPerSecond);
        }

        void OnEnable()
        {
            if (weapon != null)
                weapon.Fired += OnShipFired;
        }

        void OnDisable()
        {
            if (weapon != null)
                weapon.Fired -= OnShipFired;
            recorder?.Cancel();
        }

        void Update()
        {
            if (input == null || motor == null)
                return;

            if (!input.RecordHeld)
                waitForRelease = false;

            if (recorder.IsRecording)
            {
                recorder.Tick(Time.deltaTime, motor.LocalPosition, motor.Bank);
                if (recorder.IsFull)
                    waitForRelease = true;
                if (!input.RecordHeld || recorder.IsFull)
                    FinishRecording();
            }
            else if (input.RecordHeld && !waitForRelease)
            {
                recorder.Begin(motor.LocalPosition, motor.Bank);
                Changed?.Invoke();
            }

            if (input.InvokeEchoPressed)
                TryInvoke();
        }

        void FinishRecording()
        {
            var r = recorder.End();
            if (r != null)
                ready = r;
            Changed?.Invoke();
        }

        /// <summary>Spawn an echo from the ready recording, if there is one and a free slot.</summary>
        public bool TryInvoke()
        {
            if (ready == null || active.Count >= config.maxActiveEchoes || echoPrefab == null)
                return false;

            var ghost = Instantiate(echoPrefab, playfield.transform);
            ghost.name = $"Echo {active.Count + 1:00}";
            ghost.Finished += OnGhostFinished;
            active.Add(ghost);
            ghost.Play(ready, playfield, pool, config.fadeSeconds);
            if (config.consumeRecordingOnInvoke)
                ready = null;
            Changed?.Invoke();
            return true;
        }

        void OnShipFired(Vector2 origin, Vector2 direction)
        {
            if (recorder.IsRecording)
                recorder.AddEvent(EchoEventType.Shot, origin, direction);
        }

        void OnGhostFinished(EchoGhost ghost)
        {
            ghost.Finished -= OnGhostFinished;
            active.Remove(ghost);
            Changed?.Invoke();
        }
    }
}
