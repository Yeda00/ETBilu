using System;
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
            var shots = new[]
            {
                // index in EnemyGun.shotVariants − 1 = GunTuning.shotVariant − 1
                BuildShot("Shot_Enemy_Fast", speed: 12.5f, lifetime: 4f, size: 0.26f, color: new Color(1f, 0.88f, 0.35f)),     // S_2: small, quick
                BuildShot("Shot_Enemy_Slow", speed: 8f, lifetime: 4.5f, size: 0.3f, color: new Color(1f, 0.55f, 0.16f)),        // S_3: short slow burst
                BuildShot("Shot_Enemy_Heavy", speed: 8.8f, lifetime: 4.5f, size: 0.5f, color: null),                            // M_2: burst of big ones
                BuildShot("Shot_Enemy_Pellet", speed: 10.5f, lifetime: 2.4f, size: 0.26f, color: new Color(1f, 0.32f, 0.82f)),  // M_3: shotgun pellets
            };

            Apply("Enemy_S", shots, SmallProfiles());
            Apply("Enemy_M", shots, MediumProfiles());
            RestrictInterceptorModels();
            AssignWaveVariants();
            AssetDatabase.SaveAssets();
            Debug.Log("[EnemyVariantsBootstrap] Enemy variants ready.");
        }

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
            new EnemyProfile   // M_1 — tanker: sits like scenery, goes off when the ship gets close
            {
                name = "M_1 · Tanque", pattern = MovementPattern.Hover, health = 30, scoreValue = 250,
                holdFromRightEdge = 5.5f, enterSeconds = 1.4f, holdSeconds = 11f, bobAmplitude = 0.25f, bobFrequency = 0.2f, leaveAcceleration = 3f,
                gun = new GunTuning { fires = false },
                blast = new BlastTuning { explodes = true, triggerRadius = 3.4f, fuseSeconds = 0.9f, autoSeconds = 0f, blastRadius = 3.8f, damage = 2 },
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

        static void Apply(string prefabName, Projectile[] shots, EnemyProfile[] profiles)
        {
            string path = $"{EnemyDir}/{prefabName}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var enemy = root.GetComponent<Enemy>();
                var so = new SerializedObject(enemy);
                if (so.FindProperty("profiles").arraySize == 0)
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
