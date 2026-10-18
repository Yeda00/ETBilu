using System;
using System.Collections.Generic;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Enemies;
using UnityEditor;
using UnityEngine;

namespace OneMoreTime.EditorTools
{
    /// <summary>
    /// Gives each model of the small and medium enemies its own behaviour (GDD 04): one <see cref="EnemyProfile"/>
    /// per model on <c>Enemy_S</c> and <c>Enemy_M</c>, the shot prefabs they fire, and the model of each wave of the
    /// first encounter. Like the other bootstraps it keeps what exists: profiles are written only to an enemy that
    /// has none (clear the Profiles list on the prefab to redo them), shot prefabs only when missing, and a wave only
    /// when its variant is still 0. The numbers are starting values, to be tuned by playing.
    /// </summary>
    public static class EnemyVariantsBootstrap
    {
        const string Root = "Assets/_Project";
        const string EnemyDir = Root + "/Prefabs/Enemies";
        const string ShotDir = Root + "/Prefabs/Projectiles";
        const string MaterialDir = Root + "/Art/Placeholder/Materials";
        const string WavePath = Root + "/Data/Waves/Encounter01_PrimeiroCiclo.asset";

        [Serializable]
        sealed class ProfileList
        {
            public EnemyProfile[] profiles;
        }

        [MenuItem("One More Time/Bootstrap/Build Enemy Variants", priority = 12)]
        public static void BuildAll()
        {
            var shots = BuildShots();

            Apply("Enemy_S", shots, SmallProfiles());
            Apply("Enemy_M", shots, MediumProfiles());
            Apply("Enemy_W", shots, InterceptorProfiles());
            RestrictInterceptorModels();
            AssignWaveVariants();
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyVariantsBootstrap] Enemy variants ready.");
        }

        [MenuItem("One More Time/Bootstrap/Apply Progressive Combat Pass", priority = 14)]
        public static void ApplyProgressiveCombatPass()
        {
            var shots = BuildShots();
            Apply("Enemy_M", shots, MediumProfiles(), overwriteProfiles: true);
            Apply("Enemy_W", shots, InterceptorProfiles(), overwriteProfiles: true);
            RestrictInterceptorModels();
            RebuildProgressiveEncounter();
            TuneGuardian();
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyVariantsBootstrap] Progressive waves, tracking laser and Guardian tuning applied.");
        }

        static Projectile[] BuildShots() => new[]
        {
            // index in EnemyGun.shotVariants − 1 = GunTuning.shotVariant − 1
            BuildShot("Shot_Enemy_Fast", speed: 12.5f, lifetime: 4f, size: 0.26f, color: new Color(1f, 0.88f, 0.35f)),
            BuildShot("Shot_Enemy_Slow", speed: 8f, lifetime: 4.5f, size: 0.3f, color: new Color(1f, 0.55f, 0.16f)),
            BuildShot("Shot_Enemy_Heavy", speed: 8.8f, lifetime: 4.5f, size: 0.5f, color: null),
            BuildShot("Shot_Enemy_Pellet", speed: 10.5f, lifetime: 2.4f, size: 0.26f, color: new Color(1f, 0.32f, 0.82f)),
        };

        // ---------------------------------------------------------------- profiles

        static EnemyProfile[] SmallProfiles() => new[]
        {
            new EnemyProfile   // S_1 — kamikaze: homes in on the ship and blows up on it, or by itself after a while
            {
                name = "S_1 · Kamikaze", pattern = MovementPattern.Chase, health = 2, scoreValue = 120,
                speed = 6.4f, turnRate = 85f,
                gun = new GunTuning { fires = false },
                blast = new BlastTuning { explodes = true, triggerRadius = 1f, fuseSeconds = 0.15f, autoSeconds = 5.5f, blastRadius = 1.7f, damage = 1 },
            },
            new EnemyProfile   // S_2 — fast, one slow-firing small shot
            {
                name = "S_2 · Veloz", pattern = MovementPattern.Swoop, health = 2, scoreValue = 100,
                speed = 9.8f, swoopAt = 0.35f, swoopSeconds = 0.6f, maxSwoop = 5f,
                gun = new GunTuning { fires = true, aimAtShip = true, firstDelay = 0.25f, interval = 2.6f, shotsPerBurst = 1, windup = 0.25f, maxBursts = 2, shotVariant = 1 },
            },
            new EnemyProfile   // S_3 — a little slower, a short slow burst of three
            {
                name = "S_3 · Rajada curta", pattern = MovementPattern.Swoop, health = 3, scoreValue = 150,
                speed = 6.8f, swoopAt = 0.35f, swoopSeconds = 0.9f, maxSwoop = 5f,
                gun = new GunTuning { fires = true, aimAtShip = true, firstDelay = 0.4f, interval = 1.7f, shotsPerBurst = 3, shotGap = 0.26f, windup = 0.4f, maxBursts = 2, shotVariant = 2 },
            },
        };

        static EnemyProfile[] MediumProfiles() => new[]
        {
            new EnemyProfile   // M_1 — tracks the ship's Y, locks the lane, then fires a horizontal laser
            {
                name = "M_1 · Rastreador laser", pattern = MovementPattern.TrackerLaser, health = 24, scoreValue = 400,
                holdFromRightEdge = 4.2f, enterSeconds = 1.25f, holdSeconds = 10.5f, bobAmplitude = 0f, bobFrequency = 0f, leaveAcceleration = 4.5f,
                gun = new GunTuning { fires = false },
                trackingLaser = new TrackingLaserTuning
                {
                    enabled = true, followSpeed = 4.8f, trackingSeconds = 1.25f, chargeSeconds = 0.9f,
                    fireSeconds = 0.34f, recoverySeconds = 1.15f, beamWidth = 0.48f, damage = 1, bursts = 3,
                },
            },
            new EnemyProfile   // M_2 — slower than S_3, faster than M_3; bursts of heavy shots
            {
                name = "M_2 · Rajada", pattern = MovementPattern.Hover, health = 18, scoreValue = 300,
                holdFromRightEdge = 4.5f, enterSeconds = 1.25f, holdSeconds = 6.5f, bobAmplitude = 1.4f, bobFrequency = 0.5f, leaveAcceleration = 6f,
                gun = new GunTuning { fires = true, aimAtShip = true, firstDelay = 0.65f, interval = 1.7f, shotsPerBurst = 4, shotGap = 0.13f, windup = 0.45f, shotVariant = 3 },
            },
            new EnemyProfile   // M_3 — the slowest; a shotgun blast, and it sidesteps shots it sees coming
            {
                name = "M_3 · Escopeta", pattern = MovementPattern.Hover, health = 18, scoreValue = 350,
                holdFromRightEdge = 4.5f, enterSeconds = 1.6f, holdSeconds = 7.5f, bobAmplitude = 0.7f, bobFrequency = 0.3f, leaveAcceleration = 4f,
                gun = new GunTuning { fires = true, aimAtShip = true, firstDelay = 0.8f, interval = 2.3f, shotsPerBurst = 7, spread = 7f, windup = 0.55f, shotVariant = 4 },
                dodge = new DodgeTuning { dodges = true, senseRadius = 1.3f, lookahead = 0.45f, distance = 2.4f, seconds = 0.32f, cooldown = 2.2f },
            },
        };

        static EnemyProfile[] InterceptorProfiles() => new[]
        {
            new EnemyProfile
            {
                name = "W_1 · Batedor", pattern = MovementPattern.Serpentine, health = 3, scoreValue = 130,
                speed = 8.8f, waveAmplitude = 1.2f, waveFrequency = 0.62f,
                gun = new GunTuning { fires = true, aimAtShip = true, firstDelay = 0.5f, interval = 2.4f, shotsPerBurst = 1, windup = 0.3f, maxBursts = 2, shotVariant = 1 },
            },
            new EnemyProfile
            {
                name = "W_2 · Tecelão", pattern = MovementPattern.Serpentine, health = 3, scoreValue = 160,
                speed = 7.7f, waveAmplitude = 2.15f, waveFrequency = 0.48f,
                gun = new GunTuning { fires = true, aimAtShip = false, firstDelay = 0.35f, interval = 1.8f, shotsPerBurst = 2, shotGap = 0.18f, windup = 0.3f, maxBursts = 3, shotVariant = 1 },
            },
            new EnemyProfile
            {
                name = "W_3 · Agulha", pattern = MovementPattern.Serpentine, health = 4, scoreValue = 190,
                speed = 6.6f, waveAmplitude = 0.85f, waveFrequency = 1.05f,
                gun = new GunTuning { fires = true, aimAtShip = true, firstDelay = 0.55f, interval = 1.65f, shotsPerBurst = 3, spread = 8f, windup = 0.42f, maxBursts = 3, shotVariant = 2 },
            },
        };

        static void Apply(string prefabName, Projectile[] shots, EnemyProfile[] profiles, bool overwriteProfiles = false)
        {
            string path = $"{EnemyDir}/{prefabName}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var enemy = root.GetComponent<Enemy>();
                var so = new SerializedObject(enemy);
                if (overwriteProfiles || so.FindProperty("profiles").arraySize == 0)
                {
                    // EditorJsonUtility wraps a component's fields under its type name.
                    EditorJsonUtility.FromJsonOverwrite("{\"MonoBehaviour\":" + JsonUtility.ToJson(new ProfileList { profiles = profiles }) + "}", enemy);
                    Debug.Log($"[EnemyVariantsBootstrap] {prefabName}: {profiles.Length} profiles.");
                }

                var gun = new SerializedObject(root.GetComponent<EnemyGun>());
                var list = gun.FindProperty("shotVariants");
                list.arraySize = shots.Length;
                for (int i = 0; i < shots.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = shots[i];
                gun.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void RebuildProgressiveEncounter()
        {
            var schedule = AssetDatabase.LoadAssetAtPath<WaveSchedule>(WavePath);
            var small = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyDir}/Enemy_S.prefab")?.GetComponent<Enemy>();
            var medium = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyDir}/Enemy_M.prefab")?.GetComponent<Enemy>();
            var interceptor = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyDir}/Enemy_W.prefab")?.GetComponent<Enemy>();
            if (schedule == null || small == null || medium == null || interceptor == null)
                return;

            schedule.warningSeconds = 0.85f;
            schedule.clearDelay = 2.75f;
            schedule.waves = new List<WaveSchedule.Wave>
            {
                EW("Aquecimento: velozes pelo alto", 3f, small, 2, 4, 0.4f, 1.45f),
                EW("Resposta: velozes por baixo", 7f, small, 2, 4, -0.45f, 1.45f),
                EW("Tecelões: senoides largas", 11f, interceptor, 2, 4, 0.05f, 1.35f),
                EW("Primeiro cerco: rajadas em dois níveis", 15f, small, 3, 3, 0.62f, 1.45f, -0.62f),
                EW("Primeiro artilheiro pesado", 19f, medium, 2, 1, 0.05f, 0f),
                EW("Agulhas sob fogo do artilheiro", 22f, interceptor, 3, 5, -0.35f, 1.2f),
                EW("Kamikazes cortam a faixa superior", 26f, small, 1, 4, 0.55f, 1.35f),
                EW("Escopeteiros em pinça", 29f, medium, 3, 1, 0.52f, 0f, -0.52f),
                EW("Tesoura de tecelões", 33f, interceptor, 2, 3, 0.52f, 1.3f, -0.52f),
                EW("NOVO: rastreador laser", 37f, medium, 1, 1, -0.2f, 0f),
                EW("Escolta rápida durante a mira", 39.5f, small, 2, 5, 0.55f, 1.25f),
                EW("Parede móvel de agulhas", 44f, interceptor, 3, 6, 0f, 1.15f),
                EW("Artilheiros pesados em pinça", 48f, medium, 2, 1, 0.5f, 0f, -0.5f),
                EW("Kamikazes em duas linhas", 51f, small, 1, 3, 0.65f, 1.2f, -0.65f),
                EW("Clímax I: tecelões em duas linhas", 56f, interceptor, 2, 4, 0.42f, 1.1f, -0.42f),
                EW("Rastreador alto: mude de faixa", 60f, medium, 1, 1, 0.55f, 0f),
                EW("Rajadas ocupam a faixa baixa", 62f, small, 3, 4, -0.55f, 1.25f),
                EW("Agulhas apertam o centro", 66f, interceptor, 3, 5, 0f, 1.1f),
                EW("Escopeteiro com tecelões", 70f, medium, 3, 1, 0.48f, 0f),
                EW("Tecelões pela faixa oposta", 71.5f, interceptor, 2, 5, -0.48f, 1.05f),
                EW("Clímax II: rastreador baixo", 76f, medium, 1, 1, -0.58f, 0f),
                EW("Kamikazes cruzam a mira", 78.5f, small, 1, 2, 0.62f, 1.1f, -0.62f),
                EW("Última parede: agulhas em dois níveis", 82f, interceptor, 3, 4, 0.38f, 1.05f, -0.38f),
            };
            EditorUtility.SetDirty(schedule);
        }

        static WaveSchedule.Wave EW(string label, float time, Enemy enemy, int variant, int count,
            float height, float spacing, float? secondRow = null) => new()
        {
            label = label,
            time = time,
            enemy = enemy,
            variant = variant,
            count = count,
            height = height,
            spacing = spacing,
            secondRow = secondRow.HasValue,
            secondRowHeight = secondRow ?? height,
        };

        static void TuneGuardian()
        {
            const string path = "Assets/_Project/Prefabs/Boss/Guardian.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var boss = new SerializedObject(root.GetComponent<GuardianBoss>());
                Float(boss, "firstFighterCall", 4.5f); Float(boss, "fighterCallSeconds", 10f);
                Int(boss, "fightersPerCall", 1); Int(boss, "fighterCap", 3);
                Float(boss, "firstGunshipCall", 9f); Float(boss, "gunshipCallSeconds", 18f);
                Float(boss, "moveTelegraphSeconds", 0.64f); Float(boss, "dashSeconds", 0.5f);
                Float(boss, "moveRecoverySeconds", 0.42f); Float(boss, "furySpeedUp", 0.23f);
                Float(boss, "attackTelegraphSeconds", 0.55f); Float(boss, "fanInterval", 1.2f);
                Int(boss, "fanShots", 6); Float(boss, "fanSpread", 12f);
                Float(boss, "burstInterval", 1.1f); Int(boss, "burstShots", 6);
                Int(boss, "wallRows", 10); Float(boss, "wallGap", 3.2f);
                Float(boss, "ringInterval", 1.2f); Int(boss, "ringShots", 16);
                Float(boss, "ringGapDegrees", 38f); Float(boss, "lastChanceInterval", 0.96f);
                Float(boss, "frenzyInterval", 0.82f); Float(boss, "spiralSeconds", 1.35f);
                Float(boss, "spiralStep", 0.13f); Float(boss, "partBreakBreather", 1f);
                Float(boss, "laserSuppressionAfterPattern", 1.4f); Float(boss, "reinforcementRecovery", 1.2f);
                boss.ApplyModifiedPropertiesWithoutUndo();

                var sweep = new SerializedObject(root.GetComponentInChildren<SweepLaser>(true));
                Float(sweep, "sweepSeconds", 1.7f); Float(sweep, "restSeconds", 1.9f); Float(sweep, "warningSeconds", 1f);
                sweep.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void Float(SerializedObject target, string field, float value) => target.FindProperty(field).floatValue = value;
        static void Int(SerializedObject target, string field, int value) => target.FindProperty(field).intValue = value;

        // The interceptor shares the small models but is not a kamikaze: it picks only from models 2 and 3.
        static void RestrictInterceptorModels()
        {
            string path = $"{EnemyDir}/Enemy_W.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so = new SerializedObject(root.GetComponent<ModelVariants>());
                var allowed = so.FindProperty("randomFrom");
                if (allowed.arraySize == 0)
                {
                    allowed.arraySize = 2;
                    allowed.GetArrayElementAtIndex(0).intValue = 1;
                    allowed.GetArrayElementAtIndex(1).intValue = 2;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- shots

        static Projectile BuildShot(string name, float speed, float lifetime, float size, Color? color)
        {
            string path = $"{ShotDir}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing.GetComponent<Projectile>();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>($"{ShotDir}/Shot_Enemy.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var shot = new SerializedObject(instance.GetComponent<Projectile>());
                shot.FindProperty("speed").floatValue = speed;
                shot.FindProperty("lifetime").floatValue = lifetime;
                shot.ApplyModifiedPropertiesWithoutUndo();

                float k = size / 0.38f;
                var visual = instance.transform.Find("Visual");
                visual.localScale = Vector3.one * size;
                if (instance.GetComponent<SphereCollider>() is { } sphere)
                    sphere.radius *= Mathf.Max(0.7f, k);
                if (color.HasValue)
                    visual.GetComponent<Renderer>().sharedMaterial = ShotMaterial(name, color.Value);

                var saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
                return saved.GetComponent<Projectile>();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        static Material ShotMaterial(string shotName, Color color)
        {
            string path = $"{MaterialDir}/M_{shotName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var source = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/M_Shot_Enemy.mat");
            var material = new Material(source) { name = $"M_{shotName}" };
            material.SetColor("_BaseColor", color * 1.5f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------------------------------------------------------------- the encounter

        // (time of the wave, model 1–3 or 0 for a random one, count or 0 to keep, new label)
        static readonly (float time, int variant, int count, string label)[] WaveVariants =
        {
            (3f,    2, 0, "Aquecimento: 4 caças rápidos (S_2) no alto"),
            (7f,    2, 0, "Resposta: 4 caças rápidos (S_2) embaixo"),
            (11f,   0, 0, null),
            (15f,   3, 0, "Primeiro cerco: caças de rajada (S_3) em dois níveis"),
            (19f,   2, 0, "Primeiro artilheiro de rajada (M_2)"),
            (22f,   0, 0, null),
            (26f,   1, 0, "Kamikazes (S_1) cortam a faixa superior"),
            (29f,   3, 0, "Dupla de escopeteiros (M_3)"),
            (33f,   0, 0, null),
            (37f,   1, 0, "Tanque (M_1) de contenção"),
            (38.5f, 2, 0, "Escolta de 5 caças rápidos (S_2) sobreposta"),
            (43f,   0, 0, null),
            (47f,   2, 0, "Artilheiros de rajada (M_2) em pinça"),
            (49f,   1, 3, "Kamikazes (S_1) finais em duas linhas"),
            (54f,   0, 0, null),
        };

        static void AssignWaveVariants()
        {
            var schedule = AssetDatabase.LoadAssetAtPath<WaveSchedule>(WavePath);
            if (schedule == null)
            {
                Debug.LogWarning("[EnemyVariantsBootstrap] Encounter01_PrimeiroCiclo not found; waves left as they are.");
                return;
            }

            var so = new SerializedObject(schedule);
            var waves = so.FindProperty("waves");
            foreach (var plan in WaveVariants)
            {
                for (int i = 0; i < waves.arraySize; i++)
                {
                    var wave = waves.GetArrayElementAtIndex(i);
                    if (!Mathf.Approximately(wave.FindPropertyRelative("time").floatValue, plan.time))
                        continue;
                    var variant = wave.FindPropertyRelative("variant");
                    if (variant.intValue != 0 || plan.variant == 0)
                        break;
                    variant.intValue = plan.variant;
                    if (plan.count > 0)
                        wave.FindPropertyRelative("count").intValue = plan.count;
                    if (plan.label != null)
                        wave.FindPropertyRelative("label").stringValue = plan.label;
                    break;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(schedule);
        }
    }
}
