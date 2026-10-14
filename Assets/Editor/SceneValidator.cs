using System.Collections.Generic;
using System.Linq;
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
            // The fallback to the built-in font is silent at runtime, so a missing file shows up here instead.
            foreach ((Font font, string expected) in new[] { (UIFactory.DisplayFont, "Fredoka"), (UIFactory.BodyFont, "Nunito") })
            {
                if (font == null) Problems.Add($"No {expected} font resolved — every HUD label would render blank.");
                else if (!font.name.StartsWith(expected))
                    Problems.Add($"{expected} missing from Resources/Fonts; the HUD fell back to {font.name}.");
                else Debug.Log($"[Validate] Font: {font.name}");
            }
        }

        private static void CheckTileDefinitions()
        {
            int expected = TileDataFactory.OrderedNames.Length;
            string[] guids = AssetDatabase.FindAssets("t:TileDefinition", new[] { "Assets/_Game/Data" });
            if (guids.Length != expected)
            {
                Problems.Add($"Expected {expected} TileDefinition assets, found {guids.Length}.");
                return;
            }

            foreach (string guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<TileDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (string.IsNullOrEmpty(def.displayName)) Problems.Add($"{def.name} has no displayName.");
            }

            Debug.Log($"[Validate] Tile definitions: {expected} assets, all named.");
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

            RequireRefs<HexGridManager>(roots, "hexCellPrefab", "forestDefinition");
            RequireRefs<CityGrowth>(roots, "resourceManager", "researchManager");
            RequireRefs<TurnManager>(roots, "gridManager", "resourceManager", "cityGrowth", "researchManager",
                "randomEvents");
            RequireRefs<BuildController>(roots, "gridManager", "resourceManager", "cityGrowth", "researchManager");
            RequireRefs<SelectionController>(roots, "worldCamera");
            RequireRefs<GameManager>(roots, "gridManager", "resourceManager");
            RequireRefs<GameHUD>(roots, "resourceManager", "turnManager", "selectionController",
                "buildController", "cityGrowth", "researchManager", "randomEvents");
            RequireRefs<RandomEventManager>(roots, "resourceManager", "gridManager");

            CheckResearchTree(roots);
            CheckEventDeck(roots);

            var build = roots.SelectMany(r => r.GetComponentsInChildren<BuildController>(true)).FirstOrDefault();
            if (build != null)
            {
                var names = build.Palette
                    .Select((p, i) => p == null ? $"[{i}]=NULL" : $"[{i}]={p.displayName}")
                    .ToList();
                Debug.Log($"[Validate] Palette contents: {string.Join(", ", names)}");

                int expectedTiles = TileDataFactory.LoadPalette().Count;
                if (build.Palette.Count != expectedTiles) Problems.Add($"BuildController palette has {build.Palette.Count} entries, expected {expectedTiles}.");
                else if (build.Palette.Any(p => p == null)) Problems.Add("BuildController palette contains a null entry.");
                else Debug.Log($"[Validate] Build palette: {expectedTiles} tiles wired.");
            }
        }

        /// <summary>An empty tree would render a research panel that is blank but not broken.</summary>
        private static void CheckResearchTree(List<GameObject> roots)
        {
            var manager = roots.SelectMany(r => r.GetComponentsInChildren<ResearchManager>(true)).FirstOrDefault();
            if (manager == null)
            {
                Problems.Add("ResearchManager is missing from the scene.");
                return;
            }

            int expected = ResearchTreeFactory.OrderedNames.Length;
            if (manager.Tree.Count != expected)
                Problems.Add($"ResearchManager tree has {manager.Tree.Count} nodes, expected {expected}.");
            else if (manager.Tree.Any(n => n == null))
                Problems.Add("ResearchManager tree contains a null node.");
            else
                Debug.Log($"[Validate] Research tree: {expected} nodes wired.");
        }

        /// <summary>An empty deck rolls forever and never fires, which looks like nothing is wrong.</summary>
        private static void CheckEventDeck(List<GameObject> roots)
        {
            var manager = roots.SelectMany(r => r.GetComponentsInChildren<RandomEventManager>(true)).FirstOrDefault();
            if (manager == null)
            {
                Problems.Add("RandomEventManager is missing from the scene.");
                return;
            }

            int expected = EventDeckFactory.OrderedNames.Length;
            if (manager.Deck.Count != expected)
                Problems.Add($"RandomEventManager deck has {manager.Deck.Count} events, expected {expected}.");
            else if (manager.Deck.Any(e => e == null))
                Problems.Add("RandomEventManager deck contains a null event.");
            else
                Debug.Log($"[Validate] Event deck: {expected} cards wired, " +
                          $"{manager.ChancePerYear:P0} a year, guaranteed every {manager.GuaranteedEveryYears}.");
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
