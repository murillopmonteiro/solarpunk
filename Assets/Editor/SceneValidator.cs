using System.Collections.Generic;
using System.Linq;
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
    /// Checks the generated scene for the failure modes that a clean compile
    /// won't catch: unassigned serialized references, a missing built-in font,
    /// a prefab without a mesh or collider. Run headless in CI-style batch mode
    /// via -executeMethod Solarpunk.EditorTools.SceneValidator.Validate.
    /// </summary>
    public static class SceneValidator
    {
        private static readonly List<string> Problems = new();

        [MenuItem("Solarpunk/Validate Scene")]
        public static void Validate()
        {
            Problems.Clear();

            CheckFont();
            CheckTileDefinitions();
            CheckHexPrefab();
            CheckScene();

            if (Problems.Count > 0)
            {
                foreach (string problem in Problems) Debug.LogError($"[Validate] {problem}");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[Validate] OK — scene, prefab, tile data and HUD dependencies all check out.");
        }

        private static void CheckFont()
        {
            Font font = UIFactory.Font;
            if (font == null) Problems.Add("No built-in font resolved — every HUD label would render blank.");
            else Debug.Log($"[Validate] Font: {font.name}");
        }

        private static void CheckTileDefinitions()
        {
            string[] guids = AssetDatabase.FindAssets("t:TileDefinition", new[] { "Assets/_Game/Data" });
            if (guids.Length != 10)
            {
                Problems.Add($"Expected 10 TileDefinition assets, found {guids.Length}.");
                return;
            }

            foreach (string guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<TileDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (string.IsNullOrEmpty(def.displayName)) Problems.Add($"{def.name} has no displayName.");
            }

            Debug.Log("[Validate] Tile definitions: 10 assets, all named.");
        }

        private static void CheckHexPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/HexCell.prefab");
            if (prefab == null)
            {
                Problems.Add("Assets/_Game/Prefabs/HexCell.prefab is missing.");
                return;
            }

            Mesh mesh = prefab.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null) Problems.Add("Hex prefab has no mesh.");
            else if (mesh.vertexCount < 12) Problems.Add($"Hex mesh looks wrong: {mesh.vertexCount} verts.");
            else Debug.Log($"[Validate] Hex mesh: {mesh.vertexCount} verts, {mesh.triangles.Length / 3} tris.");

            if (prefab.GetComponent<MeshCollider>()?.sharedMesh == null)
                Problems.Add("Hex prefab has no MeshCollider mesh — clicks would never hit a hex.");

            if (prefab.GetComponent<MeshRenderer>()?.sharedMaterial == null)
                Problems.Add("Hex prefab has no material.");

            if (prefab.GetComponent<HexCell>() == null) Problems.Add("Hex prefab has no HexCell component.");
        }

        private static void CheckScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/_Game/Scenes/Game.unity", OpenSceneMode.Single);
            List<GameObject> roots = scene.GetRootGameObjects().ToList();

            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
            if (camera == null) Problems.Add("No camera in the scene.");

            if (!roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Any())
                Problems.Add("No light in the scene — everything would render flat black.");

            RequireRefs<HexGridManager>(roots, "hexCellPrefab");
            RequireRefs<CityGrowth>(roots, "resourceManager");
            RequireRefs<TurnManager>(roots, "gridManager", "resourceManager", "cityGrowth");
            RequireRefs<BuildController>(roots, "resourceManager");
            RequireRefs<SelectionController>(roots, "worldCamera");
            RequireRefs<GameManager>(roots, "gridManager", "resourceManager");
            RequireRefs<GameHUD>(roots, "resourceManager", "turnManager", "selectionController",
                "buildController", "cityGrowth");

            var build = roots.SelectMany(r => r.GetComponentsInChildren<BuildController>(true)).FirstOrDefault();
            if (build != null)
            {
                var names = build.Palette
                    .Select((p, i) => p == null ? $"[{i}]=NULL" : $"[{i}]={p.displayName}")
                    .ToList();
                Debug.Log($"[Validate] Palette contents: {string.Join(", ", names)}");

                if (build.Palette.Count != 10) Problems.Add($"BuildController palette has {build.Palette.Count} entries, expected 10.");
                else if (build.Palette.Any(p => p == null)) Problems.Add("BuildController palette contains a null entry.");
                else Debug.Log("[Validate] Build palette: 10 tiles wired.");
            }
        }

        private static void RequireRefs<T>(List<GameObject> roots, params string[] fields) where T : Component
        {
            var component = roots.SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault();
            if (component == null)
            {
                Problems.Add($"{typeof(T).Name} is missing from the scene.");
                return;
            }

            var so = new SerializedObject(component);
            foreach (string field in fields)
            {
                SerializedProperty prop = so.FindProperty(field);
                if (prop == null) Problems.Add($"{typeof(T).Name}.{field} does not exist.");
                else if (prop.objectReferenceValue == null) Problems.Add($"{typeof(T).Name}.{field} is unassigned.");
            }
        }
    }
}
