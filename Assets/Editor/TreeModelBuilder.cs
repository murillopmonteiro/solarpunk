using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Turns the authored tree into the forest tile's prefab.
    ///
    /// The source is a binary STL, which Unity cannot import at all — so unlike
    /// the city FBX it stays outside Assets/ in ArtSource/, and the mesh asset
    /// under Assets/ is baked from it here. STL carries geometry and nothing
    /// else: no materials, no submeshes, no scale or axis convention beyond
    /// "Z is up". Everything else on this page is what fills those gaps.
    /// </summary>
    public static class TreeModelBuilder
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/Tree.prefab";

        private const string SourcePath = "ArtSource/tree-basic.stl";
        private const string MeshPath = "Assets/_Game/Meshes/Tree.asset";
        private const string MaterialDir = "Assets/_Game/Materials/Nature";

        /// <summary>Scaled to this height in hex units, which fills a tile without crowding it.</summary>
        private const float TargetHeight = 1.15f;

        /// <summary>
        /// With no material data in the file, wood and leaf are told apart by shape:
        /// each disconnected shell is wood if it is taller than it is wide. In this
        /// model that separates cleanly — trunk and branches measure 0.29 to 0.83,
        /// every foliage clump 1.03 or more. The split is logged; check it there if
        /// the model is ever re-exported.
        /// </summary>
        private const float WoodAspectLimit = 0.9f;

        private static readonly Color TrunkColor = new Color(0.33f, 0.23f, 0.16f);

        /// <summary>Well clear of the hex's own green (0.26, 0.55, 0.23) so the tree reads against it.</summary>
        private static readonly Color CanopyColor = new Color(0.15f, 0.37f, 0.20f);

        [MenuItem("Solarpunk/Build Tree Model Prefab")]
        public static GameObject BuildPrefab()
        {
            List<Vector3[]> facets = ReadStl(SourcePath);
            if (facets == null) return null;

            Mesh mesh = BuildMesh(facets);

            System.IO.Directory.CreateDirectory("Assets/_Game/Meshes");
            AssetDatabase.DeleteAsset(MeshPath);
            AssetDatabase.CreateAsset(mesh, MeshPath);
            AssetDatabase.SaveAssets();

            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);

            var root = new GameObject("Tree");
            root.AddComponent<MeshFilter>().sharedMesh = saved;
            root.AddComponent<MeshRenderer>().sharedMaterials = new[]
            {
                CreateMaterial("Tree_Trunk", TrunkColor),
                CreateMaterial("Tree_Canopy", CanopyColor),
            };

            System.IO.Directory.CreateDirectory("Assets/_Game/Prefabs");
            AssetDatabase.DeleteAsset(PrefabPath);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            Debug.Log($"[Tree] Prefab built from {SourcePath}: {saved.triangles.Length / 3} tris, " +
                      $"bounds {saved.bounds.size}.");
            return prefab;
        }

        /// <summary>
        /// Binary STL: 80 bytes of ignored header, a triangle count, then 50 bytes
        /// per facet (a normal we discard, three vertices, two attribute bytes).
        /// Vertices come back already in Unity's axes.
        /// </summary>
        private static List<Vector3[]> ReadStl(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[Tree] No model at {path}.");
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            if (bytes.Length < 84)
            {
                Debug.LogError($"[Tree] {path} is too short to be an STL.");
                return null;
            }

            int count = (int)System.BitConverter.ToUInt32(bytes, 80);
            if (bytes.Length != 84 + 50 * count)
            {
                Debug.LogError($"[Tree] {path} is not a binary STL: {bytes.Length} bytes, but a " +
                               $"{count}-triangle binary STL would be {84 + 50 * count}.");
                return null;
            }

            var facets = new List<Vector3[]>(count);
            for (int i = 0; i < count; i++)
            {
                int offset = 84 + i * 50 + 12; // skip the facet normal; normals are recalculated
                var facet = new Vector3[3];
                for (int v = 0; v < 3; v++)
                {
                    int o = offset + v * 12;
                    facet[v] = ToUnityAxes(
                        System.BitConverter.ToSingle(bytes, o),
                        System.BitConverter.ToSingle(bytes, o + 4),
                        System.BitConverter.ToSingle(bytes, o + 8));
                }
                facets.Add(facet);
            }

            return facets;
        }

        /// <summary>
        /// STL is Z-up and right-handed; Unity is Y-up and left-handed. Swapping the
        /// two axes converts it, at the cost of mirroring handedness — which is why
        /// every triangle below is emitted wound backwards, or the tree renders
        /// inside out.
        /// </summary>
        private static Vector3 ToUnityAxes(float x, float y, float z) => new Vector3(x, z, y);

        private static Mesh BuildMesh(List<Vector3[]> facets)
        {
            int[] shellOfFacet = FindShells(facets, out int shellCount);
            bool[] shellIsWood = ClassifyShells(facets, shellOfFacet, shellCount);

            // Sit the tree on the hex surface (base at y = 0), centred on the hex, and
            // scaled to a fixed height so it lands right whatever units it was authored in.
            Bounds bounds = Measure(facets);
            float scale = bounds.size.y > 0f ? TargetHeight / bounds.size.y : 1f;
            var offset = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);

            var vertices = new List<Vector3>(facets.Count * 3);
            var wood = new List<int>();
            var leaf = new List<int>();

            for (int i = 0; i < facets.Count; i++)
            {
                int baseIndex = vertices.Count;
                foreach (Vector3 vertex in facets[i]) vertices.Add((vertex - offset) * scale);

                List<int> target = shellIsWood[shellOfFacet[i]] ? wood : leaf;
                target.Add(baseIndex);
                target.Add(baseIndex + 2); // reversed: the axis swap mirrored handedness
                target.Add(baseIndex + 1);
            }

            var mesh = new Mesh { name = "Tree", subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(wood, 0);
            mesh.SetTriangles(leaf, 1);
            mesh.RecalculateNormals(); // per-facet vertices, so the facets stay flat
            mesh.RecalculateBounds();

            Debug.Log($"[Tree] {shellCount} shells: {wood.Count / 3} wood tris, {leaf.Count / 3} leaf tris.");
            return mesh;
        }

        /// <summary>
        /// Groups facets into disconnected shells by union-find over shared vertex
        /// positions. STL repeats a position for every facet that touches it, so
        /// welding on rounded coordinates is what makes two facets "connected" at all.
        /// </summary>
        private static int[] FindShells(List<Vector3[]> facets, out int shellCount)
        {
            var welded = new Dictionary<Vector3, int>();
            var facetVertices = new int[facets.Count][];

            for (int i = 0; i < facets.Count; i++)
            {
                facetVertices[i] = new int[3];
                for (int v = 0; v < 3; v++)
                {
                    Vector3 key = Round(facets[i][v]);
                    if (!welded.TryGetValue(key, out int id))
                    {
                        id = welded.Count;
                        welded[key] = id;
                    }
                    facetVertices[i][v] = id;
                }
            }

            var parent = new int[welded.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;

            foreach (int[] facet in facetVertices)
            {
                Union(parent, facet[0], facet[1]);
                Union(parent, facet[1], facet[2]);
            }

            var shellIds = new Dictionary<int, int>();
            var result = new int[facets.Count];
            for (int i = 0; i < facets.Count; i++)
            {
                int root = Find(parent, facetVertices[i][0]);
                if (!shellIds.TryGetValue(root, out int shell))
                {
                    shell = shellIds.Count;
                    shellIds[root] = shell;
                }
                result[i] = shell;
            }

            shellCount = shellIds.Count;
            return result;
        }

        private static bool[] ClassifyShells(List<Vector3[]> facets, int[] shellOfFacet, int shellCount)
        {
            var shellBounds = new Bounds[shellCount];
            var seeded = new bool[shellCount];

            for (int i = 0; i < facets.Count; i++)
            {
                int shell = shellOfFacet[i];
                foreach (Vector3 vertex in facets[i])
                {
                    if (!seeded[shell])
                    {
                        shellBounds[shell] = new Bounds(vertex, Vector3.zero);
                        seeded[shell] = true;
                    }
                    else
                    {
                        shellBounds[shell].Encapsulate(vertex);
                    }
                }
            }

            var isWood = new bool[shellCount];
            for (int i = 0; i < shellCount; i++)
            {
                Vector3 size = shellBounds[i].size;
                float width = Mathf.Max(size.x, size.z);
                isWood[i] = size.y > 0f && width / size.y < WoodAspectLimit;
            }

            return isWood;
        }

        private static Bounds Measure(List<Vector3[]> facets)
        {
            var bounds = new Bounds(facets[0][0], Vector3.zero);
            foreach (Vector3[] facet in facets)
            {
                foreach (Vector3 vertex in facet) bounds.Encapsulate(vertex);
            }
            return bounds;
        }

        /// <summary>Rounded so positions that should coincide actually compare equal.</summary>
        private static Vector3 Round(Vector3 v) => new Vector3(
            Mathf.Round(v.x * 10000f) / 10000f,
            Mathf.Round(v.y * 10000f) / 10000f,
            Mathf.Round(v.z * 10000f) / 10000f);

        private static int Find(int[] parent, int a)
        {
            while (parent[a] != a)
            {
                parent[a] = parent[parent[a]];
                a = parent[a];
            }
            return a;
        }

        private static void Union(int[] parent, int a, int b)
        {
            int rootA = Find(parent, a);
            int rootB = Find(parent, b);
            if (rootA != rootB) parent[rootB] = rootA;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            System.IO.Directory.CreateDirectory(MaterialDir);
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");

            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.05f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);

            string path = $"{MaterialDir}/{name}.mat";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(material, path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }
}
