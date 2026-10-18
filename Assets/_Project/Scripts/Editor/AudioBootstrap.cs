using System.Collections.Generic;
using OneMoreTime.Core.Audio;
using OneMoreTime.Gameplay.Audio;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Session;
using OneMoreTime.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreTime.EditorTools
{
    /// <summary>
    /// Builds the audio layer of the prototype: the <see cref="AudioLibrary"/> asset with a first mapping of
    /// cues to the CC0 clips in <c>Assets/_Project/Audio</c>, the import settings of the music, and the
    /// Audio group in the Arena_01 scene. Idempotent like <see cref="PlaceholderBootstrap"/>: an existing
    /// library is kept (delete it to rebuild), and the scene is only touched where something is missing.
    /// The mapping is a starting point chosen from file names; tune it by ear in the library asset.
    /// </summary>
    public static class AudioBootstrap
    {
        const string AudioDir = "Assets/_Project/Audio";
        const string LibraryPath = "Assets/_Project/Data/AudioLibrary.asset";

        [MenuItem("One More Time/Bootstrap/Build Audio", priority = 10)]
        public static void BuildAll()
        {
            ConfigureMusicImport();
            var library = BuildLibrary();
            WireScene(library);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- import settings

        static void ConfigureMusicImport()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir + "/Music" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                    continue;
                var settings = importer.defaultSampleSettings;
                if (settings.loadType == AudioClipLoadType.Streaming)
                    continue;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.55f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- library

        static AudioLibrary BuildLibrary()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            var cues = new List<AudioLibrary.CueEntry>
            {
                // Shots. The echo's are muffled and a touch lower: another time layer (GDD 08).
                Cue(SoundCue.ShipShot, 0.35f, 4, 0.04f, 0.95f, 1.08f, true, 0f, 0f, 0f,
                    "SFX/Deva8bit/Shoot 2.wav", "SFX/Deva8bit/Shoot 3.wav"),
                Cue(SoundCue.EchoShot, 0.26f, 3, 0.04f, 0.85f, 0.95f, true, 1800f, 0f, 0f,
                    "SFX/Deva8bit/Shoot 2.wav", "SFX/Deva8bit/Shoot 3.wav"),
                Cue(SoundCue.EnemyShot, 0.22f, 4, 0.05f, 0.9f, 1.1f, true, 0f, 0f, 0f,
                    "SFX/KenneySciFi/laserRetro_000.ogg", "SFX/KenneySciFi/laserRetro_001.ogg", "SFX/KenneySciFi/laserRetro_002.ogg",
                    "SFX/KenneySciFi/laserRetro_003.ogg", "SFX/KenneySciFi/laserRetro_004.ogg"),

                // Hits and deaths, growing with the size of the target.
                Cue(SoundCue.EnemyHit, 0.28f, 3, 0.04f, 0.95f, 1.1f, true, 0f, 0f, 0f,
                    "SFX/Deva8bit/Hit 1.wav", "SFX/Deva8bit/Hit 2.wav", "SFX/Deva8bit/Hit 4.wav", "SFX/Deva8bit/Hit 5.wav", "SFX/Deva8bit/Hit 6.wav"),
                Cue(SoundCue.ShieldBlock, 0.35f, 2, 0.06f, 0.95f, 1.05f, true, 0f, 0f, 0f,
                    "SFX/KenneySciFi/impactMetal_002.ogg", "SFX/KenneySciFi/impactMetal_004.ogg"),
                Cue(SoundCue.PlayerHit, 0.7f, 1, 0.1f, 0.98f, 1.02f, false, 0f, 0f, 0f,
                    "SFX/Mini8bit/Ouch/Ouch__001.ogg", "SFX/Mini8bit/Ouch/Ouch__002.ogg", "SFX/Mini8bit/Ouch/Ouch__003.ogg"),
                Cue(SoundCue.EnemyDeathSmall, 0.5f, 4, 0.03f, 0.95f, 1.1f, true, 0f, 0f, 0f,
                    "SFX/Deva8bit/Explosion 5.wav", "SFX/Deva8bit/Explosion 3.wav"),
                Cue(SoundCue.EnemyDeathBig, 0.6f, 3, 0.05f, 0.92f, 1.05f, true, 0f, 0f, 0f,
                    "SFX/Deva8bit/Explosion 2.wav", "SFX/Deva8bit/Explosion 4.wav", "SFX/KenneySciFi/explosionCrunch_000.ogg"),
                Cue(SoundCue.GeneratorDeath, 0.75f, 2, 0.1f, 0.95f, 1.02f, true, 0f, 0f, 0f,
                    "SFX/KenneySciFi/explosionCrunch_001.ogg", "SFX/KenneySciFi/explosionCrunch_002.ogg"),
                Cue(SoundCue.CoreDeath, 0.9f, 1, 0.2f, 1f, 1f, false, 0f, 0f, 0f,
                    "SFX/KenneySciFi/lowFrequency_explosion_000.ogg", "SFX/KenneySciFi/explosionCrunch_004.ogg"),
                Cue(SoundCue.PlayerDeath, 0.9f, 1, 0.2f, 1f, 1f, false, 0f, 0f, 0f,
                    "SFX/Deva8bit/Explosion 1.wav"),

                // Gadget.
                Cue(SoundCue.MineDrop, 0.4f, 2, 0.1f, 0.95f, 1.05f, true, 0f, 0f, 0f,
                    "SFX/Deva8bit/Powerup 3.wav"),
                Cue(SoundCue.MineBlast, 0.7f, 2, 0.1f, 0.95f, 1.05f, true, 0f, 0f, 0f,
                    "SFX/KenneySciFi/lowFrequency_explosion_001.ogg"),

                // The echo: the record pulse is procedural (AudioDirector); a reversal then a digital impact on invoke.
                Cue(SoundCue.RecordStart, 0.4f, 1, 0.1f, 1f, 1f, false, 0f, 0f, 0f,
                    "SFX/Deva8bit/Select 4.wav"),
                Cue(SoundCue.RecordStop, 0.35f, 1, 0.1f, 1f, 1f, false, 0f, 0f, 0f,
                    "SFX/Deva8bit/Select 2.wav"),
                Cue(SoundCue.EchoInvoke, 0.45f, 2, 0.1f, 1f, 1f, false, 0f, 0.7f, 0.2f,
                    "SFX/Time/Reverse-Time-Loop.ogg"),
                Cue(SoundCue.EchoArrive, 0.6f, 2, 0.1f, 0.7f, 0.8f, false, 0f, 0f, 0f,
                    "SFX/KenneySciFi/impactMetal_003.ogg"),
                Cue(SoundCue.Combo, 0.55f, 2, 0.1f, 1f, 1f, true, 0f, 1.2f, 0.3f,
                    "SFX/Mini8bit/Starpower/Starpower__001.ogg", "SFX/Mini8bit/Starpower/Starpower__002.ogg"),

                // Boss and session.
                Cue(SoundCue.BossVulnerable, 0.6f, 1, 0.3f, 1f, 1f, false, 0f, 0f, 0f,
                    "SFX/KenneySciFi/forceField_000.ogg"),
                Cue(SoundCue.BossPhase, 0.7f, 1, 0.5f, 1f, 1f, false, 0f, 1.6f, 0.4f,
                    "SFX/Mini8bit/Roar1/Roar1__001.ogg", "SFX/Mini8bit/Roar1/Roar1__002.ogg"),
                Cue(SoundCue.Pause, 0.5f, 1, 0.1f, 1f, 1f, false, 0f, 0f, 0f,
                    "UI/menu-select.ogg"),
                Cue(SoundCue.Resume, 0.5f, 1, 0.1f, 1f, 1f, false, 0f, 0f, 0f,
                    "UI/menu-highlight.ogg"),
                Cue(SoundCue.Victory, 0.8f, 1, 0.5f, 1f, 1f, false, 0f, 0f, 0f,
                    "Music/NesShooter/Win Jingle.wav"),

                // Menus and the mission card.
                Cue(SoundCue.MenuMove, 0.45f, 2, 0.04f, 1f, 1f, false, 0f, 0f, 0f,
                    "UI/menu-highlight.ogg"),
                Cue(SoundCue.MenuSelect, 0.55f, 2, 0.05f, 1f, 1f, false, 0f, 0f, 0f,
                    "UI/menu-select.ogg"),
                Cue(SoundCue.MenuBack, 0.45f, 2, 0.05f, 0.85f, 0.85f, false, 0f, 0f, 0f,
                    "SFX/Deva8bit/Select 3.wav"),
                Cue(SoundCue.MenuStart, 0.6f, 1, 0.2f, 1f, 1f, false, 0f, 0f, 0f,
                    "SFX/Deva8bit/Powerup 2.wav"),
                Cue(SoundCue.MenuDenied, 0.5f, 1, 0.1f, 1f, 1f, false, 0f, 0f, 0f,
                    "SFX/Deva8bit/Wrong 1.wav"),
                Cue(SoundCue.MissionCard, 0.6f, 1, 0.5f, 1f, 1f, false, 0f, 1.4f, 0.3f,
                    "SFX/KenneySciFi/forceField_001.ogg"),
            };

            var music = new List<AudioLibrary.MusicEntry>
            {
                Music(MusicTrack.Waves, null, "Music/EpicSpaceShooter/8bit-spaceshooter.mp3", 0.6f),
                Music(MusicTrack.Boss, "Music/NesShooter/BossIntro.wav", "Music/NesShooter/BossMain.wav", 0.65f),
                Music(MusicTrack.Title, null, "Music/NesShooter/Venus.wav", 0.65f),
                Music(MusicTrack.Missions, null, "Music/NesShooter/Map.wav", 0.6f),
                Music(MusicTrack.Intro, null, "Music/Juhani Junkala [Chiptune Adventures] 1. Stage 1.ogg", 0.6f),
            };

            if (existing != null)
            {
                // Keep what is there (it may be tuned by ear) and add only the cues and tracks that are missing.
                var allCues = new List<AudioLibrary.CueEntry>(existing.cues);
                foreach (var cue in cues)
                    if (existing.Find(cue.cue) == null)
                        allCues.Add(cue);
                var allMusic = new List<AudioLibrary.MusicEntry>(existing.music);
                foreach (var track in music)
                    if (existing.FindMusic(track.track) == null)
                        allMusic.Add(track);
                if (allCues.Count != existing.cues.Length || allMusic.Count != existing.music.Length)
                {
                    existing.cues = allCues.ToArray();
                    existing.music = allMusic.ToArray();
                    EditorUtility.SetDirty(existing);
                    Debug.Log("[One More Time] Audio library completed with the missing cues and tracks.");
                }
                return existing;
            }

            var library = ScriptableObject.CreateInstance<AudioLibrary>();
            library.cues = cues.ToArray();
            library.music = music.ToArray();
            EnsureFolder("Assets/_Project/Data");
            AssetDatabase.CreateAsset(library, LibraryPath);
            Debug.Log($"[One More Time] Audio library created at {LibraryPath}.");
            return library;
        }

        static AudioLibrary.CueEntry Cue(SoundCue cue, float volume, int maxVoices, float minInterval, float pitchMin, float pitchMax,
            bool panned, float lowPassHz, float maxSeconds, float fadeOut, params string[] clipPaths)
        {
            return new AudioLibrary.CueEntry
            {
                cue = cue,
                clips = Clips(clipPaths),
                volume = volume,
                maxVoices = maxVoices,
                minInterval = minInterval,
                pitchMin = pitchMin,
                pitchMax = pitchMax,
                panned = panned,
                lowPassHz = lowPassHz,
                maxSeconds = maxSeconds,
                fadeOutSeconds = fadeOut > 0f ? fadeOut : 0.1f,
            };
        }

        static AudioLibrary.MusicEntry Music(MusicTrack track, string intro, string loop, float volume)
        {
            return new AudioLibrary.MusicEntry
            {
                track = track,
                intro = intro != null ? Clip(intro) : null,
                loop = Clip(loop),
                volume = volume,
            };
        }

        static AudioClip[] Clips(string[] paths)
        {
            var list = new List<AudioClip>();
            foreach (string path in paths)
            {
                var clip = Clip(path);
                if (clip != null)
                    list.Add(clip);
            }
            return list.ToArray();
        }

        static AudioClip Clip(string relativePath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioDir}/{relativePath}");
            if (clip == null)
                Debug.LogWarning($"[One More Time] Audio clip not found: {AudioDir}/{relativePath}");
            return clip;
        }

        // ---------------------------------------------------------------- scene

        static void WireScene(AudioLibrary library)
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != PlaceholderBootstrap.ScenePath)
                scene = EditorSceneManager.OpenScene(PlaceholderBootstrap.ScenePath, OpenSceneMode.Single);

            var director = Object.FindFirstObjectByType<AudioDirector>();
            var systems = GameObject.Find("Systems");
            var playfield = Object.FindFirstObjectByType<Playfield>();
            var pool = Object.FindFirstObjectByType<ProjectilePool>();
            var spawner = Object.FindFirstObjectByType<WaveSpawner>();
            var stage = Object.FindFirstObjectByType<StageDirector>();
            var combos = Object.FindFirstObjectByType<ComboTracker>();
            var session = Object.FindFirstObjectByType<GameSession>();
            var ability = Object.FindFirstObjectByType<EchoAbility>();
            if (systems == null || playfield == null || pool == null || spawner == null || stage == null
                || combos == null || session == null || ability == null)
            {
                Debug.LogError("[One More Time] Arena_01 is missing a base object; run Build Placeholder Content first.");
                return;
            }

            bool changed = false;
            if (director == null)
            {
                var group = new GameObject("Audio");
                group.transform.SetParent(systems.transform, false);
                director = group.AddComponent<AudioDirector>();
                group.AddComponent<SfxPlayer>();
                group.AddComponent<MusicPlayer>();
                changed = true;
            }

            var sfx = director.GetComponent<SfxPlayer>() ?? director.gameObject.AddComponent<SfxPlayer>();
            var music = director.GetComponent<MusicPlayer>() ?? director.gameObject.AddComponent<MusicPlayer>();
            var player = ability.GetComponent<Health>();

            changed |= SetIfNull(sfx, "library", library);
            changed |= SetIfNull(sfx, "playfield", playfield);
            changed |= SetIfNull(music, "library", library);
            changed |= SetIfNull(director, "sfx", sfx);
            changed |= SetIfNull(director, "music", music);
            changed |= SetIfNull(director, "player", player);
            changed |= SetIfNull(director, "waves", spawner);
            changed |= SetIfNull(director, "stage", stage);
            changed |= SetIfNull(director, "combos", combos);
            changed |= SetIfNull(director, "echoes", ability);
            changed |= SetIfNull(director, "session", session);
            changed |= SetIfNull(director, "pool", pool);

            if (!changed)
                return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[One More Time] Audio wired into {PlaceholderBootstrap.ScenePath}.");
        }

        static bool SetIfNull(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
                throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
            if (property.objectReferenceValue != null || value == null)
                return false;
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
