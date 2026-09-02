using System.Collections.Generic;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Tiles;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Generates the 10 starting TileDefinition assets (city, 8 power plants,
    /// extraction) with placeholder-but-plausible numbers derived from the
    /// qualitative profiles in docs/GameDesign.md §3. None of these numbers are
    /// balanced or playtested — tune them freely in the Inspector; this factory
    /// only exists so the assets don't have to be hand-typed.
    /// </summary>
    public static class TileDataFactory
    {
        private const string OutputDir = "Assets/_Game/Data/TileDefinitions";

        /// <summary>Canonical build-menu order: city first, then plants, then extraction.</summary>
        public static readonly string[] OrderedNames =
        {
            "City", "Hidreletrica", "Maremotriz", "Eolica", "Solar",
            "Nuclear", "Biomassa", "Carvao", "Petroleo", "Extracao"
        };

        /// <summary>
        /// Loads the definitions fresh off disk in menu order. Always use this
        /// rather than the list returned by <see cref="GenerateAll"/> — those
        /// in-memory references can be invalidated by the AssetDatabase refresh
        /// that follows creation.
        /// </summary>
        public static List<TileDefinition> LoadAll()
        {
            var loaded = new List<TileDefinition>();

            foreach (string name in OrderedNames)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TileDefinition>($"{OutputDir}/{name}.asset");
                if (asset == null) Debug.LogError($"Tile definition '{name}' failed to load from {OutputDir}.");
                else loaded.Add(asset);
            }

            return loaded;
        }

        [MenuItem("Solarpunk/Generate Starting Tile Definitions")]
        public static List<TileDefinition> GenerateAll()
        {
            System.IO.Directory.CreateDirectory(OutputDir);

            var results = new List<TileDefinition>
            {
                // Per-turn effect for the city comes from CityGrowth (scales with level), not this vector.
                Create("City", TileCategory.City, TerrainRelief.Mutable, 200f,
                    ResourceVector.Zero, false, new Color(0.93f, 0.90f, 0.82f)),

                Create("Hidreletrica", TileCategory.PowerPlant, TerrainRelief.Waterfall, 800f,
                    new ResourceVector { energy = 40f, sustainability = 1f, money = -5f }, false,
                    new Color(0.35f, 0.62f, 0.92f)),

                Create("Maremotriz", TileCategory.PowerPlant, TerrainRelief.Coast, 1200f,
                    new ResourceVector { energy = 25f, sustainability = 2f, money = -6f }, false,
                    new Color(0.30f, 0.80f, 0.78f)),

                Create("Eolica", TileCategory.PowerPlant, TerrainRelief.Mutable, 400f,
                    new ResourceVector { energy = 18f, sustainability = 1f, money = -3f }, false,
                    new Color(0.85f, 0.93f, 0.96f)),

                Create("Solar", TileCategory.PowerPlant, TerrainRelief.Mutable, 250f,
                    new ResourceVector { energy = 15f, sustainability = 1f, money = -2f }, false,
                    new Color(0.97f, 0.82f, 0.30f)),

                Create("Nuclear", TileCategory.PowerPlant, TerrainRelief.Mutable, 3000f,
                    new ResourceVector { energy = 80f, sustainability = 0f, money = -15f }, true,
                    new Color(0.72f, 0.55f, 0.90f)),

                Create("Biomassa", TileCategory.PowerPlant, TerrainRelief.Mutable, 350f,
                    new ResourceVector { energy = 12f, sustainability = -1f, money = -2f }, false,
                    new Color(0.55f, 0.62f, 0.30f)),

                Create("Carvao", TileCategory.PowerPlant, TerrainRelief.Mutable, 300f,
                    new ResourceVector { energy = 35f, sustainability = -6f, money = -3f }, true,
                    new Color(0.26f, 0.26f, 0.28f)),

                Create("Petroleo", TileCategory.PowerPlant, TerrainRelief.Mutable, 900f,
                    new ResourceVector { energy = 35f, sustainability = -6f, money = -10f }, true,
                    new Color(0.36f, 0.26f, 0.20f)),

                Create("Extracao", TileCategory.Extraction, TerrainRelief.Mutable, 500f,
                    new ResourceVector { money = 20f, sustainability = -2f }, false,
                    new Color(0.85f, 0.50f, 0.22f)),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {results.Count} tile definitions in {OutputDir}.");
            return results;
        }

        private static TileDefinition Create(string name, TileCategory category, TerrainRelief relief,
            float cost, ResourceVector effect, bool requiresExtraction, Color placeholderColor)
        {
            var asset = ScriptableObject.CreateInstance<TileDefinition>();
            asset.displayName = name;
            asset.category = category;
            asset.requiredRelief = relief;
            asset.buildCost = cost;
            asset.perTurnEffect = effect;
            asset.requiresExtraction = requiresExtraction;
            asset.placeholderColor = placeholderColor;

            string path = $"{OutputDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
