using System.Collections.Generic;
using System.Linq;
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

        /// <summary>Canonical build-menu order: city first, then plants, then extractions.</summary>
        public static readonly string[] OrderedNames =
        {
            "City", "Hidreletrica", "Maremotriz", "Eolica", "Solar",
            "Nuclear", "Biomassa", "Carvao", "Petroleo", "ExtracaoCarvao", "Floresta"
        };

        /// <summary>The definitions the build menu offers. Forests are board-generated, not built.</summary>
        public static List<TileDefinition> LoadPalette() =>
            LoadAll().FindAll(d => d.playerBuildable);

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
                // Per-turn effect for the city comes from CityGrowth, not this vector. The
                // bonus here is the one-off happiness lift on founding. Nothing caps how
                // many of these the island can hold.
                Create("City", TileCategory.City, Anywhere, 200f,
                    ResourceVector.Zero, new Color(0.93f, 0.90f, 0.82f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(CityModelBuilder.PrefabPath),
                    new ResourceVector { happiness = 5f }),

                Create("Hidreletrica", TileCategory.PowerPlant, Only(TerrainRelief.Waterfall), 800f,
                    new ResourceVector { energy = 40f, sustainability = 1f, money = -5f },
                    new Color(0.35f, 0.62f, 0.92f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(HydroModelBuilder.PrefabPath),
                    tags: Renewable),

                Create("Maremotriz", TileCategory.PowerPlant, Only(TerrainRelief.Coast), 1200f,
                    new ResourceVector { energy = 25f, sustainability = 2f, money = -6f },
                    new Color(0.30f, 0.80f, 0.78f),
                    tags: Renewable),

                // Doc §2: a mountain has more wind and costs more to build on ("mais vento,
                // mais custo"). The research tree puts both at 20%.
                Create("Eolica", TileCategory.PowerPlant, Anywhere, 400f,
                    new ResourceVector { energy = 18f, sustainability = 1f, money = -3f },
                    new Color(0.85f, 0.93f, 0.96f),
                    tags: Renewable,
                    terrainBonuses: new[]
                    {
                        new TerrainBonus
                        {
                            relief = TerrainRelief.Mountain,
                            resource = ResourceType.Energy,
                            outputMultiplier = 1.2f,
                            costMultiplier = 1.2f,
                        },
                    }),

                Create("Solar", TileCategory.PowerPlant, Anywhere, 250f,
                    new ResourceVector { energy = 15f, sustainability = 1f, money = -2f },
                    new Color(0.97f, 0.82f, 0.30f),
                    tags: Renewable),

                // TODO: requiredFuel = FuelType.Uranium once a uranium extraction tile exists.
                // Gating it now would make nuclear permanently unbuildable.
                Create("Nuclear", TileCategory.PowerPlant, Anywhere, 3000f,
                    new ResourceVector { energy = 80f, sustainability = 0f, money = -15f },
                    new Color(0.72f, 0.55f, 0.90f)),

                Create("Biomassa", TileCategory.PowerPlant, Anywhere, 350f,
                    new ResourceVector { energy = 12f, sustainability = -1f, money = -2f },
                    new Color(0.55f, 0.62f, 0.30f)),

                Create("Carvao", TileCategory.PowerPlant, Anywhere, 300f,
                    new ResourceVector { energy = 10f, sustainability = -3f, money = -3f },
                    new Color(0.26f, 0.26f, 0.28f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(CoalPlantModelBuilder.PrefabPath),
                    requiredFuel: FuelType.Coal,
                    tags: Fossil),

                // TODO: requiredFuel = FuelType.Oil once an oil extraction tile exists.
                Create("Petroleo", TileCategory.PowerPlant, Anywhere, 900f,
                    new ResourceVector { energy = 35f, sustainability = -6f, money = -10f },
                    new Color(0.36f, 0.26f, 0.20f),
                    tags: Fossil),

                // The reference triad's "Coal Extraction": +20 money, -1 sustainability.
                // Coal is dug out of grass or rock, never out of water, and a mountain
                // seam pays better and opens cheaper: $400 and +24 money there.
                // Sibling extractions (oil, uranium) drop in here as further entries —
                // same shape, a different producedFuel, their own numbers.
                Create("ExtracaoCarvao", TileCategory.Extraction,
                    Only(TerrainRelief.Mutable, TerrainRelief.Mountain), 500f,
                    new ResourceVector { money = 20f, sustainability = -1f },
                    new Color(0.85f, 0.50f, 0.22f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(CoalMineModelBuilder.PrefabPath),
                    producedFuel: FuelType.Coal, displayName: "Extracao de Carvao",
                    tags: FossilExtraction,
                    terrainBonuses: new[]
                    {
                        new TerrainBonus
                        {
                            relief = TerrainRelief.Mountain,
                            resource = ResourceType.Money,
                            outputMultiplier = 1.2f,
                            costMultiplier = 0.8f,
                        },
                    }),

                // Not in the build menu: the board seeds these on open ground itself.
                // Free standing sustainability, and the price of a hex is felling one.
                Create("Floresta", TileCategory.Forest, Only(TerrainRelief.Mutable), 0f,
                    new ResourceVector { sustainability = 3f },
                    new Color(0.15f, 0.37f, 0.20f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(TreeModelBuilder.PrefabPath),
                    clearCost: 200f, playerBuildable: false),
            };

            PruneStrayDefinitions();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {results.Count} tile definitions in {OutputDir}.");
            return results;
        }

        /// <summary>
        /// Deletes definitions left behind by an earlier run under a name this one
        /// no longer produces. Without it a renamed tile lingers as a second asset
        /// that is in no palette but still counted by SceneValidator.
        /// </summary>
        private static void PruneStrayDefinitions()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:TileDefinition", new[] { OutputDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (OrderedNames.Contains(name)) continue;

                Debug.Log($"Removing stale tile definition '{name}'.");
                AssetDatabase.DeleteAsset(path);
            }
        }

        /// <summary>Reliefs a tile accepts. Empty means it goes anywhere.</summary>
        private static readonly TerrainRelief[] Anywhere = System.Array.Empty<TerrainRelief>();

        // Groups research effects target, so "every fossil plant" needs no list of tiles.
        private static readonly string[] Renewable = { "renewable" };
        private static readonly string[] Fossil = { "fossil" };
        private static readonly string[] FossilExtraction = { "fossil-extraction" };

        private static TerrainRelief[] Only(params TerrainRelief[] reliefs) => reliefs;

        private static TileDefinition Create(string name, TileCategory category, TerrainRelief[] allowedRelief,
            float cost, ResourceVector effect, Color placeholderColor,
            GameObject structurePrefab = null, ResourceVector onBuildBonus = default,
            FuelType requiredFuel = FuelType.None, FuelType producedFuel = FuelType.None,
            string displayName = null, float clearCost = 0f, bool playerBuildable = true,
            TerrainBonus[] terrainBonuses = null, string[] tags = null)
        {
            var asset = ScriptableObject.CreateInstance<TileDefinition>();
            asset.displayName = displayName ?? name;
            asset.category = category;
            asset.allowedRelief = allowedRelief;
            asset.terrainBonuses = terrainBonuses == null
                ? new List<TerrainBonus>()
                : new List<TerrainBonus>(terrainBonuses);
            asset.buildCost = cost;
            asset.clearCost = clearCost;
            asset.playerBuildable = playerBuildable;
            asset.perTurnEffect = effect;
            asset.requiredFuel = requiredFuel;
            asset.producedFuel = producedFuel;
            asset.tags = tags ?? System.Array.Empty<string>();
            asset.placeholderColor = placeholderColor;
            asset.structurePrefab = structurePrefab;
            asset.onBuildBonus = onBuildBonus;

            string path = $"{OutputDir}/{name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
