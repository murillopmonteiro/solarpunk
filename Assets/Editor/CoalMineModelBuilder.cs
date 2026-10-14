using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Turns the coal extraction FBX into a game-ready prefab.
    ///
    /// The mesh itself is generated, not modelled: ArtSource/build_coalmine.py
    /// builds it from primitives inside Blender. The order of <see cref="Palette"/>
    /// must match MATERIALS in that script, which is what sets the material index
    /// on every face.
    /// </summary>
    public static class CoalMineModelBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/CoalMine.prefab";

        private const string FbxPath = "Assets/_Game/Models/CoalMine.fbx";
        private const string MaterialDir = "Assets/_Game/Materials/Mine";

        /// <summary>Stone and slate deliberately match the city's values: the pithead is
        /// the same era and should read as built by the same hands.</summary>
        private static readonly (string Name, Color Color)[] Palette =
        {
            ("Mine_Timber", new Color(0.55f, 0.42f, 0.28f)),
            ("Mine_Stone", new Color(0.90f, 0.87f, 0.79f)),
            ("Mine_Slate", new Color(0.38f, 0.42f, 0.48f)),
            ("Mine_Iron", new Color(0.30f, 0.30f, 0.33f)),
            ("Mine_Coal", new Color(0.10f, 0.10f, 0.12f)),
            ("Mine_Rock", new Color(0.46f, 0.33f, 0.28f)),
            ("Mine_Grass", new Color(0.28f, 0.45f, 0.22f)),
        };

        /// <summary>Authored 0.96 units wide; this fills the hex without the rail spur
        /// spilling over an edge.</summary>
        private const float DisplayScale = 1.25f;

        /// <summary>The board camera is fixed at yaw 0, and the FBX axis conversion lands
        /// the model facing away from it: the winding house sits nearest and the headframe
        /// hides it. This turns the pithead to the three-quarter view it was composed for —
        /// house back-left, cart front-left, spoil heap front. Orientation lives here rather
        /// than baked into the mesh so build_coalmine.py's coordinates still read literally.</summary>
        private const float DisplayYaw = -175f;

        [MenuItem("Solarpunk/Build Coal Mine Model Prefab")]
        public static GameObject BuildPrefab()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[Mine] No model at {FbxPath}. Run ArtSource/build_coalmine.py " +
                               "in Blender and export it first.");
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
                Debug.LogError($"[Mine] {FbxPath} failed to load after reimport.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = "CoalMine";
            instance.transform.localScale = Vector3.one * DisplayScale;
            instance.transform.localRotation = Quaternion.Euler(0f, DisplayYaw, 0f);

            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("[Mine] Imported model has no MeshRenderer.");
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
            Debug.Log($"[Mine] Prefab built: {mesh.triangles.Length / 3} tris, " +
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
