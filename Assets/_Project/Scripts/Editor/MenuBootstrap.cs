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
    /// Builds the menu flow: the text assets (MenuTexts and the first MissionData), the Menu scene (title, menu,
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
        const string TextsPath = DataDir + "/Menu/MenuTexts.asset";
        const string MissionPath = DataDir + "/Missions/Mission_01.asset";
        const string PicturePath = Root + "/Art/UI/Mission01_Story.png";

        static readonly Color Background = new(0.024f, 0.016f, 0.07f);

        [MenuItem("One More Time/Bootstrap/Build Menu", priority = 12)]
        public static void BuildAll()
        {
            var texts = LoadOrCreate(TextsPath, () => ScriptableObject.CreateInstance<MenuTexts>());
            var mission = LoadOrCreate(MissionPath, CreateMission01);
            BuildMenuScene(texts, mission);
            WireArena(texts, mission);
            SetBuildOrder();
            AssetDatabase.SaveAssets();
            Debug.Log("[One More Time] Menu flow built: Menu.unity, MenuTexts, Mission_01, mission card in the arena.");
        }

        // ---------------------------------------------------------------- assets

        static MissionData CreateMission01()
        {
            var mission = ScriptableObject.CreateInstance<MissionData>();
            mission.number = 1;
            mission.place = "Terra · Sertão Seco";
            mission.title = "Derrubar o Império";
            mission.mapCode = "SE";
            mission.sceneName = "Arena_01";
            mission.story = new[]
            {
                "Há muito tempo, o ET Bilu fez um único pedido à humanidade: busquem conhecimento.",
                "A humanidade ouviu. E não fez nada.",
                "Em vez de perguntas, escolheu ordens. Em vez de livros, escolheu um império, governado pela ignorância, que esgotou a Terra até o último gole: sem água, sem mata, só sertão seco e máquinas de extração enferrujadas sob um céu roxo e doente.",
                "O Bilu voltou para ver o que fizeram com o lugar. Não gostou.",
                "Ele tem uma nave pequena, um truque estranho (o passado dele continua lutando ao seu lado) e muita raiva acumulada.",
                "Hora de derrubar o império. Uma vez. E, se for preciso, mais uma.",
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

        static void BuildMenuScene(MenuTexts texts, MissionData mission)
        {
            if (File.Exists(MenuScenePath))
            {
                Debug.Log($"[One More Time] {MenuScenePath} already exists; kept as is. Delete it to rebuild.");
                return;
            }

            // NewScene unloads unused assets: remember paths and refetch.
            AssetDatabase.SaveAssets();
            string textsPath = AssetDatabase.GetAssetPath(texts), missionPath = AssetDatabase.GetAssetPath(mission);
            var current = SceneManager.GetActiveScene();
            if (current.isDirty && !string.IsNullOrEmpty(current.path))
                EditorSceneManager.SaveScene(current);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            texts = AssetDatabase.LoadAssetAtPath<MenuTexts>(textsPath);
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
            menuGo.AddComponent<StarfieldView>();
            var menu = menuGo.AddComponent<MenuController>();
            Set(menu, "input", reader);
            Set(menu, "sfx", sfx);
            Set(menu, "music", music);
            Set(menu, "texts", texts);
            Set(menu, "showcase", showcase);
            SetArray(menu, "missions", new Object[] { mission });

            EnsureFolder(Path.GetDirectoryName(MenuScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        // ---------------------------------------------------------------- the mission card in the arena

        static void WireArena(MenuTexts texts, MissionData mission)
        {
            var scene = EditorSceneManager.OpenScene(PlaceholderBootstrap.ScenePath, OpenSceneMode.Single);
            // Building the menu scene unloaded the assets passed in: load them again by path.
            texts = AssetDatabase.LoadAssetAtPath<MenuTexts>(TextsPath);
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
            changed |= SetIfNull(card, "texts", texts);
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
