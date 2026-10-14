using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Turns the level 1 city FBX into a game-ready prefab.
    ///
    /// The mesh is generated, not modelled: ArtSource/build_city.py builds it from
    /// primitives inside Blender, the same way the mine and the power station are
    /// built. The order of <see cref="Palette"/> must match MATERIALS in that
    /// script, which is what sets the material index on every face.
    /// </summary>
    public static class CityModelBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/CityLevel1.prefab";

        private const string FbxPath = "Assets/_Game/Models/City.fbx";
        private const string MaterialDir = "Assets/_Game/Materials/City";

        /// <summary>Brick, stone, slate, iron and glass are the power station's exact
        /// values: the town that works the pit is built of the same material as the pit.
        /// Tile is the warm roof red the old city already used.</summary>
        private static readonly (string Name, Color Color)[] Palette =
        {
            ("City_Brick", new Color(0.52f, 0.30f, 0.24f)),
            ("City_Stone", new Color(0.90f, 0.87f, 0.79f)),
            ("City_Slate", new Color(0.38f, 0.42f, 0.48f)),
            ("City_Tile", new Color(0.62f, 0.34f, 0.27f)),
            ("City_Iron", new Color(0.30f, 0.30f, 0.33f)),
            ("City_Glass", new Color(0.34f, 0.47f, 0.60f)),
            ("City_Cobble", new Color(0.55f, 0.54f, 0.52f)),
            ("City_Grass", new Color(0.28f, 0.45f, 0.22f)),
        };

        /// <summary>Authored 1.07 units wide. Fitted against the hexagon itself rather than a
        /// bounding box, the way the plant's is: the worst corner sits 12% inside the nearest
        /// edge here. Kept at the old city's 1.32 so the town's footprint on the board does
        /// not change. Re-measure if the footprint does.</summary>
        private const float DisplayScale = 1.32f;

        /// <summary>The board camera is fixed at yaw 0 and the FBX axis conversion lands the
        /// model facing away from it. This turns the terrace fronts and the cobbled street
        /// toward the viewer, with the civic hall and its tower behind.</summary>
        private const float DisplayYaw = -175f;

        [MenuItem("Solarpunk/Build City Model Prefab")]
        public static GameObject BuildPrefab()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[City] No model at {FbxPath}. Run ArtSource/build_city.py in Blender first.");
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
            instance.transform.localRotation = Quaternion.Euler(0f, DisplayYaw, 0f);

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
