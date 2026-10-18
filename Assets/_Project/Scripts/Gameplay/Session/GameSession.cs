using System;
using System.Collections;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreTime.Gameplay.Session
{
    public enum SessionState
    {
        Playing = 0,
        Paused = 1,
        Defeated = 2,
        Cleared = 3,
    }

    /// <summary>
    /// Score, pause, win and loss. Victory is the Guardian of the Cycle falling (the stage reports it).
    /// Temporal combos add to the score (GDD 03, pontuação por habilidade).
    /// Death reloads the scene at the current encounter — the "eco durante a vida" model recommended
    /// for the prototype (GDD 06).
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Health player;
        [SerializeField] WaveSpawner spawner;
        [SerializeField] StageDirector stage;
        [SerializeField] ComboTracker combos;
        [SerializeField, Min(0f)] float restartDelay = 2f;

        public event Action Changed;

        public SessionState State { get; private set; } = SessionState.Playing;
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public int ComboCount { get; private set; }

        void OnEnable()
        {
            if (player != null) player.Died += OnPlayerDied;
            if (spawner != null)
                spawner.EnemyRemoved += OnEnemyRemoved;
            if (stage != null)
            {
                stage.Scored += AddScore;
                stage.Completed += OnCleared;
            }
            if (combos != null)
                combos.Triggered += OnCombo;
        }

        void OnDisable()
        {
            if (player != null) player.Died -= OnPlayerDied;
            if (spawner != null)
                spawner.EnemyRemoved -= OnEnemyRemoved;
            if (stage != null)
            {
                stage.Scored -= AddScore;
                stage.Completed -= OnCleared;
            }
            if (combos != null)
                combos.Triggered -= OnCombo;
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (input == null || !input.PausePressed)
                return;
            if (State == SessionState.Playing) SetPaused(true);
            else if (State == SessionState.Paused) SetPaused(false);
        }

        void SetPaused(bool paused)
        {
            State = paused ? SessionState.Paused : SessionState.Playing;
            Time.timeScale = paused ? 0f : 1f;
            Changed?.Invoke();
        }

        void OnEnemyRemoved(Enemy enemy, bool killed)
        {
            if (!killed || State != SessionState.Playing)
                return;
            Kills++;
            AddScore(enemy.ScoreValue);
        }

        void OnCombo(ComboReport combo)
        {
            if (State != SessionState.Playing)
                return;
            ComboCount++;
            AddScore(combo.Points);
        }

        void AddScore(int points)
        {
            if (State != SessionState.Playing && State != SessionState.Paused)
                return;
            Score += points;
            Changed?.Invoke();
        }

        void OnPlayerDied(Health _, DamageInfo __)
        {
            if (State == SessionState.Defeated)
                return;
            State = SessionState.Defeated;
            Changed?.Invoke();
            player.gameObject.SetActive(false);
            StartCoroutine(RestartAfterDelay());
        }

        void OnCleared()
        {
            if (State != SessionState.Playing)
                return;
            State = SessionState.Cleared;
            Changed?.Invoke();
        }

        IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSecondsRealtime(restartDelay);
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex >= 0 ? scene.buildIndex : 0);
        }
    }
}
