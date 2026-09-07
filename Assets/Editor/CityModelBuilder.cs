using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Turns the Blender-authored city FBX into a game-ready prefab.
    ///
    /// Materials are assigned here rather than imported from the FBX: FBX
    /// material import produces unpredictable results across Blender versions,
    /// and the palette belongs to the game anyway. The order of
    /// <see cref="Palette"/> must match the material slot order in the .blend.
    /// </summary>
    public static class CityModelBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/CityLevel1.prefab";

        private const string FbxPath = "Assets/_Game/Models/CityLevel1.fbx";
        private const string MaterialDir = "Assets/_Game/Materials/City";

        /// <summary>The town front faces away from the key light, so wall values are
        /// pitched high to survive being read in shadow at gameplay distance.</summary>
        private static readonly (string Name, Color Color)[] Palette =
        {
            ("City_Wall_Cream", new Color(0.95f, 0.92f, 0.84f)),
            ("City_Wall_Brick", new Color(0.70f, 0.44f, 0.36f)),
            ("City_Wall_Plaster", new Color(0.85f, 0.81f, 0.73f)),
            ("City_Roof_Slate", new Color(0.38f, 0.42f, 0.48f)),
            ("City_Roof_Tile", new Color(0.62f, 0.34f, 0.27f)),
            ("City_Trim", new Color(0.42f, 0.35f, 0.31f)),
        };

        /// <summary>Authored at 1 hex unit wide; this fills the tile without spilling past its edges.</summary>
        private const float DisplayScale = 1.32f;

        [MenuItem("Solarpunk/Build City Model Prefab")]
        public static GameObject BuildPrefab()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[City] No model at {FbxPath}. Export it from Blender first.");
                return null;
            }

            importer.globalScale = 1f;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.importNormals = ModelImporterNormals.Import; // keep the flat-shaded faces
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();

            Material[] materials = BuildMaterials();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (model == null)
            {
                Debug.LogError($"[City] {FbxPath} failed to load after reimport.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = "CityLevel1";
            instance.transform.localScale = Vector3.one * DisplayScale;

            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("[City] Imported model has no MeshRenderer.");
                Object.DestroyImmediate(instance);
                return null;
            }

            int slots = renderer.sharedMaterials.Length;
            var assigned = new Material[slots];
            for (int i = 0; i < slots; i++) assigned[i] = materials[Mathf.Min(i, materials.Length - 1)];
            renderer.sharedMaterials = assigned;

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(collider);
            }

            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs");
            AssetDatabase.DeleteAsset(PrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);

            var mesh = prefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            Debug.Log($"[City] Prefab built: {mesh.triangles.Length / 3} tris, " +
                      $"{slots} material slots, bounds {mesh.bounds.size}.");
            return prefab;
        }

        private static Material[] BuildMaterials()
        {
            System.IO.Directory.CreateDirectory(MaterialDir);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");

            var results = new Material[Palette.Length];
            for (int i = 0; i < Palette.Length; i++)
            {
                string path = $"{MaterialDir}/{Palette[i].Name}.mat";
                var material = new Material(shader) { color = Palette[i].Color };
                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.08f);
                if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(material, path);
                results[i] = AssetDatabase.LoadAssetAtPath<Material>(path);
            }

            AssetDatabase.SaveAssets();
            return results;
        }
    }
}
