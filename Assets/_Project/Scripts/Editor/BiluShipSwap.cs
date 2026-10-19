using System.Linq;
using OneMoreTime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OneMoreTime.EditorTools
{
    /// <summary>
    /// Puts the textured Bilu model (Art/Ships/Bilu_Ship.fbx) in the ship, the echo and the menu showcase.
    /// The ship keeps the model's own materials, remapped to M_BiluShip (color and emissive maps) and
    /// M_BiluShip_Glow; the echo keeps its translucent material on every slot. The new model takes the old
    /// one's place under Visual/Model, with the same position, rotation, scale and layer. Safe to run again.
    /// </summary>
    public static class BiluShipSwap
    {
        const string ArtDir = "Assets/_Project/Art/Ships";
        const string ModelPath = ArtDir + "/Bilu_Ship.fbx";
        const string ColorMapPath = ArtDir + "/Textures/Bilu_Ship_ColorMap.png";
        const string EmissiveMapPath = ArtDir + "/Textures/Bilu_Ship_EmissiveMap.png";
        const string MaterialDir = ArtDir + "/Materials";
        const string ShipMaterialPath = MaterialDir + "/M_BiluShip.mat";
        const string GlowMaterialPath = MaterialDir + "/M_BiluShip_Glow.mat";
        const string ShipPrefabPath = "Assets/_Project/Prefabs/Ship/PlayerShip.prefab";
        const string EchoPrefabPath = "Assets/_Project/Prefabs/Echo/EchoGhost.prefab";
        const string EchoMaterialPath = "Assets/_Project/Art/Placeholder/Materials/M_Echo_Violet.mat";

        // The cyan painted in the emissive map.
        static readonly Color Glow = new(0f, 1f, 0.906f);

        [MenuItem("One More Time/Art/Swap In Bilu Ship Model", priority = 30)]
        public static void Run()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError($"[One More Time] {ModelPath} not found.");
                return;
            }

            var colorMap = SetUpTexture(ColorMapPath);
            var emissiveMap = SetUpTexture(EmissiveMapPath);
            var ship = SaveMaterial(ShipMaterialPath, colorMap, Color.white, emissiveMap, Color.white);
            var glow = SaveMaterial(GlowMaterialPath, null, Glow, null, Glow);
            RemapModelMaterials(ship, glow);

            SwapIn(ShipPrefabPath, model, null);
            SwapIn(EchoPrefabPath, model, AssetDatabase.LoadAssetAtPath<Material>(EchoMaterialPath));
            RebuildMenuShowcase();
            AssetDatabase.SaveAssets();
            Debug.Log("[One More Time] Bilu_Ship is on PlayerShip, EchoGhost and the menu showcase.");
        }

        // The ship is drawn far smaller than the 512 px maps, so they get mipmaps and bilinear filtering
        // (point filtering would shimmer); PixelView pixelates the result.
        static Texture2D SetUpTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material SaveMaterial(string path, Texture2D baseMap, Color baseColor, Texture2D emissionMap, Color emission)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                EnsureFolder(MaterialDir);
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_BaseMap", baseMap);
            material.SetTexture("_MainTex", baseMap);
            material.SetColor("_BaseColor", baseColor);
            material.SetTexture("_EmissionMap", emissionMap);
            material.SetColor("_EmissionColor", emission);
            material.SetFloat("_Smoothness", 0.5f);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void RemapModelMaterials(Material ship, Material glow)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Bilu_Ship"), ship);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "BlueEmissive"), glow);
            importer.SaveAndReimport();
        }

        // Replaces the model under Visual/Model. A non-null material goes on every slot (the echo).
        static void SwapIn(string prefabPath, GameObject model, Material everySlot)
        {
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var holder = root.transform.Find("Visual/Model");
                if (holder == null)
                {
                    Debug.LogError($"[One More Time] {prefabPath} has no Visual/Model.");
                    return;
                }

                var placed = (GameObject)PrefabUtility.InstantiatePrefab(model, holder);
                int layer = holder.gameObject.layer;
                if (holder.childCount > 1)
                {
                    var old = holder.GetChild(0);
                    placed.transform.SetLocalPositionAndRotation(old.localPosition, old.localRotation);
                    placed.transform.localScale = old.localScale;
                    layer = old.gameObject.layer;
                    Object.DestroyImmediate(old.gameObject);
                }

                foreach (var t in placed.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = layer;
                if (everySlot != null)
                    foreach (var renderer in placed.GetComponentsInChildren<Renderer>(true))
                        renderer.sharedMaterials = Enumerable.Repeat(everySlot, renderer.sharedMaterials.Length).ToArray();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The showcase holds a plain copy of the ship's Visual (see MenuBootstrap); copy it again.
        static void RebuildMenuShowcase()
        {
            var scene = SceneManager.GetSceneByPath(MenuBootstrap.MenuScenePath);
            bool wasOpen = scene.isLoaded;
            if (!wasOpen)
                scene = EditorSceneManager.OpenScene(MenuBootstrap.MenuScenePath, OpenSceneMode.Additive);

            var showcase = scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<ShowcaseSpin>(true))
                .FirstOrDefault();
            var shipVisual = AssetDatabase.LoadAssetAtPath<GameObject>(ShipPrefabPath).transform.Find("Visual");
            if (showcase != null && shipVisual != null)
            {
                var old = showcase.transform.Find("Visual");
                var copy = Object.Instantiate(shipVisual.gameObject, showcase.transform);
                copy.name = "Visual";
                if (old != null)
                {
                    copy.transform.SetLocalPositionAndRotation(old.localPosition, old.localRotation);
                    copy.transform.localScale = old.localScale;
                    Object.DestroyImmediate(old.gameObject);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            else
            {
                Debug.LogWarning("[One More Time] Menu showcase not found; the menu still shows the old model.");
            }

            if (!wasOpen)
                EditorSceneManager.CloseScene(scene, true);
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
