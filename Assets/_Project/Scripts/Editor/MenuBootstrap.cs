using System.IO;
using OneMoreTime.Gameplay.Audio;
using OneMoreTime.Gameplay.Input;
using OneMoreTime.Gameplay.Session;
using OneMoreTime.Gameplay.World;
using OneMoreTime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace OneMoreTime.EditorTools
{
    /// <summary>
    /// Builds the menu flow: the first MissionData (its words are keys of the Game string table, see Build
    /// Localization), the Menu scene (title, menu,
    /// sound, controls, credits, mission map, story), the mission card in the arena, and the build order (Menu
    /// first, then the arena). Idempotent like the other bootstraps: existing assets and scenes are kept, and
    /// the arena is only touched where something is missing. Run Build Audio first, so the sounds exist.
    /// </summary>
    public static class MenuBootstrap
    {
        const string Root = "Assets/_Project";
        const string DataDir = Root + "/Data";
        const string UiDir = Root + "/UI";
        public const string MenuScenePath = Root + "/Scenes/Menu.unity";
        const string MissionPath = DataDir + "/Missions/Mission_01.asset";
        const string PicturePath = Root + "/Art/UI/Mission01_Story.png";
        const string StarMaterialPath = Root + "/Art/UI/M_MenuStar.mat";

        static readonly Color Background = new(0.024f, 0.016f, 0.07f);

        [MenuItem("One More Time/Bootstrap/Build Menu", priority = 12)]
        public static void BuildAll()
        {
            var mission = LoadOrCreate(MissionPath, CreateMission01);
            BuildMenuScene(mission);
            WireArena(mission);
            SetBuildOrder();
            AssetDatabase.SaveAssets();
            Debug.Log("[One More Time] Menu flow built: Menu.unity, Mission_01, mission card in the arena.");
        }

        // ---------------------------------------------------------------- assets

        static MissionData CreateMission01()
        {
            var mission = ScriptableObject.CreateInstance<MissionData>();
            mission.number = 1;
            mission.place = "Earth · Dry Backlands";
            mission.title = "Overthrow the Empire";
            mission.placeKey = "mission.01.place";
            mission.titleKey = "mission.01.title";
            mission.storyKeys = LocalizationStrings.Mission01Story;
            mission.mapCode = "SE";
            mission.sceneName = "Arena_01";
            mission.story = new[]
            {
                "Outubro de 2010. Bilu visitou a Terra numa missão de paz, trazendo palavras sábias que deveriam mudar o destino do planeta para sempre.",
                "42 anos depois, tomado por uma mistura de ira, decepção e revolta, Bilu volta à Terra. Mas desta vez a paz não é mais uma opção.",
                "Consumida pela tirania e pelo totalitarismo, a Terra foi espremida até a última gota. Depois de uma série de catástrofes climáticas, o mar secou, e o planeta virou um deserto árido e decadente.",
                "Os recursos que restavam foram saqueados e levados numa missão desesperada para a tão sonhada colônia em Marte. Que, ironicamente, deu errado: pelo visto, eles precisavam de professores universitários, afinal...",
                "E o tal 1% saiu à deriva pelo infinito, num cruzeiro de luxo com um destino tão certo quanto o do Titanic.",
                "A Terra virou uma piada sem graça: as ruínas de um império decadente, defendido pelas engrenagens de um sistema sem patrão que já não sabe como parar.",
                "Bilu volta para entregar a mensagem de novo. E desta vez ela vai descer goela abaixo.",
                "Uma vez não bastou. Então, mais uma vez:",
                "BUSQUEM CONHECIMENTO!!!!",
            };
            mission.storyPicture = AssetDatabase.LoadAssetAtPath<Sprite>(PicturePath);
            return mission;
        }

        static T LoadOrCreate<T>(string path, System.Func<T> create) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // ---------------------------------------------------------------- the menu scene

        static void BuildMenuScene(MissionData mission)
        {
            if (File.Exists(MenuScenePath))
            {
                Debug.Log($"[One More Time] {MenuScenePath} already exists; kept as is. Delete it to rebuild.");
                return;
            }

            // NewScene unloads unused assets: remember paths and refetch.
            AssetDatabase.SaveAssets();
            string missionPath = AssetDatabase.GetAssetPath(mission);
            var current = SceneManager.GetActiveScene();
            if (current.isDirty && !string.IsNullOrEmpty(current.path))
                EditorSceneManager.SaveScene(current);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            mission = AssetDatabase.LoadAssetAtPath<MissionData>(missionPath);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.3f, 0.45f);
            RenderSettings.fog = false;

            // Systems
            var systems = Group("Systems");
            var reader = Child<MenuInputReader>(systems, "Input");
            Set(reader, "controls", AssetDatabase.LoadAssetAtPath<InputActionAsset>($"{Root}/Input/OneMoreTimeControls.inputactions"));
            var audio = new GameObject("Audio");
            audio.transform.SetParent(systems, false);
            var sfx = audio.AddComponent<SfxPlayer>();
            var music = audio.AddComponent<MusicPlayer>();
            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>($"{DataDir}/AudioLibrary.asset");
            Set(sfx, "library", library);
            Set(music, "library", library);
            Child<PixelView>(systems, "PixelView");

            // Lighting
            var lighting = Group("Lighting");
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.transform.SetParent(lighting, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = new Color(0.9f, 0.93f, 1f);
            sun.transform.rotation = Quaternion.Euler(35f, -35f, 0f);

            // World: the camera and the ship on show
            var world = Group("World");
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>();
            cam.transform.SetParent(world, false);
            cam.transform.localPosition = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 60f;
            cam.GetUniversalAdditionalCameraData();

            var showcase = new GameObject("Showcase").AddComponent<ShowcaseSpin>();
            showcase.transform.SetParent(world, false);
            var shipVisual = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Ship/PlayerShip.prefab")?.transform.Find("Visual");
            if (shipVisual != null)
            {
                var copy = Object.Instantiate(shipVisual.gameObject, showcase.transform);
                copy.name = "Visual";
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = Vector3.one * 1.35f;
            }

            // UI
            var ui = Group("UI");
            var menuGo = new GameObject("Menu");
            menuGo.transform.SetParent(ui, false);
            var doc = menuGo.AddComponent<UIDocument>();
            doc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>($"{UiDir}/HudPanelSettings.asset");
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UiDir}/Menu.uxml");
            var starfield = menuGo.AddComponent<StarfieldView>();
            Set(starfield, "view", cam);
            Set(starfield, "material", LoadOrCreate(StarMaterialPath, () => new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"))));
            var menu = menuGo.AddComponent<MenuController>();
            Set(menu, "input", reader);
            Set(menu, "sfx", sfx);
            Set(menu, "music", music);
            Set(menu, "showcase", showcase);
            SetArray(menu, "missions", new Object[] { mission });

            EnsureFolder(Path.GetDirectoryName(MenuScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        // ---------------------------------------------------------------- the mission card in the arena

        static void WireArena(MissionData mission)
        {
            var scene = EditorSceneManager.OpenScene(PlaceholderBootstrap.ScenePath, OpenSceneMode.Single);
            // Building the menu scene unloaded the assets passed in: load them again by path.
            mission = AssetDatabase.LoadAssetAtPath<MissionData>(MissionPath);

            var stage = Object.FindFirstObjectByType<StageDirector>();
            var sfx = Object.FindFirstObjectByType<SfxPlayer>();
            var ui = GameObject.Find("UI");
            if (stage == null || ui == null)
            {
                Debug.LogError("[One More Time] Arena_01 is missing the stage or the UI group; run Build Placeholder Content first.");
                return;
            }

            bool changed = false;
            var card = Object.FindFirstObjectByType<MissionCardController>();
            if (card == null)
            {
                var go = new GameObject("MissionCard");
                go.transform.SetParent(ui.transform, false);
                var doc = go.AddComponent<UIDocument>();
                doc.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>($"{UiDir}/HudPanelSettings.asset");
                doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UiDir}/MissionCard.uxml");
                doc.sortingOrder = 10;   // above the HUD
                card = go.AddComponent<MissionCardController>();
                changed = true;
            }
            changed |= SetIfNull(card, "fallbackMission", mission);
            changed |= SetIfNull(card, "sfx", sfx);
            changed |= SetIfNull(stage, "gate", card);

            if (!changed)
                return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void SetBuildOrder()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(PlaceholderBootstrap.ScenePath, true),
            };
        }

        // ---------------------------------------------------------------- helpers

        static Transform Group(string name) => new GameObject(name).transform;

        static T Child<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool SetIfNull(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
            if (property.objectReferenceValue != null || value == null)
                return false;
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
