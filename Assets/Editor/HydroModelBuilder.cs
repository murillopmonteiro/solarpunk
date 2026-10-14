using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Turns the hydroelectric plant FBX into a game-ready prefab.
    ///
    /// The mesh is generated, not modelled: ArtSource/build_hidreletrica.py builds
    /// it from primitives inside Blender. The order of <see cref="Palette"/> must
    /// match MATERIALS in that script, which is what sets the material index on
    /// every face.
    /// </summary>
    public static class HydroModelBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/Hidreletrica.prefab";

        private const string FbxPath = "Assets/_Game/Models/Hidreletrica.fbx";
        private const string MaterialDir = "Assets/_Game/Materials/Hydro";

        /// <summary>Brick, slate, iron and glass are the power station's exact values.
        /// Water is pitched darker and greener than the waterfall hex it sits on, so the
        /// impounded head reads as standing above the river rather than merging into it.</summary>
        private static readonly (string Name, Color Color)[] Palette =
        {
            ("Hydro_Brick", new Color(0.52f, 0.30f, 0.24f)),
            ("Hydro_Concrete", new Color(0.72f, 0.71f, 0.67f)),
            ("Hydro_Roof", new Color(0.31f, 0.34f, 0.40f)),
            ("Hydro_Timber", new Color(0.55f, 0.42f, 0.28f)),
            ("Hydro_Iron", new Color(0.30f, 0.30f, 0.33f)),
            ("Hydro_Glass", new Color(0.34f, 0.47f, 0.60f)),
            ("Hydro_Water", new Color(0.26f, 0.46f, 0.58f)),
            ("Hydro_Foam", new Color(0.86f, 0.90f, 0.91f)),
            ("Hydro_Rock", new Color(0.46f, 0.44f, 0.42f)),
            ("Hydro_Grass", new Color(0.28f, 0.45f, 0.22f)),
        };

        /// <summary>Authored 1.04 units wide — a dam has to span its hex, so this footprint
        /// is wider than the other tiles'. Fitted against the hexagon itself, not a bounding
        /// box: the worst corner sits about 10% inside the nearest edge. Re-measure if the
        /// footprint changes.</summary>
        private const float DisplayScale = 1.26f;

        /// <summary>The board camera is fixed at yaw 0 and the FBX axis conversion lands
        /// the model facing away from it. This turns the spillway, the plunge pool and the
        /// powerhouse windows downstream toward the viewer, with the held water behind.</summary>
        private const float DisplayYaw = -175f;

        [MenuItem("Solarpunk/Build Hydro Model Prefab")]
        public static GameObject BuildPrefab()
        {
            var importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"[Hydro] No model at {FbxPath}. Run ArtSource/build_hidreletrica.py " +
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
                Debug.LogError($"[Hydro] {FbxPath} failed to load after reimport.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction);
            instance.name = "Hidreletrica";
            instance.transform.localScale = Vector3.one * DisplayScale;
            instance.transform.localRotation = Quaternion.Euler(0f, DisplayYaw, 0f);

            var renderer = instance.GetComponentInChildren<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("[Hydro] Imported model has no MeshRenderer.");
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
            Debug.Log($"[Hydro] Prefab built: {mesh.triangles.Length / 3} tris, " +
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
