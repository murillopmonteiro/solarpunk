using Solarpunk.Debugging;
using Solarpunk.Grid;
using Solarpunk.Managers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// One-off scaffolding: builds a hex cell prefab and a bootstrap scene
    /// wiring up the core managers, so there's something to press Play on.
    /// Run via menu Solarpunk/Build Initial Scene, or headless with
    /// -executeMethod Solarpunk.EditorTools.SceneBootstrapper.BuildInitialScene.
    /// </summary>
    public static class SceneBootstrapper
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/HexCellPrototype.prefab";
        private const string ScenePath = "Assets/_Game/Scenes/Game.unity";

        [MenuItem("Solarpunk/Build Initial Scene")]
        public static void BuildInitialScene()
        {
            GameObject prefab = CreateHexPrefab();
            BuildScene(prefab);
        }

        private static GameObject CreateHexPrefab()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "HexCellPrototype";
            root.transform.localScale = new Vector3(0.95f, 0.2f, 0.95f);

            var cell = root.AddComponent<HexCell>();
            root.AddComponent<HexCellVisual>();

            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs");
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            return prefabAsset;
        }

        private static void BuildScene(GameObject hexPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 12f;
            cameraGo.transform.position = new Vector3(0f, 15f, -0.01f);
            cameraGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cameraGo.tag = "MainCamera";

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var managersRoot = new GameObject("Managers");

            var gridGo = new GameObject("HexGridManager");
            gridGo.transform.SetParent(managersRoot.transform);
            var gridManager = gridGo.AddComponent<HexGridManager>();
            var gridSo = new SerializedObject(gridManager);
            gridSo.FindProperty("hexCellPrefab").objectReferenceValue = hexPrefab;
            gridSo.FindProperty("gridRadius").intValue = 4;
            gridSo.FindProperty("hexSize").floatValue = 1f;
            gridSo.ApplyModifiedPropertiesWithoutUndo();

            var resourceGo = new GameObject("ResourceManager");
            resourceGo.transform.SetParent(managersRoot.transform);
            var resourceManager = resourceGo.AddComponent<ResourceManager>();

            var turnGo = new GameObject("TurnManager");
            turnGo.transform.SetParent(managersRoot.transform);
            var turnManager = turnGo.AddComponent<TurnManager>();
            var turnSo = new SerializedObject(turnManager);
            turnSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            turnSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            turnSo.ApplyModifiedPropertiesWithoutUndo();

            var gameGo = new GameObject("GameManager");
            gameGo.transform.SetParent(managersRoot.transform);
            var gameManager = gameGo.AddComponent<GameManager>();
            var gameSo = new SerializedObject(gameManager);
            gameSo.FindProperty("gridManager").objectReferenceValue = gridManager;
            gameSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            gameSo.ApplyModifiedPropertiesWithoutUndo();

            var debugGo = new GameObject("DebugControls");
            debugGo.transform.SetParent(managersRoot.transform);
            var debugControls = debugGo.AddComponent<DebugControls>();
            var debugSo = new SerializedObject(debugControls);
            debugSo.FindProperty("turnManager").objectReferenceValue = turnManager;
            debugSo.FindProperty("resourceManager").objectReferenceValue = resourceManager;
            debugSo.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            Debug.Log($"Bootstrap scene saved to {ScenePath}. Press Play, then Space to advance a turn.");
        }
    }
}
