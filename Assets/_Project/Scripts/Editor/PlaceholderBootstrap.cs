using System.Collections.Generic;
using System.IO;
using OneMoreTime.Gameplay.Boss;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Gadgets;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Session;
using OneMoreTime.Gameplay.Ship;
using OneMoreTime.Gameplay.Traps;
using OneMoreTime.Gameplay.World;
using OneMoreTime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OneMoreTime.EditorTools
{
    /// <summary>
    /// Builds the placeholder content of the prototype: materials, prefabs, data assets, the HUD
    /// panel and the Arena_01 scene. Everything is made of primitives so mechanics can be validated
    /// before the art arrives (GDD 10). Idempotent: existing assets are kept, never overwritten —
    /// delete an asset to have it rebuilt. Content added after the first build (gadget, traps, boss,
    /// stage) is added to existing prefabs and to the scene only where it is missing.
    /// </summary>
    public static class PlaceholderBootstrap
    {
        const string Root = "Assets/_Project";
        const string MaterialDir = Root + "/Art/Placeholder/Materials";
        const string PrefabDir = Root + "/Prefabs";
        const string DataDir = Root + "/Data";
        const string UiDir = Root + "/UI";
        const string ShipArtDir = Root + "/Art/Ships";
        public const string ScenePath = Root + "/Scenes/Arena_01.unity";

        const int LayerPlayer = 8, LayerPlayerShot = 9, LayerEnemy = 10, LayerEnemyShot = 11, LayerEcho = 12;

        static readonly Color Background = new(0.039f, 0.059f, 0.122f);
        static readonly Color Cyan = new(0.21f, 0.89f, 1f);
        static readonly Color Violet = new(0.69f, 0.55f, 1f);
        static readonly Color Orange = new(1f, 0.54f, 0.24f);
        static readonly Color Amber = new(1f, 0.79f, 0.3f);

        [MenuItem("One More Time/Bootstrap/Build Placeholder Content", priority = 0)]
        public static void BuildAll()
        {
            ConfigurePhysicsLayers();
            var m = BuildMaterials();
            var p = BuildPrefabs(m);
            UpgradePrefabs(m, p);
            var echoConfig = LoadOrCreate($"{DataDir}/EchoConfig.asset", () => ScriptableObject.CreateInstance<EchoConfig>());
            var schedule = LoadOrCreate($"{DataDir}/Waves/Encounter01_PrimeiroCiclo.asset", () => BuildEncounter01(p));
            UpgradeEncounter01(schedule, p);
            var panel = BuildPanelSettings();
            BuildScene(m, p, echoConfig, schedule, panel);
            WireStage();
            AssetDatabase.SaveAssets();
            Debug.Log("[One More Time] Placeholder content ready: " + ScenePath);
        }

        // ---------- physics ----------

        [MenuItem("One More Time/Bootstrap/Configure Physics Layers", priority = 20)]
        public static void ConfigurePhysicsLayers()
        {
            int[] all = { LayerPlayer, LayerPlayerShot, LayerEnemy, LayerEnemyShot, LayerEcho };
            foreach (var a in all)
                foreach (var b in all)
                    Physics.IgnoreLayerCollision(a, b, true);

            // Only these pairs ever meet: shots against the other side, and enemies ramming the ship.
            Physics.IgnoreLayerCollision(LayerPlayerShot, LayerEnemy, false);
            Physics.IgnoreLayerCollision(LayerEnemyShot, LayerPlayer, false);
            Physics.IgnoreLayerCollision(LayerEnemy, LayerPlayer, false);
            // The echo touches nothing: no collision with the ship, no damage (GDD 02).
            AssetDatabase.SaveAssets();
        }

        // ---------- materials ----------

        sealed class Materials
        {
            public Material Ship, Echo, Enemy, EnemyMedium, ShotShip, ShotEcho, Warning, Rock, Ground;
            public Material ShotEnemy, Laser, TrapMetal, Barrier, Switch, Blast, BossHull, BossCore, Shield;
        }

        static Materials BuildMaterials()
        {
            const string lit = "Universal Render Pipeline/Lit";
            const string unlit = "Universal Render Pipeline/Unlit";
            return new Materials
            {
                Ship = Mat("M_Ship_Cyan", lit, Cyan, emission: Cyan * 0.6f),
                Echo = Mat("M_Echo_Violet", unlit, new Color(Violet.r, Violet.g, Violet.b, 0.45f), transparent: true),
                Enemy = Mat("M_Enemy_Orange", lit, Orange, emission: Orange * 0.35f),
                EnemyMedium = Mat("M_Enemy_Medium", lit, new Color(0.86f, 0.22f, 0.3f), emission: new Color(0.9f, 0.15f, 0.2f) * 0.35f),
                ShotShip = Mat("M_Shot_Ship", unlit, Cyan * 1.6f),
                ShotEcho = Mat("M_Shot_Echo", unlit, Violet * 1.15f),
                Warning = Mat("M_Warning", unlit, Amber * 1.3f),
                Rock = Mat("M_Backdrop_Rock", lit, new Color(0.1f, 0.13f, 0.24f), smoothness: 0.15f),
                Ground = Mat("M_Backdrop_Ground", lit, new Color(0.035f, 0.05f, 0.1f), smoothness: 0f),
                ShotEnemy = Mat("M_Shot_Enemy", unlit, new Color(1f, 0.38f, 0.24f) * 1.5f),
                Laser = Mat("M_Trap_Laser", unlit, new Color(1f, 0.3f, 0.22f) * 1.6f),
                TrapMetal = Mat("M_Trap_Metal", lit, new Color(0.2f, 0.23f, 0.33f), smoothness: 0.4f),
                Barrier = Mat("M_Trap_Barrier", lit, new Color(0.62f, 0.24f, 0.12f), emission: Orange * 0.25f),
                Switch = Mat("M_Trap_Switch", unlit, new Color(0.3f, 0.36f, 0.52f)),   // tinted at runtime by SwitchPad
                Blast = Mat("M_Blast", unlit, new Color(Amber.r, Amber.g, Amber.b, 0.45f), transparent: true),
                BossHull = Mat("M_Boss_Hull", lit, new Color(0.15f, 0.17f, 0.27f), smoothness: 0.35f),
                BossCore = Mat("M_Boss_Core", lit, new Color(1f, 0.32f, 0.26f), emission: new Color(1f, 0.25f, 0.2f) * 0.9f),
                Shield = Mat("M_Shield", unlit, new Color(Amber.r, Amber.g, Amber.b, 0.22f), transparent: true),
            };
        }

        static Material Mat(string name, string shaderName, Color color, Color? emission = null, bool transparent = false, float? smoothness = null)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.InvalidOperationException($"Shader not found: {shaderName}");
            var m = new Material(shader) { name = name };
            m.SetColor("_BaseColor", color);
            if (smoothness.HasValue)
                m.SetFloat("_Smoothness", smoothness.Value);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            EnsureFolder(MaterialDir);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---------- prefabs ----------

        sealed class Prefabs
        {
            public GameObject Ship, Echo, EnemyS, EnemyM, ShotShip, ShotEcho, Warning, Pillar, Rock;
            public GameObject ShotEnemy, MineShip, MineEcho, LaserGate, Barrier, Switch, TrapSection, Guardian;
        }

        static Prefabs BuildPrefabs(Materials m)
        {
            var p = new Prefabs();
            p.ShotShip = Prefab($"{PrefabDir}/Projectiles/Shot_Ship.prefab", () => BuildShot("Shot_Ship", m.ShotShip));
            p.ShotEcho = Prefab($"{PrefabDir}/Projectiles/Shot_Echo.prefab", () => BuildShot("Shot_Echo", m.ShotEcho));
            p.Echo = Prefab($"{PrefabDir}/Echo/EchoGhost.prefab", () => BuildEchoGhost(m, p.ShotEcho));
            p.Ship = Prefab($"{PrefabDir}/Ship/PlayerShip.prefab", () => BuildShip(m, p.ShotShip, p.Echo));
            p.Warning = Prefab($"{PrefabDir}/World/WaveWarning.prefab", () => BuildWarning(m.Warning));
            p.Pillar = Prefab($"{PrefabDir}/World/Prop_Pillar.prefab", () => BuildProp("Prop_Pillar", PrimitiveType.Cube, m.Rock));
            p.Rock = Prefab($"{PrefabDir}/World/Prop_Rock.prefab", () => BuildProp("Prop_Rock", PrimitiveType.Sphere, m.Rock));
            p.ShotEnemy = Prefab($"{PrefabDir}/Projectiles/Shot_Enemy.prefab", () => BuildEnemyShot(m.ShotEnemy));
            p.EnemyS = Prefab($"{PrefabDir}/Enemies/Enemy_S.prefab", () => BuildEnemyS(m, p.ShotEnemy));
            p.EnemyM = Prefab($"{PrefabDir}/Enemies/Enemy_M.prefab", () => BuildEnemyM(m, p.ShotEnemy));
            p.MineShip = Prefab($"{PrefabDir}/Gadgets/TimeMine_Ship.prefab", () => BuildMine("TimeMine_Ship", m.Ship, m.Blast));
            p.MineEcho = Prefab($"{PrefabDir}/Gadgets/TimeMine_Echo.prefab", () => BuildMine("TimeMine_Echo", m.Echo, m.Blast));
            p.LaserGate = Prefab($"{PrefabDir}/Traps/LaserGate.prefab", () => BuildLaserGate(m));
            p.Barrier = Prefab($"{PrefabDir}/Traps/MovingBarrier.prefab", () => BuildBarrier(m));
            p.Switch = Prefab($"{PrefabDir}/Traps/SwitchPad.prefab", () => BuildSwitch(m));
            p.TrapSection = Prefab($"{PrefabDir}/Traps/TrapSection_Corredor.prefab", () => BuildTrapSection(m, p));
            p.Guardian = Prefab($"{PrefabDir}/Boss/Guardian.prefab", () => BuildGuardian(m, p.ShotEnemy));
            return p;
        }

        // Prefabs built before a feature existed get it here, in place, so scene instances and waves keep their links:
        // the gadget on the ship, the model variants on the enemies.
        static void UpgradePrefabs(Materials m, Prefabs p)
        {
            EditPrefab(AssetDatabase.GetAssetPath(p.Ship), root =>
            {
                bool changed = false;
                var gadget = root.GetComponent<ShipGadget>();
                if (gadget == null)
                {
                    gadget = root.AddComponent<ShipGadget>();
                    changed = true;
                }
                changed |= SetIfNull(gadget, "mine", p.MineShip.GetComponent<TimeMine>());
                changed |= SetIfNull(root.GetComponent<EchoAbility>(), "gadget", gadget);
                return changed;
            });
            EditPrefab(AssetDatabase.GetAssetPath(p.Echo), root =>
                SetIfNull(root.GetComponent<EchoGhost>(), "mine", p.MineEcho.GetComponent<TimeMine>()));
            EditPrefab(AssetDatabase.GetAssetPath(p.EnemyS), root => AddMissingVariants(root, EnemySModels, EnemySScale, m.Enemy));
            EditPrefab(AssetDatabase.GetAssetPath(p.EnemyM), root => AddMissingVariants(root, EnemyMModels, EnemyMScale, m.EnemyMedium));
        }

        static bool AddMissingVariants(GameObject root, string[] modelPaths, float scale, Material mat)
        {
            if (root.GetComponent<ModelVariants>() != null)
                return false;
            ModelVariantsOf(root, modelPaths, scale, mat);
            return true;
        }

        static GameObject BuildShip(Materials m, GameObject shot, GameObject echo)
        {
            var root = new GameObject("PlayerShip");
            var visual = ShipVisual(root.transform, m.Ship);
            AddKinematicBody(root);
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(1.4f, 0.5f, 1f);

            var health = root.AddComponent<Health>();
            SetEnum(health, "faction", (int)Faction.Player);
            SetInt(health, "maxHealth", 3);
            SetFloat(health, "invulnerabilitySeconds", 1.2f);

            var motor = root.AddComponent<ShipMotor>();
            Set(motor, "visual", visual);
            var aim = root.AddComponent<ShipAim>();
            Set(aim, "motor", motor);
            var weapon = root.AddComponent<ShipWeapon>();
            Set(weapon, "projectile", shot.GetComponent<Projectile>());
            Set(weapon, "aim", aim);
            var ability = root.AddComponent<EchoAbility>();
            Set(ability, "motor", motor);
            Set(ability, "weapon", weapon);
            Set(ability, "echoPrefab", echo.GetComponent<EchoGhost>());

            SetLayer(root, LayerPlayer);
            return root;
        }

        static GameObject BuildEchoGhost(Materials m, GameObject shot)
        {
            var root = new GameObject("EchoGhost");
            var visual = ShipVisual(root.transform, m.Echo);
            var ghost = root.AddComponent<EchoGhost>();
            Set(ghost, "projectile", shot.GetComponent<Projectile>());
            Set(ghost, "visual", visual);
            SetArray(ghost, "fadeRenderers", visual.GetComponentsInChildren<Renderer>());
            SetLayer(root, LayerEcho);
            return root;
        }

        // Placeholder silhouette: fuselage, wings and a fin, pointing +x.
        static Transform ShipVisual(Transform parent, Material mat)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(parent, false);
            Part(visual, PrimitiveType.Capsule, "Fuselage", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.45f, 0.8f, 0.45f), mat);
            Part(visual, PrimitiveType.Cube, "Wings", new Vector3(-0.2f, 0f, 0f), Vector3.zero, new Vector3(0.6f, 0.06f, 1.5f), mat);
            Part(visual, PrimitiveType.Cube, "Fin", new Vector3(-0.55f, 0.25f, 0f), new Vector3(0f, 0f, -20f), new Vector3(0.35f, 0.35f, 0.06f), mat);
            return visual;
        }

        // Enemies use the designer's blockout models (Art/Ships). Their nose points to -Z: turn them to face
        // the ship (-X), with the same small roll toward the camera as the player ship.
        static readonly Quaternion EnemyFacing = Quaternion.AngleAxis(-15f, Vector3.right) * Quaternion.Euler(0f, 90f, 0f);

        // The designer's variants of each enemy (GDD 04), in his numbering; the first S export is the S_2.
        static readonly string[] EnemySModels =
        {
            $"{ShipArtDir}/Enemie_S_1_BlockOut.fbx", $"{ShipArtDir}/Enemie_S_BlockOut.fbx", $"{ShipArtDir}/Enemie_S_3_BlockOut.fbx",
        };
        static readonly string[] EnemyMModels =
        {
            $"{ShipArtDir}/Enemie_M_1_BlockOut.fbx", $"{ShipArtDir}/Enemie_M_2_BlockOut.fbx", $"{ShipArtDir}/Enemie_M_3_BlockOut.fbx",
        };
        const float EnemySScale = 0.9f, EnemyMScale = 1f;

        // Small fighter (GDD 04): comes in a line, swoops to the ship's height and fires one aimed shot.
        static GameObject BuildEnemyS(Materials m, GameObject shot)
        {
            var root = BuildEnemy("Enemy_S", EnemySModels, EnemySScale, m.Enemy, m.Warning,
                MovementPattern.Swoop, hp: 1, score: 100, shot);
            var gun = root.GetComponent<EnemyGun>();
            SetFloat(gun, "firstDelay", 0.35f);
            SetFloat(gun, "windup", 0.35f);
            SetInt(gun, "shotsPerBurst", 1);
            SetInt(gun, "maxBursts", 1);
            return root;
        }

        // Medium gunship (GDD 04): holds a column on the right and fires aimed fans of three.
        static GameObject BuildEnemyM(Materials m, GameObject shot)
        {
            var root = BuildEnemy("Enemy_M", EnemyMModels, EnemyMScale, m.EnemyMedium, m.Warning,
                MovementPattern.Hover, hp: 20, score: 300, shot);
            var gun = root.GetComponent<EnemyGun>();
            SetFloat(gun, "firstDelay", 0.8f);
            SetFloat(gun, "interval", 1.8f);
            SetFloat(gun, "windup", 0.45f);
            SetInt(gun, "shotsPerBurst", 3);
            SetFloat(gun, "spread", 12f);
            return root;
        }

        static GameObject BuildEnemy(string name, string[] modelPaths, float scale, Material mat, Material warningMat,
            MovementPattern pattern, int hp, int score, GameObject shot)
        {
            var root = new GameObject(name);
            var visual = Visual(root.transform);
            AddKinematicBody(root);
            var col = root.AddComponent<BoxCollider>();
            col.isTrigger = true;

            var health = root.AddComponent<Health>();
            SetEnum(health, "faction", (int)Faction.Enemy);
            SetInt(health, "maxHealth", hp);
            var enemy = root.AddComponent<Enemy>();
            SetEnum(enemy, "pattern", (int)pattern);
            Set(enemy, "visual", visual);
            SetInt(enemy, "scoreValue", score);
            var contact = root.AddComponent<ContactDamage>();
            SetEnum(contact, "faction", (int)Faction.Enemy);

            // The gun sits at the nose (placed per model below); the telegraph blinks there before each burst.
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            var telegraph = Part(muzzle, PrimitiveType.Sphere, "Telegraph", Vector3.zero, Vector3.zero, Vector3.one * 0.5f, warningMat, shadows: false);
            telegraph.AddComponent<Blinker>();
            var gun = root.AddComponent<EnemyGun>();
            Set(gun, "shot", shot.GetComponent<Projectile>());
            Set(gun, "muzzle", muzzle);
            Set(gun, "telegraph", telegraph);

            ModelVariantsOf(root, modelPaths, scale, mat);
            SetLayer(root, LayerEnemy);
            return root;
        }

        // Builds Visual/Model_1..n, one per model, and the ModelVariants that picks one per spawn. Each variant's
        // hitbox is a little smaller than its art, on the play plane; its muzzle sits just ahead of its nose.
        // The prefab shows the first variant.
        static void ModelVariantsOf(GameObject root, string[] modelPaths, float scale, Material mat)
        {
            var visual = root.transform.Find("Visual");
            for (int i = visual.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(visual.GetChild(i).gameObject);
            var looks = root.GetComponent<ModelVariants>();
            if (looks == null)
                looks = root.AddComponent<ModelVariants>();
            var so = new SerializedObject(looks);
            var list = Prop(so, "variants");
            list.arraySize = modelPaths.Length;
            for (int i = 0; i < modelPaths.Length; i++)
            {
                var art = Model(visual, modelPaths[i], EnemyFacing, scale, mat);
                var pivot = visual.GetChild(visual.childCount - 1);
                pivot.name = $"Model_{i + 1}";
                pivot.gameObject.SetActive(i == 0);
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("model").objectReferenceValue = pivot.gameObject;
                entry.FindPropertyRelative("hitbox").vector2Value = new Vector2(art.size.x * 0.85f, art.size.y * 0.85f);
                entry.FindPropertyRelative("muzzleX").floatValue = art.min.x - 0.1f;
            }
            var hitbox = root.GetComponent<BoxCollider>();
            var muzzle = root.transform.Find("Muzzle");
            Prop(so, "hitbox").objectReferenceValue = hitbox;
            Prop(so, "muzzle").objectReferenceValue = muzzle;
            so.ApplyModifiedPropertiesWithoutUndo();
            var first = list.GetArrayElementAtIndex(0);
            hitbox.size = new Vector3(first.FindPropertyRelative("hitbox").vector2Value.x, first.FindPropertyRelative("hitbox").vector2Value.y, 1f);
            muzzle.localPosition = new Vector3(first.FindPropertyRelative("muzzleX").floatValue, 0f, 0f);
            SetLayer(visual.gameObject, root.layer);
        }

        // Puts a model under "parent/Model" (the pivot keeps rotation, scale and the centring offset) and returns
        // its bounds in the parent's space. The model stays a linked FBX instance, so re-exports update it.
        // Falls back to a cube when the FBX is missing.
        static Bounds Model(Transform parent, string modelPath, Quaternion rotation, float scale, Material mat)
        {
            var pivot = new GameObject("Model").transform;
            pivot.SetParent(parent, false);
            pivot.localRotation = rotation;
            pivot.localScale = Vector3.one * scale;
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (fbx != null)
                PrefabUtility.InstantiatePrefab(fbx, pivot);
            else
                Part(pivot, PrimitiveType.Cube, "Missing model", Vector3.zero, Vector3.zero, Vector3.one, mat);

            var bounds = new Bounds();
            bool first = true;
            foreach (var r in pivot.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = mat;
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            var centre = parent.InverseTransformPoint(bounds.center);
            pivot.localPosition -= centre;
            return new Bounds(Vector3.zero, bounds.size);
        }

        static GameObject BuildShot(string name, Material mat)
        {
            var root = new GameObject(name);
            Part(root.transform, PrimitiveType.Capsule, "Visual", Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.16f, 0.32f, 0.16f), mat, shadows: false);
            AddKinematicBody(root);
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.22f;
            root.AddComponent<Projectile>();
            SetLayer(root, LayerPlayerShot);
            return root;
        }

        static GameObject BuildWarning(Material mat)
        {
            var root = new GameObject("WaveWarning");
            Part(root.transform, PrimitiveType.Cube, "Arrow", Vector3.zero, new Vector3(0f, 0f, 45f), new Vector3(0.45f, 0.45f, 0.1f), mat, shadows: false);
            root.AddComponent<Blinker>();
            return root;
        }

        static GameObject BuildProp(string name, PrimitiveType type, Material mat)
        {
            var root = new GameObject(name);
            Part(root.transform, type, "Mesh", Vector3.zero, Vector3.zero, Vector3.one, mat);
            return root;
        }

        static GameObject BuildEnemyShot(Material mat)
        {
            var root = new GameObject("Shot_Enemy");
            Part(root.transform, PrimitiveType.Sphere, "Visual", Vector3.zero, Vector3.zero, Vector3.one * 0.38f, mat, shadows: false);
            AddKinematicBody(root);
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.2f;
            var shot = root.AddComponent<Projectile>();
            SetFloat(shot, "speed", 8f);
            SetFloat(shot, "lifetime", 4.5f);
            SetLayer(root, LayerEnemyShot);
            return root;
        }

        // ---------- gadget ----------

        static GameObject BuildMine(string name, Material body, Material blastMat)
        {
            var root = new GameObject(name);
            var visual = Visual(root.transform);
            Part(visual, PrimitiveType.Sphere, "Core", Vector3.zero, Vector3.zero, Vector3.one * 0.42f, body);
            Part(visual, PrimitiveType.Cube, "Spike_A", Vector3.zero, new Vector3(0f, 0f, 45f), new Vector3(0.75f, 0.1f, 0.1f), body);
            Part(visual, PrimitiveType.Cube, "Spike_B", Vector3.zero, new Vector3(0f, 0f, -45f), new Vector3(0.75f, 0.1f, 0.1f), body);
            var blast = Part(root.transform, PrimitiveType.Sphere, "Blast", Vector3.zero, Vector3.zero, Vector3.one, blastMat, shadows: false);
            blast.SetActive(false);

            var mine = root.AddComponent<TimeMine>();
            Set(mine, "visual", visual);
            Set(mine, "blast", blast.transform);
            SetInt(mine, "targets", 1 << LayerEnemy);
            SetLayer(root, LayerPlayerShot);
            return root;
        }

        // ---------- traps ----------

        const float ScreenHeight = 14.8f;   // a little taller than the visible plane (GDD 08 camera)

        static GameObject BuildLaserGate(Materials m)
        {
            var root = new GameObject("LaserGate");
            var gate = root.AddComponent<LaserGate>();
            AddKinematicBody(root);
            var visual = Visual(root.transform);
            Part(visual, PrimitiveType.Cube, "Emitter_Top", new Vector3(0f, ScreenHeight * 0.5f, 0f), Vector3.zero, new Vector3(0.8f, 0.5f, 0.8f), m.TrapMetal);
            Part(visual, PrimitiveType.Cube, "Emitter_Bottom", new Vector3(0f, -ScreenHeight * 0.5f, 0f), Vector3.zero, new Vector3(0.8f, 0.5f, 0.8f), m.TrapMetal);

            var beam = new GameObject("Beam");
            beam.transform.SetParent(root.transform, false);
            var col = beam.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.35f, ScreenHeight, 1f);
            beam.AddComponent<ContactDamage>();
            Part(beam.transform, PrimitiveType.Cube, "Visual", Vector3.zero, Vector3.zero, new Vector3(0.22f, ScreenHeight, 0.22f), m.Laser, shadows: false);

            var warning = new GameObject("Warning");
            warning.transform.SetParent(root.transform, false);
            Part(warning.transform, PrimitiveType.Cube, "Visual", Vector3.zero, Vector3.zero, new Vector3(0.06f, ScreenHeight, 0.06f), m.Warning, shadows: false);
            warning.AddComponent<Blinker>();
            warning.SetActive(false);

            Set(gate, "beam", beam);
            Set(gate, "warning", warning);
            SetLayer(root, LayerEnemyShot);
            return root;
        }

        static GameObject BuildBarrier(Materials m)
        {
            var root = new GameObject("MovingBarrier");
            var barrier = root.AddComponent<MovingBarrier>();
            const float height = 8f;
            Transform Block(string name, float side)
            {
                var block = new GameObject(name);
                block.transform.SetParent(root.transform, false);
                AddKinematicBody(block);
                var col = block.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.center = new Vector3(0f, side * height * 0.5f, 0f);
                col.size = new Vector3(1.2f, height, 1.2f);
                block.AddComponent<ContactDamage>();
                Part(Visual(block.transform), PrimitiveType.Cube, "Block", new Vector3(0f, side * height * 0.5f, 0f), Vector3.zero, new Vector3(1.2f, height, 1.2f), m.Barrier);
                return block.transform;
            }
            Set(barrier, "upper", Block("Upper", 1f));
            Set(barrier, "lower", Block("Lower", -1f));
            SetLayer(root, LayerEnemyShot);
            return root;
        }

        static GameObject BuildSwitch(Materials m)
        {
            var root = new GameObject("SwitchPad");
            var visual = Visual(root.transform);
            // Behind the gameplay plane, so the ship and the echo pass in front of it.
            var plate = Part(visual, PrimitiveType.Cube, "Plate", new Vector3(0f, 0f, 0.5f), Vector3.zero, new Vector3(1.8f, 1.4f, 0.1f), m.Switch, shadows: false);
            var lamp = Part(visual, PrimitiveType.Sphere, "Lamp", new Vector3(0f, 0f, 0.35f), Vector3.zero, Vector3.one * 0.35f, m.Switch, shadows: false);
            Part(visual, PrimitiveType.Cube, "Base", new Vector3(0f, -0.8f, 0.4f), Vector3.zero, new Vector3(2f, 0.2f, 0.6f), m.TrapMetal);
            var pad = root.AddComponent<SwitchPad>();
            SetArray(pad, "indicators", new Object[] { plate.GetComponent<Renderer>(), lamp.GetComponent<Renderer>() });
            return root;
        }

        // Encounter 2 (GDD 06): a cycling gate and a moving barrier that cross the ship as the screen
        // scrolls, then the scroll locks on a gate held off by a switch — the echo's job (GDD 05, puzzle 1).
        static GameObject BuildTrapSection(Materials m, Prefabs p)
        {
            var root = new GameObject("TrapSection_Corredor");
            var section = root.AddComponent<TrapSection>();
            SetFloat(section, "exitX", 7f);

            Nested(p.LaserGate, root.transform, "Gate_Ciclo", new Vector3(-26f, 0f, 0f));
            Nested(p.Barrier, root.transform, "Barreira", new Vector3(-16f, 0f, 0f));
            var padGo = Nested(p.Switch, root.transform, "Interruptor", new Vector3(1.5f, -5.6f, 0f));
            var gate = Nested(p.LaserGate, root.transform, "Gate_Interruptor", new Vector3(6f, 0f, 0f)).GetComponent<LaserGate>();
            var pad = padGo.GetComponent<SwitchPad>();
            SetBool(gate, "cycles", false);
            Set(gate, "holdOpenBy", pad);

            // Conduit from the switch to the gate's foot, lit with the switch: shows what it controls.
            var conduit = Part(root.transform, PrimitiveType.Cube, "Conduto", new Vector3(3.75f, -6.6f, 0.4f), Vector3.zero, new Vector3(4.5f, 0.12f, 0.12f), m.Switch, shadows: false);
            SetArray(pad, "indicators", new Object[]
            {
                padGo.transform.Find("Visual/Plate").GetComponent<Renderer>(),
                padGo.transform.Find("Visual/Lamp").GetComponent<Renderer>(),
                conduit.GetComponent<Renderer>(),
            });
            return root;
        }

        // ---------- boss ----------

        // O Guardião do Ciclo (GDD 06): armoured core, two generators, a sweeping laser.
        static GameObject BuildGuardian(Materials m, GameObject enemyShot)
        {
            var root = new GameObject("Guardian");
            AddKinematicBody(root);
            var visual = Visual(root.transform);
            Part(visual, PrimitiveType.Cube, "Hull", new Vector3(0.6f, 0f, 0.4f), Vector3.zero, new Vector3(2.6f, 7.2f, 3f), m.BossHull);
            Part(visual, PrimitiveType.Cylinder, "Ring", new Vector3(-0.9f, 0f, 0.3f), new Vector3(90f, 0f, 0f), new Vector3(2.8f, 0.15f, 2.8f), m.BossHull);
            Part(visual, PrimitiveType.Cube, "Arm_Upper", new Vector3(-0.4f, 4.6f, 0.2f), Vector3.zero, new Vector3(1.8f, 0.35f, 0.6f), m.BossHull);
            Part(visual, PrimitiveType.Cube, "Arm_Lower", new Vector3(-0.4f, -4.6f, 0.2f), Vector3.zero, new Vector3(1.8f, 0.35f, 0.6f), m.BossHull);

            // Armour: absorbs shots and hurts the ship on contact.
            var armor = new GameObject("Armor");
            armor.transform.SetParent(root.transform, false);
            var armorCol = armor.AddComponent<BoxCollider>();
            armorCol.isTrigger = true;
            armorCol.center = new Vector3(0.6f, 0f, 0f);
            armorCol.size = new Vector3(2.4f, 7f, 2f);
            var armorHealth = armor.AddComponent<Health>();
            SetEnum(armorHealth, "faction", (int)Faction.Enemy);
            SetBool(armorHealth, "shielded", true);
            armor.AddComponent<ContactDamage>();

            var core = new GameObject("Core");
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
            var coreCol = core.AddComponent<SphereCollider>();
            coreCol.isTrigger = true;
            coreCol.radius = 0.85f;
            var coreHealth = core.AddComponent<Health>();
            SetEnum(coreHealth, "faction", (int)Faction.Enemy);
            SetInt(coreHealth, "maxHealth", 120);
            SetBool(coreHealth, "shielded", true);
            Part(core.transform, PrimitiveType.Sphere, "Visual", Vector3.zero, Vector3.zero, Vector3.one * 1.6f, m.BossCore);
            var coreShield = Part(core.transform, PrimitiveType.Sphere, "Shield", Vector3.zero, Vector3.zero, Vector3.one * 2.5f, m.Shield, shadows: false);

            var generators = new Object[2];
            var generatorShields = new Object[2];
            for (int i = 0; i < 2; i++)
            {
                var g = new GameObject(i == 0 ? "Generator_Upper" : "Generator_Lower");
                g.transform.SetParent(root.transform, false);
                g.transform.localPosition = new Vector3(-1.4f, i == 0 ? 4.6f : -4.6f, 0f);
                var col = g.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = Vector3.one * 1.3f;
                var health = g.AddComponent<Health>();
                SetEnum(health, "faction", (int)Faction.Enemy);
                SetInt(health, "maxHealth", 12);
                Part(g.transform, PrimitiveType.Cube, "Visual", Vector3.zero, new Vector3(0f, 0f, 45f), Vector3.one, m.Enemy);
                generators[i] = health;
                generatorShields[i] = Part(g.transform, PrimitiveType.Sphere, "Shield", Vector3.zero, Vector3.zero, Vector3.one * 2f, m.Shield, shadows: false);
            }

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(-2.5f, 0f, 0f);

            var sweep = new GameObject("SweepLaser");
            sweep.transform.SetParent(root.transform, false);
            sweep.transform.localPosition = new Vector3(-1.8f, 6.6f, 0f);
            var laser = sweep.AddComponent<SweepLaser>();
            var beam = new GameObject("Beam");
            beam.transform.SetParent(sweep.transform, false);
            var beamCol = beam.AddComponent<BoxCollider>();
            beamCol.isTrigger = true;
            beamCol.center = new Vector3(-14f, 0f, 0f);
            beamCol.size = new Vector3(28f, 0.5f, 1f);
            beam.AddComponent<ContactDamage>();
            Part(beam.transform, PrimitiveType.Cube, "Visual", new Vector3(-14f, 0f, 0f), Vector3.zero, new Vector3(28f, 0.32f, 0.32f), m.Laser, shadows: false);
            beam.SetActive(false);
            var warning = new GameObject("Warning");
            warning.transform.SetParent(sweep.transform, false);
            Part(warning.transform, PrimitiveType.Cube, "Visual", new Vector3(-14f, 0f, 0f), Vector3.zero, new Vector3(28f, 0.06f, 0.06f), m.Warning, shadows: false);
            warning.AddComponent<Blinker>();
            warning.SetActive(false);
            Set(laser, "beam", beam);
            Set(laser, "warning", warning);

            var boss = root.AddComponent<GuardianBoss>();
            Set(boss, "core", coreHealth);
            SetArray(boss, "generators", generators);
            Set(boss, "coreShield", coreShield);
            SetArray(boss, "generatorShields", generatorShields);
            Set(boss, "muzzle", muzzle);
            Set(boss, "laser", laser);
            Set(boss, "shot", enemyShot.GetComponent<Projectile>());

            SetLayer(root, LayerEnemy);
            SetLayer(sweep, LayerEnemyShot);
            return root;
        }

        // ---------- data ----------

        static WaveSchedule BuildEncounter01(Prefabs p)
        {
            var s = ScriptableObject.CreateInstance<WaveSchedule>();
            FillEncounter01(s, p);
            return s;
        }

        // An encounter built before the S and M enemies existed (drones and ondulantes) is refilled in place.
        static void UpgradeEncounter01(WaveSchedule s, Prefabs p)
        {
            var small = p.EnemyS.GetComponent<Enemy>();
            var medium = p.EnemyM.GetComponent<Enemy>();
            foreach (var w in s.waves)
                if (w.enemy != small && w.enemy != medium)
                {
                    FillEncounter01(s, p);
                    EditorUtility.SetDirty(s);
                    Debug.Log("[One More Time] Encounter 1 rebuilt with the S and M enemies.");
                    return;
                }
        }

        static void FillEncounter01(WaveSchedule s, Prefabs p)
        {
            var small = p.EnemyS.GetComponent<Enemy>();
            var medium = p.EnemyM.GetComponent<Enemy>();
            s.warningSeconds = 0.8f;
            s.clearDelay = 4f;
            s.waves = new List<WaveSchedule.Wave>
            {
                // GDD 04, encounter 1. 0:00–0:10 is free flight, to learn moving and recording.
                W("4 caças em fila, no alto", 10f, small, 4, 0.35f, 1.6f),
                W("4 caças em fila, embaixo", 17f, small, 4, -0.4f, 1.6f),
                W("primeiro artilheiro: para à direita e dispara leques", 25f, medium, 1, 0f, 0f),
                W("caças em dois níveis", 35f, small, 3, 0.6f, 1.6f, -0.6f),
                W("dois artilheiros, em cima e embaixo", 44f, medium, 1, 0.5f, 0f, -0.5f),
                W("5 caças pelo meio", 55f, small, 5, 0f, 1.4f),
                W("artilheiro com escolta", 62f, medium, 1, -0.35f, 0f),
                W("escolta de caças", 63f, small, 4, 0.6f, 1.6f),
                W("caças finais em dois níveis", 73f, small, 4, 0.5f, 1.6f, -0.5f),
            };
        }

        static WaveSchedule.Wave W(string label, float time, Enemy enemy, int count, float height, float spacing, float? second = null)
        {
            return new WaveSchedule.Wave
            {
                label = label, time = time, enemy = enemy, count = count,
                height = height, spacing = spacing,
                secondRow = second.HasValue, secondRowHeight = second ?? height,
            };
        }

        static PanelSettings BuildPanelSettings()
        {
            return LoadOrCreate($"{UiDir}/HudPanelSettings.asset", () =>
            {
                var ps = ScriptableObject.CreateInstance<PanelSettings>();
                ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{UiDir}/HudTheme.tss");
                ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                ps.referenceResolution = new Vector2Int(1920, 1080);
                ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                ps.match = 0.5f;
                return ps;
            });
        }

        // ---------- scene ----------

        static void BuildScene(Materials m, Prefabs p, EchoConfig echoConfig, WaveSchedule schedule, PanelSettings panel)
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log($"[One More Time] {ScenePath} already exists; kept as is.");
                AddToBuild();
                return;
            }

            // NewScene unloads unused assets, so refetch everything just created by its path.
            AssetDatabase.SaveAssets();
            string echoPath = AssetDatabase.GetAssetPath(echoConfig), schedulePath = AssetDatabase.GetAssetPath(schedule), panelPath = AssetDatabase.GetAssetPath(panel);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            echoConfig = AssetDatabase.LoadAssetAtPath<EchoConfig>(echoPath);
            schedule = AssetDatabase.LoadAssetAtPath<WaveSchedule>(schedulePath);
            panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            m = BuildMaterials();
            p = BuildPrefabs(m);
            ConfigureEnvironment();

            // Systems
            var systems = Group("Systems");
            var input = Child<PlayerInputReader>(systems, "Input");
            Set(input, "controls", AssetDatabase.LoadAssetAtPath<InputActionAsset>($"{Root}/Input/OneMoreTimeControls.inputactions"));
            var pool = Child<ProjectilePool>(systems, "ProjectilePool");
            var spawner = Child<WaveSpawner>(systems, "WaveSpawner");
            var session = Child<GameSession>(systems, "GameSession");

            // Lighting
            var lighting = Group("Lighting");
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.transform.SetParent(lighting, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.color = new Color(0.85f, 0.9f, 1f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.transform.SetParent(lighting, false);
            volume.isGlobal = true;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");

            // Playfield: the scrolling frame with the camera and the ship
            var playfield = new GameObject("Playfield").AddComponent<Playfield>();
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>();
            cam.transform.SetParent(playfield.transform, false);
            cam.transform.localPosition = new Vector3(0f, 0f, -20f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 150f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Set(playfield, "view", cam);

            var ship = (GameObject)PrefabUtility.InstantiatePrefab(p.Ship, playfield.transform);
            ship.transform.localPosition = new Vector3(-8f, 0f, 0f);
            Set(ship.GetComponent<ShipMotor>(), "input", input);
            Set(ship.GetComponent<ShipMotor>(), "playfield", playfield);
            var weapon = ship.GetComponent<ShipWeapon>();
            Set(weapon, "input", input);
            Set(weapon, "playfield", playfield);
            Set(weapon, "pool", pool);
            var ability = ship.GetComponent<EchoAbility>();
            Set(ability, "config", echoConfig);
            Set(ability, "input", input);
            Set(ability, "playfield", playfield);
            Set(ability, "pool", pool);

            // World: 3D placeholder scenery behind the gameplay plane
            var world = Group("World");
            var ground = Part(world, PrimitiveType.Cube, "Ground", new Vector3(0f, -10f, 30f), Vector3.zero, new Vector3(400f, 1f, 70f), m.Ground);
            ground.transform.SetParent(playfield.transform, true);   // follows the scroll
            var backdrop = Child<BackdropScroller>(world, "Backdrop");
            Set(backdrop, "playfield", playfield);
            SetArray(backdrop, "props", new Object[] { p.Pillar, p.Rock });

            // Wiring of the systems
            Set(spawner, "schedule", schedule);
            Set(spawner, "playfield", playfield);
            Set(spawner, "warningMarker", p.Warning);
            Set(session, "input", input);
            Set(session, "player", ship.GetComponent<Health>());
            Set(session, "spawner", spawner);

            // UI
            var ui = Group("UI");
            var hudGo = new GameObject("HUD");
            hudGo.transform.SetParent(ui, false);
            var doc = hudGo.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UiDir}/Hud.uxml");
            var hud = hudGo.AddComponent<HudController>();
            Set(hud, "player", ship.GetComponent<Health>());
            Set(hud, "echo", ability);
            Set(hud, "session", session);

            EnsureFolder(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuild();
        }

        static void ConfigureEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.24f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Background;
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 95f;
        }

        static void AddToBuild()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // Adds the stage (waves → traps → Guardian), the combo tracker, the pixel view and the gadget links
        // to the scene, only where missing.
        static void WireStage()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var p = BuildPrefabs(BuildMaterials());   // refetch: opening a scene unloads unused assets

            var systems = RootGroup(scene, "Systems");
            var world = RootGroup(scene, "World");
            var input = Object.FindFirstObjectByType<PlayerInputReader>();
            var playfield = Object.FindFirstObjectByType<Playfield>();
            var pool = Object.FindFirstObjectByType<ProjectilePool>();
            var spawner = Object.FindFirstObjectByType<WaveSpawner>();
            var session = Object.FindFirstObjectByType<GameSession>();
            var hud = Object.FindFirstObjectByType<HudController>();
            var ability = Object.FindFirstObjectByType<EchoAbility>();
            var gadget = Object.FindFirstObjectByType<ShipGadget>();
            if (systems == null || world == null || input == null || playfield == null || pool == null || spawner == null
                || session == null || hud == null || ability == null || gadget == null)
            {
                Debug.LogError($"[One More Time] {ScenePath} is missing a base object; delete the scene to rebuild it.");
                return;
            }

            bool changed = false;
            var stage = Object.FindFirstObjectByType<StageDirector>();
            if (stage == null)
            {
                stage = Child<StageDirector>(systems, "StageDirector");
                changed = true;
            }
            changed |= SetIfNull(stage, "playfield", playfield);
            changed |= SetIfNull(stage, "waves", spawner);
            changed |= SetIfNull(stage, "ship", ability.transform);
            changed |= SetIfNull(stage, "echoes", ability);
            changed |= SetIfNull(stage, "pool", pool);
            changed |= SetIfNull(stage, "world", world);
            changed |= SetIfNull(stage, "trapSection", p.TrapSection.GetComponent<TrapSection>());
            changed |= SetIfNull(stage, "boss", p.Guardian.GetComponent<GuardianBoss>());
            changed |= SetIfNull(spawner, "pool", pool);
            changed |= SetIfNull(spawner, "ship", ability.transform);

            var combos = Object.FindFirstObjectByType<ComboTracker>();
            if (combos == null)
            {
                combos = Child<ComboTracker>(systems, "ComboTracker");
                changed = true;
            }
            changed |= SetIfNull(combos, "waves", spawner);
            changed |= SetIfNull(combos, "stage", stage);

            // The low-resolution look is an experiment (GDD 08): untick the component to turn it off.
            if (Object.FindFirstObjectByType<PixelView>() == null)
            {
                Child<PixelView>(systems, "PixelView");
                changed = true;
            }

            changed |= SetIfNull(gadget, "input", input);
            changed |= SetIfNull(gadget, "playfield", playfield);
            changed |= SetIfNull(gadget, "pool", pool);
            changed |= SetIfNull(session, "stage", stage);
            changed |= SetIfNull(hud, "gadget", gadget);
            changed |= SetIfNull(hud, "stage", stage);
            changed |= SetIfNull(session, "combos", combos);
            changed |= SetIfNull(hud, "combos", combos);
            changed |= SetIfNull(hud, "input", input);

            if (!changed)
                return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[One More Time] Stage wired into {ScenePath}.");
        }

        static Transform RootGroup(Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == name)
                    return go.transform;
            return null;
        }

        // ---------- helpers ----------

        static Transform Group(string name) => new GameObject(name).transform;

        static Transform Visual(Transform parent)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(parent, false);
            return visual;
        }

        static GameObject Nested(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            go.transform.localPosition = position;
            return go;
        }

        static T Child<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 euler, Vector3 scale, Material mat, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        static void AddKinematicBody(GameObject go)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
        }

        static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        static GameObject Prefab(string path, System.Func<GameObject> build)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;
            EnsureFolder(Path.GetDirectoryName(path));
            var go = build();
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        static void EditPrefab(string path, System.Func<GameObject, bool> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (edit(root))
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static T LoadOrCreate<T>(string path, System.Func<T> create) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            EnsureFolder(Path.GetDirectoryName(path));
            var asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static SerializedProperty Prop(SerializedObject so, string field)
        {
            var p = so.FindProperty(field);
            if (p == null)
                throw new System.ArgumentException($"{so.targetObject.GetType().Name} has no serialized field '{field}'.");
            return p;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Set a reference only if it is still empty. Returns true when it changed something.</summary>
        static bool SetIfNull(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = Prop(so, field);
            if (p.objectReferenceValue != null)
                return false;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static void SetArray(Object target, string field, IReadOnlyList<Object> values)
        {
            var so = new SerializedObject(target);
            var p = Prop(so, field);
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            Prop(so, field).enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
