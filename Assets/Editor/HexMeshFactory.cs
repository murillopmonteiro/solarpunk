using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Generates a pointy-top hexagonal prism mesh asset. Circumradius 1, top
    /// face at y = 0 so structures placed at the cell origin sit on the surface.
    /// </summary>
    public static class HexMeshFactory
    {
        public const string MeshPath = "Assets/_Game/Meshes/HexPrism.asset";
        private const float Height = 0.35f;

        [MenuItem("Solarpunk/Generate Hex Mesh")]
        public static Mesh GenerateAndSave()
        {
            Mesh mesh = Build();
            System.IO.Directory.CreateDirectory("Assets/_Game/Meshes");
            AssetDatabase.DeleteAsset(MeshPath);
            AssetDatabase.CreateAsset(mesh, MeshPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        }

        private static Mesh Build()
        {
            // Pointy-top: vertices sit at 30° + 60°·i, so one vertex lands at 90° (+Z).
            var ring = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (30f + 60f * i);
                ring[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            }

            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // --- Top face (fan around a center vertex) ---
            int topCenter = vertices.Count;
            vertices.Add(Vector3.zero);
            int topRingStart = vertices.Count;
            for (int i = 0; i < 6; i++) vertices.Add(ring[i]);

            for (int i = 0; i < 6; i++)
            {
                triangles.Add(topCenter);
                triangles.Add(topRingStart + (i + 1) % 6);
                triangles.Add(topRingStart + i);
            }

            // --- Bottom face ---
            int botCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -Height, 0f));
            int botRingStart = vertices.Count;
            for (int i = 0; i < 6; i++) vertices.Add(ring[i] + Vector3.down * Height);

            for (int i = 0; i < 6; i++)
            {
                triangles.Add(botCenter);
                triangles.Add(botRingStart + i);
                triangles.Add(botRingStart + (i + 1) % 6);
            }

            // --- Sides (own vertices so the edges stay flat-shaded) ---
            for (int i = 0; i < 6; i++)
            {
                Vector3 a = ring[i];
                Vector3 b = ring[(i + 1) % 6];

                int baseIndex = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(a + Vector3.down * Height);
                vertices.Add(b + Vector3.down * Height);

                triangles.Add(baseIndex);     // topA
                triangles.Add(baseIndex + 1); // topB
                triangles.Add(baseIndex + 2); // botA

                triangles.Add(baseIndex + 1); // topB
                triangles.Add(baseIndex + 3); // botB
                triangles.Add(baseIndex + 2); // botA
            }

            var mesh = new Mesh { name = "HexPrism" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
