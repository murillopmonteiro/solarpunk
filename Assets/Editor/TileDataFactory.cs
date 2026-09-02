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
    /// qualitative profiles in docs/GameDesign.md §3. None of these numbers
    /// are balanced/playtested — tune freely in the Inspector, this factory
    /// only exists to avoid hand-typing 10 assets once.
    /// </summary>
    public static class TileDataFactory
    {
        private const string OutputDir = "Assets/_Game/Data/TileDefinitions";

        [MenuItem("Solarpunk/Generate Starting Tile Definitions")]
        public static List<TileDefinition> GenerateAll()
        {
            System.IO.Directory.CreateDirectory(OutputDir);

            var results = new List<TileDefinition>
            {
                Create("City", TileCategory.City, TerrainRelief.Mutable, 0f,
                    ResourceVector.Zero, false), // per-turn effect comes from CityGrowth, not this vector

                Create("Hidreletrica", TileCategory.PowerPlant, TerrainRelief.Waterfall, 800f,
                    new ResourceVector { energy = 40f, sustainability = 1f, money = -5f }, false),

                Create("Maremotriz", TileCategory.PowerPlant, TerrainRelief.Coast, 1200f,
                    new ResourceVector { energy = 25f, sustainability = 2f, money = -6f }, false),

                Create("Eolica", TileCategory.PowerPlant, TerrainRelief.Mutable, 400f,
                    new ResourceVector { energy = 18f, sustainability = 1f, money = -3f }, false),

                Create("Solar", TileCategory.PowerPlant, TerrainRelief.Mutable, 250f,
                    new ResourceVector { energy = 15f, sustainability = 1f, money = -2f }, false),

                Create("Nuclear", TileCategory.PowerPlant, TerrainRelief.Mutable, 3000f,
                    new ResourceVector { energy = 80f, sustainability = 0f, money = -15f }, true),

                Create("Biomassa", TileCategory.PowerPlant, TerrainRelief.Mutable, 350f,
                    new ResourceVector { energy = 12f, sustainability = -1f, money = -2f }, false),

                Create("Carvao", TileCategory.PowerPlant, TerrainRelief.Mutable, 300f,
                    new ResourceVector { energy = 35f, sustainability = -6f, money = -3f }, true),

                Create("Petroleo", TileCategory.PowerPlant, TerrainRelief.Mutable, 900f,
                    new ResourceVector { energy = 35f, sustainability = -6f, money = -10f }, true),

                Create("Extracao", TileCategory.Extraction, TerrainRelief.Mutable, 500f,
                    new ResourceVector { money = 20f, sustainability = -2f }, false),
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {results.Count} tile definitions in {OutputDir}.");
            return results;
        }

        private static TileDefinition Create(string name, TileCategory category, TerrainRelief relief,
            float cost, ResourceVector effect, bool requiresExtraction)
        {
            var asset = ScriptableObject.CreateInstance<TileDefinition>();
            asset.displayName = name;
            asset.category = category;
            asset.requiredRelief = relief;
            asset.buildCost = cost;
            asset.perTurnEffect = effect;
            asset.requiresExtraction = requiresExtraction;

            string path = $"{OutputDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
