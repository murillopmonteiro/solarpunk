using System.Collections.Generic;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Tiles;
using Solarpunk.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Rebuilds the whole playable test scene from code: hex mesh, hex prefab,
    /// tile definitions, board, cameras/lighting and the manager stack. Safe to
    /// rerun — it overwrites the generated assets and the scene each time.
    /// </summary>
    public static class SceneBootstrapper
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/HexCell.prefab";
        private const string MaterialPath = "Assets/_Game/Materials/HexSurface.mat";
        private const string GroundMaterialPath = "Assets/_Game/Materials/Ground.mat";
        private const string ScenePath = "Assets/_Game/Scenes/Game.unity";

        [MenuItem("Solarpunk/Build Initial Scene")]
        public static void BuildInitialScene()
        {
            Mesh hexMesh = HexMeshFactory.GenerateAndSave();
            CreateHexPrefab(hexMesh);
            TileDataFactory.GenerateAll();
            BuildScene();
        }

        private static Material CreateMaterial(string path, Color color, float smoothness)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

            System.IO.Directory.CreateDirectory("Assets/_Game/Materials");
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static GameObject CreateHexPrefab(Mesh hexMesh)
        {
            Material surface = CreateMaterial(MaterialPath, Color.white, 0.12f);

            var root = new GameObject("HexCell");
            root.AddComponent<MeshFilter>().sharedMesh = hexMesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = surface;
            root.AddComponent<MeshCollider>().sharedMesh = hexMesh;

            var cell = root.AddComponent<HexCell>();
            var cellSo = new SerializedObject(cell);
            cellSo.FindProperty("tileRenderer").objectReferenceValue = root.GetComponent<MeshRenderer>();
            cellSo.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs");
            AssetDatabase.DeleteAsset(PrefabPath);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            // The old cube prototype is superseded by the real hex mesh.
            AssetDatabase.DeleteAsset("Assets/_Game/Prefabs/HexCellPrototype.prefab");

            return prefabAsset;
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Resolve asset references only AFTER the new scene exists. Creating a
            // scene unloads unused assets, which turns any reference grabbed
            // beforehand into a destroyed "fake null" that silently serialises as
            // none — the palette came out empty for exactly this reason.
            GameObject hexPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            List<TileDefinition> palette = TileDataFactory.LoadAll();

            if (hexPrefab == null) throw new System.InvalidOperationException($"Hex prefab missing at {PrefabPath}.");
            if (palette.Count != TileDataFactory.OrderedNames.Length || palette.Any(p => p == null))
                throw new System.InvalidOperationException("Tile definitions failed to load — aborting scene build.");

            // --- Camera: angled perspective so the board reads as 3D ---
            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.13f, 0.16f);
            camera.fieldOfView = 45f;
            // Offset right and pulled back so the board sits clear of the top bar
            // and the right-hand inspector panel rather than behind them.
            cameraGo.transform.SetPositionAndRotation(
                new Vector3(1.15f, 6.6f, -5.5f), Quaternion.Euler(49f, 0f, 0f));
            cameraGo.tag = "MainCamera";

            // --- Lighting ---
            var keyLightGo = new GameObject("Key Light");
            var keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.1f;
            keyLight.color = new Color(1f, 0.97f, 0.90f);
            keyLightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.38f, 0.42f);

            // --- Ground slab under the island ---
            Material groundMaterial = CreateMaterial(GroundMaterialPath, new Color(0.11f, 0.16f, 0.18f), 0.05f);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.36f, 0f);
            ground.transform.localScale = Vector3.one * 3f;
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            // --- Board ---
            var gridGo = new GameObject("HexGrid");
            var gridManager = gridGo.AddComponent<HexGridManager>();
            var gridSo = new SerializedObject(gridManager);
            gridSo.FindProperty("hexCellPrefab").objectReferenceValue = hexPrefab.GetComponent<HexCell>();
            gridSo.FindProperty("hexSize").floatValue = 1f;
            gridSo.FindProperty("spacing").floatValue = 1.04f;
            gridSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Managers ---
            var managersRoot = new GameObject("Managers");

            var resourceManager = AddChild<ResourceManager>(managersRoot, "ResourceManager");

            var cityGrowth = AddChild<CityGrowth>(managersRoot, "CityGrowth");
            var citySo = new SerializedObject(cityGrowth);
            citySo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            citySo.FindProperty("happinessHealthyThreshold").floatValue = 30f;
            SetResourceVector(citySo.FindProperty("perLevelEffect"),
                new ResourceVector { energy = -8f, population = 5f, happiness = 2f, sustainability = -1f });
            citySo.ApplyModifiedPropertiesWithoutUndo();

            var turnManager = AddChild<TurnManager>(managersRoot, "TurnManager");
            var turnSo = new SerializedObject(turnManager);
            turnSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            turnSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            turnSo.FindProperty("cityGrowth").objectReferenceValue = cityGrowth;
            turnSo.ApplyModifiedPropertiesWithoutUndo();

            var buildController = AddChild<BuildController>(managersRoot, "BuildController");
            var buildSo = new SerializedObject(buildController);
            buildSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            SerializedProperty paletteProp = buildSo.FindProperty("palette");
            paletteProp.ClearArray();
            for (int i = 0; i < palette.Count; i++)
            {
                paletteProp.InsertArrayElementAtIndex(i);
                paletteProp.GetArrayElementAtIndex(i).objectReferenceValue = palette[i];
            }
            buildSo.ApplyModifiedPropertiesWithoutUndo();

            var selectionController = AddChild<SelectionController>(managersRoot, "SelectionController");
            var selectionSo = new SerializedObject(selectionController);
            selectionSo.FindProperty("worldCamera").objectReferenceValue = camera;
            selectionSo.ApplyModifiedPropertiesWithoutUndo();

            var gameManager = AddChild<GameManager>(managersRoot, "GameManager");
            var gameSo = new SerializedObject(gameManager);
            gameSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            gameSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            gameSo.ApplyModifiedPropertiesWithoutUndo();

            // --- HUD ---
            var hud = AddChild<GameHUD>(managersRoot, "GameHUD");
            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            hudSo.FindProperty("turnManager").objectReferenceValue = turnManager;
            hudSo.FindProperty("selectionController").objectReferenceValue = selectionController;
            hudSo.FindProperty("buildController").objectReferenceValue = buildController;
            hudSo.FindProperty("cityGrowth").objectReferenceValue = cityGrowth;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Dev-only capture rig; inert unless the player is run with -autoshot.
            var screenshots = AddChild<Solarpunk.Dev.DevScreenshots>(managersRoot, "DevScreenshots");
            var shotSo = new SerializedObject(screenshots);
            shotSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            shotSo.FindProperty("selectionController").objectReferenceValue = selectionController;
            shotSo.FindProperty("buildController").objectReferenceValue = buildController;
            shotSo.FindProperty("turnManager").objectReferenceValue = turnManager;
            shotSo.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log($"Scene rebuilt at {ScenePath}: 10 hexes, HUD, click-to-build. Press Play.");
        }

        private static T AddChild<T>(GameObject parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform);
            return go.AddComponent<T>();
        }

        private static void SetResourceVector(SerializedProperty property, ResourceVector value)
        {
            property.FindPropertyRelative("energy").floatValue = value.energy;
            property.FindPropertyRelative("money").floatValue = value.money;
            property.FindPropertyRelative("sustainability").floatValue = value.sustainability;
            property.FindPropertyRelative("population").floatValue = value.population;
            property.FindPropertyRelative("happiness").floatValue = value.happiness;
        }
    }
}
