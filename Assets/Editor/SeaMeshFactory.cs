using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Generates the faceted low-poly sea the island floats in: a jittered
    /// triangle grid, one flat colour per triangle, so it shades like the models.
    ///
    /// Colour is baked by position rather than lit. A pale lagoon rings the island,
    /// deepening outward, and the far edge (the top of the screen from the board
    /// camera) hazes into a warm dawn tone, standing in for a sky the camera's
    /// pitch never shows.
    /// </summary>
    public static class SeaMeshFactory
    {
        public const string MeshPath = "Assets/_Game/Meshes/Sea.asset";

        // Covers the board camera's whole view of the y = 0 plane, with margin.
        private const float MinX = -18f, MaxX = 18f, MinZ = -9f, MaxZ = 16f;
        private const float Cell = 0.9f;
        private const float Jitter = 0.32f;

        private static readonly Color Lagoon = new Color(0.66f, 0.88f, 0.82f);
        private static readonly Color Shallow = new Color(0.47f, 0.78f, 0.79f);
        private static readonly Color Deep = new Color(0.31f, 0.62f, 0.69f);
        private static readonly Color Haze = new Color(0.97f, 0.89f, 0.74f);

        [MenuItem("Solarpunk/Generate Sea Mesh")]
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
            int cols = Mathf.CeilToInt((MaxX - MinX) / Cell);
            int rows = Mathf.CeilToInt((MaxZ - MinZ) / Cell);
            var random = new System.Random(7); // fixed seed: the rebuild must be reproducible

            // Shared grid points, nudged so the triangles do not read as a lattice.
            // Edge points stay put so the sheet keeps a straight border.
            var points = new Vector3[cols + 1, rows + 1];
            for (int z = 0; z <= rows; z++)
            {
                for (int x = 0; x <= cols; x++)
                {
                    bool edge = x == 0 || z == 0 || x == cols || z == rows;
                    float jx = edge ? 0f : ((float)random.NextDouble() - 0.5f) * 2f * Jitter;
                    float jz = edge ? 0f : ((float)random.NextDouble() - 0.5f) * 2f * Jitter;
                    points[x, z] = new Vector3(MinX + x * Cell + jx, 0f, MinZ + z * Cell + jz);
                }
            }

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 centre = (a + b + c) / 3f;
                float shade = 1f + ((float)random.NextDouble() - 0.5f) * 0.07f;
                Color color = ColorAt(centre) * shade;
                color.a = 1f;

                // Clockwise seen from above, so the face points up.
                foreach (Vector3 v in new[] { a, b, c })
                {
                    triangles.Add(vertices.Count);
                    vertices.Add(v);
                    colors.Add(color);
                }
            }

            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Vector3 p00 = points[x, z], p10 = points[x + 1, z];
                    Vector3 p01 = points[x, z + 1], p11 = points[x + 1, z + 1];

                    // Alternate the diagonal so the facets do not all lean one way.
                    if ((x + z) % 2 == 0)
                    {
                        Triangle(p00, p01, p11);
                        Triangle(p00, p11, p10);
                    }
                    else
                    {
                        Triangle(p00, p01, p10);
                        Triangle(p10, p01, p11);
                    }
                }
            }

            var mesh = new Mesh { name = "Sea" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Color ColorAt(Vector3 p)
        {
            float distance = new Vector2(p.x, p.z).magnitude;
            Color water = distance < 3.4f
                ? Color.Lerp(Lagoon, Shallow, Mathf.InverseLerp(2.6f, 3.4f, distance))
                : Color.Lerp(Shallow, Deep, Mathf.InverseLerp(3.4f, 9f, distance));

            return Color.Lerp(water, Haze, Mathf.InverseLerp(3.5f, 9.5f, p.z) * 0.85f);
        }
    }
}
