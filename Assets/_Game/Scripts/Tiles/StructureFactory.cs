using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// Builds the throwaway primitive "buildings" that sit on a hex. Real art
    /// replaces this wholesale later — the only contract is that it returns a
    /// GameObject whose origin sits on the hex surface and that never blocks
    /// the click raycast (all colliders are stripped).
    /// </summary>
    public static class StructureFactory
    {
        public static Material CreateColoredMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard")
                            ?? Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Diffuse");

            var material = new Material(shader);
            if (material.HasProperty("_Color")) material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            return material;
        }

        public static GameObject Create(TileDefinition definition)
        {
            var root = new GameObject($"Structure_{definition.displayName}");
            Material material = CreateColoredMaterial(definition.placeholderColor);

            switch (definition.category)
            {
                case TileCategory.City:
                    AddPart(root, PrimitiveType.Cube, material, new Vector3(-0.16f, 0.18f, 0.05f), new Vector3(0.26f, 0.36f, 0.26f));
                    AddPart(root, PrimitiveType.Cube, material, new Vector3(0.14f, 0.28f, -0.08f), new Vector3(0.24f, 0.56f, 0.24f));
                    AddPart(root, PrimitiveType.Cube, material, new Vector3(0.02f, 0.13f, 0.24f), new Vector3(0.22f, 0.26f, 0.22f));
                    break;

                case TileCategory.PowerPlant:
                    AddPart(root, PrimitiveType.Cylinder, material, new Vector3(0f, 0.30f, 0f), new Vector3(0.30f, 0.30f, 0.30f));
                    AddPart(root, PrimitiveType.Cube, material, new Vector3(0f, 0.08f, 0f), new Vector3(0.66f, 0.16f, 0.66f));
                    break;

                case TileCategory.Extraction:
                    AddPart(root, PrimitiveType.Cube, material, new Vector3(0f, 0.07f, 0f), new Vector3(0.62f, 0.14f, 0.62f));
                    AddPart(root, PrimitiveType.Cube, material, new Vector3(0f, 0.34f, 0f), new Vector3(0.12f, 0.54f, 0.12f));
                    break;
            }

            return root;
        }

        private static void AddPart(GameObject parent, PrimitiveType type, Material material, Vector3 localPos, Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;

            // Structures must never intercept the hex-selection raycast.
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
        }
    }
}
