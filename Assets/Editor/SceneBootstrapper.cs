using System.Collections.Generic;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Events;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Research;
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
        private const string GroundMaterialPath = "Assets/_Game/Materials/Ground.mat"; // superseded by the sea
        private const string SeaMaterialPath = "Assets/_Game/Materials/Sea.mat";
        private const string ScenePath = "Assets/_Game/Scenes/Game.unity";

        [MenuItem("Solarpunk/Build Initial Scene")]
        public static void BuildInitialScene()
        {
            Mesh hexMesh = HexMeshFactory.GenerateAndSave();
            SeaMeshFactory.GenerateAndSave();
            CreateHexPrefab(hexMesh);

            // Must precede the tile assets: the city, forest and coal extraction
            // definitions reference these.
            CityModelBuilder.BuildPrefab();
            TreeModelBuilder.BuildPrefab();
            CoalMineModelBuilder.BuildPrefab();
            CoalPlantModelBuilder.BuildPrefab();
            HydroModelBuilder.BuildPrefab();

            TileDataFactory.GenerateAll();
            ResearchTreeFactory.GenerateAll();
            EventDeckFactory.GenerateAll();
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
            List<TileDefinition> allTiles = TileDataFactory.LoadAll();
            List<ResearchNode> researchNodes = ResearchTreeFactory.LoadAll();
            List<EventDefinition> eventDeck = EventDeckFactory.LoadAll();

            if (hexPrefab == null) throw new System.InvalidOperationException($"Hex prefab missing at {PrefabPath}.");
            if (allTiles.Count != TileDataFactory.OrderedNames.Length || allTiles.Any(p => p == null))
                throw new System.InvalidOperationException("Tile definitions failed to load — aborting scene build.");

            // The forest is board-generated terrain, so it is wired to the grid and kept
            // out of the build menu.
            List<TileDefinition> palette = allTiles.FindAll(d => d.playerBuildable);
            TileDefinition forest = allTiles.Find(d => d.category == TileCategory.Forest);
            if (forest == null) throw new System.InvalidOperationException("No forest tile definition.");

            // --- Camera: angled perspective so the board reads as 3D ---
            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            cameraGo.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // The sea fills the whole view; this dawn haze only shows if the camera ever lifts past it.
            camera.backgroundColor = new Color(0.97f, 0.89f, 0.74f);
            camera.fieldOfView = 45f;
            // Centred in the corridor between the resource capsules on the left
            // and the inspector panel on the right.
            cameraGo.transform.SetPositionAndRotation(
                new Vector3(0.35f, 6.5f, -5.4f), Quaternion.Euler(49f, 0f, 0f));
            cameraGo.tag = "MainCamera";

            // --- Lighting ---
            var keyLightGo = new GameObject("Key Light");
            var keyLight = keyLightGo.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.05f;
            keyLight.color = new Color(1f, 0.96f, 0.88f);
            keyLightGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            // Warm sky bounce keeps the shaded faces from going muddy.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.34f, 0.39f, 0.42f);

            // --- Faceted sea under the island ---
            // Unlit: its shading is baked per triangle, and the lagoon-to-haze gradient
            // stands in for the sky. Just under the hexes so their sides read as slabs.
            AssetDatabase.DeleteAsset(GroundMaterialPath);
            var seaMaterial = new Material(Shader.Find("Solarpunk/VertexColorUnlit"));
            AssetDatabase.DeleteAsset(SeaMaterialPath);
            AssetDatabase.CreateAsset(seaMaterial, SeaMaterialPath);

            var sea = new GameObject("Sea");
            sea.transform.position = new Vector3(0f, -0.36f, 0f);
            sea.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(SeaMeshFactory.MeshPath);
            sea.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SeaMaterialPath);

            // --- Board ---
            var gridGo = new GameObject("HexGrid");
            var gridManager = gridGo.AddComponent<HexGridManager>();
            var gridSo = new SerializedObject(gridManager);
            gridSo.FindProperty("hexCellPrefab").objectReferenceValue = hexPrefab.GetComponent<HexCell>();
            gridSo.FindProperty("forestDefinition").objectReferenceValue = forest;
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
            // The city's G = [E, M, S, P, H]. Constant: every level contributes this same vector.
            SetResourceVector(citySo.FindProperty("cityEffect"),
                new ResourceVector { energy = -8f, money = -2f, sustainability = -1f, population = 5f, happiness = 0f });
            citySo.ApplyModifiedPropertiesWithoutUndo();

            // Research sits outside the 5-resource vector, so it gets its own manager.
            var researchManager = AddChild<ResearchManager>(managersRoot, "ResearchManager");
            var researchSo = new SerializedObject(researchManager);
            researchSo.FindProperty("pointsPerCityLevel").floatValue = 1f;
            SerializedProperty treeProp = researchSo.FindProperty("tree");
            treeProp.ClearArray();
            for (int i = 0; i < researchNodes.Count; i++)
            {
                treeProp.InsertArrayElementAtIndex(i);
                treeProp.GetArrayElementAtIndex(i).objectReferenceValue = researchNodes[i];
            }
            researchSo.ApplyModifiedPropertiesWithoutUndo();

            // The city level cap comes from research: 2 before any, then 3, 5, 7, 10 along the city line.
            var citySoResearch = new SerializedObject(cityGrowth);
            citySoResearch.FindProperty("researchManager").objectReferenceValue = researchManager;
            citySoResearch.FindProperty("baseLevelCap").intValue = 2;
            citySoResearch.ApplyModifiedPropertiesWithoutUndo();

            // One ordinary year in twenty draws a card; every 5th year is certain.
            // A drawn card waits on the HUD for an answer; the year does not turn until it is given.
            var randomEvents = AddChild<RandomEventManager>(managersRoot, "RandomEventManager");
            var eventsSo = new SerializedObject(randomEvents);
            eventsSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            eventsSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            eventsSo.FindProperty("chancePerYear").floatValue = 0.05f;
            eventsSo.FindProperty("guaranteedEveryYears").intValue = 5;
            eventsSo.FindProperty("seed").intValue = 0;
            SerializedProperty deckProp = eventsSo.FindProperty("deck");
            deckProp.ClearArray();
            for (int i = 0; i < eventDeck.Count; i++)
            {
                deckProp.InsertArrayElementAtIndex(i);
                deckProp.GetArrayElementAtIndex(i).objectReferenceValue = eventDeck[i];
            }
            eventsSo.ApplyModifiedPropertiesWithoutUndo();

            var turnManager = AddChild<TurnManager>(managersRoot, "TurnManager");
            var turnSo = new SerializedObject(turnManager);
            turnSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            turnSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            turnSo.FindProperty("cityGrowth").objectReferenceValue = cityGrowth;
            turnSo.FindProperty("researchManager").objectReferenceValue = researchManager;
            turnSo.FindProperty("randomEvents").objectReferenceValue = randomEvents;
            turnSo.ApplyModifiedPropertiesWithoutUndo();

            var buildController = AddChild<BuildController>(managersRoot, "BuildController");
            var buildSo = new SerializedObject(buildController);
            buildSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            buildSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            buildSo.FindProperty("cityGrowth").objectReferenceValue = cityGrowth;
            buildSo.FindProperty("researchManager").objectReferenceValue = researchManager;
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
            hudSo.FindProperty("researchManager").objectReferenceValue = researchManager;
            hudSo.FindProperty("randomEvents").objectReferenceValue = randomEvents;
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            // Dev-only capture rig; inert unless the player is run with -autoshot.
            var screenshots = AddChild<Solarpunk.Dev.DevScreenshots>(managersRoot, "DevScreenshots");
            var shotSo = new SerializedObject(screenshots);
            shotSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            shotSo.FindProperty("selectionController").objectReferenceValue = selectionController;
            shotSo.FindProperty("buildController").objectReferenceValue = buildController;
            shotSo.FindProperty("turnManager").objectReferenceValue = turnManager;
            shotSo.FindProperty("researchManager").objectReferenceValue = researchManager;
            shotSo.FindProperty("randomEvents").objectReferenceValue = randomEvents;
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
