using System.Collections.Generic;
using System.IO;
using OneMoreTime.Gameplay.Combat;
using OneMoreTime.Gameplay.Echo;
using OneMoreTime.Gameplay.Enemies;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Session;
using OneMoreTime.Gameplay.Ship;
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
    /// delete an asset to have it rebuilt.
    /// </summary>
    public static class PlaceholderBootstrap
    {
        const string Root = "Assets/_Project";
        const string MaterialDir = Root + "/Art/Placeholder/Materials";
        const string PrefabDir = Root + "/Prefabs";
        const string DataDir = Root + "/Data";
        const string UiDir = Root + "/UI";
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
            var echoConfig = LoadOrCreate($"{DataDir}/EchoConfig.asset", () => ScriptableObject.CreateInstance<EchoConfig>());
            var schedule = LoadOrCreate($"{DataDir}/Waves/Encounter01_PrimeiroCiclo.asset", () => BuildEncounter01(p));
            var panel = BuildPanelSettings();
            BuildScene(m, p, echoConfig, schedule, panel);
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
            public Material Ship, Echo, Enemy, Wave, ShotShip, ShotEcho, Warning, Rock, Ground;
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
                Wave = Mat("M_Enemy_Ondulante", lit, new Color(1f, 0.42f, 0.3f), emission: new Color(1f, 0.3f, 0.2f) * 0.35f),
                ShotShip = Mat("M_Shot_Ship", unlit, Cyan * 1.6f),
                ShotEcho = Mat("M_Shot_Echo", unlit, Violet * 1.15f),
                Warning = Mat("M_Warning", unlit, Amber * 1.3f),
                Rock = Mat("M_Backdrop_Rock", lit, new Color(0.1f, 0.13f, 0.24f), smoothness: 0.15f),
                Ground = Mat("M_Backdrop_Ground", lit, new Color(0.035f, 0.05f, 0.1f), smoothness: 0f),
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
            public GameObject Ship, Echo, Drone, Ondulante, ShotShip, ShotEcho, Warning, Pillar, Rock;
        }

        static Prefabs BuildPrefabs(Materials m)
        {
            var p = new Prefabs();
            p.ShotShip = Prefab($"{PrefabDir}/Projectiles/Shot_Ship.prefab", () => BuildShot("Shot_Ship", m.ShotShip));
            p.ShotEcho = Prefab($"{PrefabDir}/Projectiles/Shot_Echo.prefab", () => BuildShot("Shot_Echo", m.ShotEcho));
            p.Echo = Prefab($"{PrefabDir}/Echo/EchoGhost.prefab", () => BuildEchoGhost(m, p.ShotEcho));
            p.Ship = Prefab($"{PrefabDir}/Ship/PlayerShip.prefab", () => BuildShip(m, p.ShotShip, p.Echo));
            p.Drone = Prefab($"{PrefabDir}/Enemies/Drone.prefab", () => BuildEnemy("Drone", m.Enemy, MovementPattern.Straight, hp: 2, speed: 4f, diamond: false));
            p.Ondulante = Prefab($"{PrefabDir}/Enemies/Ondulante.prefab", () => BuildEnemy("Ondulante", m.Wave, MovementPattern.Sine, hp: 1, speed: 3.5f, diamond: true));
            p.Warning = Prefab($"{PrefabDir}/World/WaveWarning.prefab", () => BuildWarning(m.Warning));
            p.Pillar = Prefab($"{PrefabDir}/World/Prop_Pillar.prefab", () => BuildProp("Prop_Pillar", PrimitiveType.Cube, m.Rock));
            p.Rock = Prefab($"{PrefabDir}/World/Prop_Rock.prefab", () => BuildProp("Prop_Rock", PrimitiveType.Sphere, m.Rock));
            return p;
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
            var weapon = root.AddComponent<ShipWeapon>();
            Set(weapon, "projectile", shot.GetComponent<Projectile>());
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

        static GameObject BuildEnemy(string name, Material mat, MovementPattern pattern, int hp, float speed, bool diamond)
        {
            var root = new GameObject(name);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            if (diamond)
                Part(visual, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(45f, 45f, 0f), Vector3.one * 0.7f, mat);
            else
            {
                Part(visual, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0f, 0f, 45f), Vector3.one * 0.85f, mat);
                Part(visual, PrimitiveType.Sphere, "Eye", new Vector3(-0.35f, 0f, -0.3f), Vector3.zero, Vector3.one * 0.35f, mat);
            }
            AddKinematicBody(root);
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var health = root.AddComponent<Health>();
            SetEnum(health, "faction", (int)Faction.Enemy);
            SetInt(health, "maxHealth", hp);
            var enemy = root.AddComponent<Enemy>();
            SetEnum(enemy, "pattern", (int)pattern);
            SetFloat(enemy, "speed", speed);
            var contact = root.AddComponent<ContactDamage>();
            SetEnum(contact, "faction", (int)Faction.Enemy);

            SetLayer(root, LayerEnemy);
            return root;
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

        // ---------- data ----------

        static WaveSchedule BuildEncounter01(Prefabs p)
        {
            var drone = p.Drone.GetComponent<Enemy>();
            var wave = p.Ondulante.GetComponent<Enemy>();
            var s = ScriptableObject.CreateInstance<WaveSchedule>();
            s.warningSeconds = 0.8f;
            s.clearDelay = 4f;
            s.waves = new List<WaveSchedule.Wave>
            {
                // GDD 04, encounter 1. 0:00–0:10 is free flight (obstacles not built yet).
                W("3 drones em fila, vindo do centro", 10f, drone, MovementPattern.Straight, 3, 0f, 2.2f),
                W("drones em dois níveis de altura", 25f, drone, MovementPattern.Straight, 3, 0.5f, 2.2f, -0.5f),
                W("drones em dois níveis de altura (2)", 33f, drone, MovementPattern.Straight, 3, 0.7f, 2.2f, -0.1f),
                W("primeiro grupo de ondulantes", 40f, wave, MovementPattern.Sine, 4, 0.2f, 1.6f),
                W("ondulantes (2)", 50f, wave, MovementPattern.Sine, 5, -0.3f, 1.6f),
                // 1:00–1:20 asks for a turret + drones; the turret isn't built yet, so drones only.
                W("drones (torreta pendente)", 60f, drone, MovementPattern.Straight, 3, 0.6f, 2.2f, -0.6f),
                W("drones finais", 70f, drone, MovementPattern.Straight, 4, 0f, 2f),
            };
            return s;
        }

        static WaveSchedule.Wave W(string label, float time, Enemy enemy, MovementPattern pattern, int count, float height, float spacing, float? second = null)
        {
            return new WaveSchedule.Wave
            {
                label = label, time = time, enemy = enemy, pattern = pattern, count = count,
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

        // ---------- helpers ----------

        static Transform Group(string name) => new GameObject(name).transform;

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
