using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Turns the coal thermal plant FBX into a game-ready prefab.
    ///
    /// The mesh is generated, not modelled: ArtSource/build_coalplant.py builds it
    /// from primitives inside Blender. The order of <see cref="Palette"/> must match
    /// MATERIALS in that script, which is what sets the material index on every face.
    /// </summary>
    public static class CoalPlantModelBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/CoalPlant.prefab";

        private const string FbxPath = "Assets/_Game/Models/CoalPlant.fbx";
        private const string MaterialDir = "Assets/_Game/Materials/Plant";

        /// <summary>Stone, iron and coal are the mine's exact values, so the pit that digs
        /// the fuel and the station that burns it read as the same world.</summary>
        private static readonly (string Name, Color Color)[] Palette =
        {
            ("Plant_Brick", new Color(0.52f, 0.30f, 0.24f)),
            ("Plant_Stone", new Color(0.90f, 0.87f, 0.79f)),
            ("Plant_Roof", new Color(0.31f, 0.34f, 0.40f)),
            ("Plant_Iron", new Color(0.30f, 0.30f, 0.33f)),
            ("Plant_Glass", new Color(0.34f, 0.47f, 0.60f)),
            ("Plant_Coal", new Color(0.10f, 0.10f, 0.12f)),
            ("Plant_Concrete", new Color(0.60f, 0.60f, 0.58f)),
            ("Plant_Grass", new Color(0.28f, 0.45f, 0.22f)),
        };

        /// <summary>Authored 1.26 units wide once the coal store moved out to the side. Fitted
        /// against the hexagon itself, not the bounding box: at this scale the worst corner
        /// sits 8% inside the nearest edge. Re-measure if the footprint changes again.</summary>
        private const float DisplayScale = 1.20f;

        /// <summary>Same reasoning as the mine's: the board camera is fixed at yaw 0 and the
        /// FBX axis conversion lands the model facing away from it. This turns the glazed
        /// turbine hall and the coal store to the front, chimneys rising behind.</summary>
        private const float DisplayYaw = -175f;

        [MenuItem("Solarpunk/Build Coal Plant Model Prefab")]
        public static GameObject BuildPrefab()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[Plant] No model at {FbxPath}. Run ArtSource/build_coalplant.py " +
                               "in Blender first.");
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
                Debug.LogError($"[Plant] {FbxPath} failed to load after reimport.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = "CoalPlant";
            instance.transform.localScale = Vector3.one * DisplayScale;
            instance.transform.localRotation = Quaternion.Euler(0f, DisplayYaw, 0f);

            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("[Plant] Imported model has no MeshRenderer.");
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
            Debug.Log($"[Plant] Prefab built: {mesh.triangles.Length / 3} tris, " +
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
