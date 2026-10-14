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
    /// Generates the TileDefinition assets (city, power plants, extractions,
    /// forest) with placeholder-but-plausible numbers derived from the
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
            "City", "Hidreletrica", "Maremotriz", "Ondas", "Eolica", "Solar", "Geotermica",
            "Nuclear", "Fusao", "Biomassa", "Carvao", "Petroleo", "GasNatural",
            "ExtracaoCarvao", "ExtracaoPetroleo", "ExtracaoGas", "ExtracaoUranio", "Floresta"
        };

        /// <summary>The definitions the build menu offers. Forests are in it, locked until Reforestation.</summary>
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

                Create("Nuclear", TileCategory.PowerPlant, Anywhere, 3000f,
                    new ResourceVector { energy = 80f, sustainability = 0f, money = -15f },
                    new Color(0.72f, 0.55f, 0.90f),
                    requiredFuel: FuelType.Uranium),

                Create("Biomassa", TileCategory.PowerPlant, Anywhere, 350f,
                    new ResourceVector { energy = 12f, sustainability = -1f, money = -2f },
                    new Color(0.55f, 0.62f, 0.30f)),

                Create("Carvao", TileCategory.PowerPlant, Anywhere, 300f,
                    new ResourceVector { energy = 10f, sustainability = -3f, money = -3f },
                    new Color(0.26f, 0.26f, 0.28f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(CoalPlantModelBuilder.PrefabPath),
                    requiredFuel: FuelType.Coal,
                    tags: Fossil),

                Create("Petroleo", TileCategory.PowerPlant, Anywhere, 900f,
                    new ResourceVector { energy = 35f, sustainability = -6f, money = -10f },
                    new Color(0.36f, 0.26f, 0.20f),
                    requiredFuel: FuelType.Oil,
                    tags: Fossil),

                // Numbers for the tiles below come from the research tree's unlock text where it
                // gives them (Wave, Geothermal, Gas plant, Fusion). The extractions' figures are
                // the tree's "before" values (oil +8 money / -2 sust, uranium -1 sust) with the
                // gaps guessed. All PLACEHOLDER, to tune.
                Create("Ondas", TileCategory.PowerPlant, Only(TerrainRelief.Coast), 600f,
                    new ResourceVector { energy = 10f, sustainability = 2f, money = -5f },
                    new Color(0.45f, 0.72f, 0.95f),
                    tags: Renewable),

                // Earthquake risk from the tree is not modelled: there are no per-tile risk rolls.
                Create("Geotermica", TileCategory.PowerPlant, Only(TerrainRelief.Mountain), 900f,
                    new ResourceVector { energy = 20f, sustainability = 1f, money = -4f },
                    new Color(0.86f, 0.38f, 0.26f),
                    tags: Renewable),

                Create("Fusao", TileCategory.PowerPlant, Anywhere, 5000f,
                    new ResourceVector { energy = 120f, money = -25f },
                    new Color(0.95f, 0.55f, 0.85f)),

                Create("GasNatural", TileCategory.PowerPlant, Anywhere, 700f,
                    new ResourceVector { energy = 30f, sustainability = -2f, money = -6f },
                    new Color(0.55f, 0.70f, 0.82f),
                    requiredFuel: FuelType.Gas,
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

                // One well until Oil Tracking. Drilled on land or just offshore.
                Create("ExtracaoPetroleo", TileCategory.Extraction,
                    Only(TerrainRelief.Mutable, TerrainRelief.Coast), 600f,
                    new ResourceVector { money = 8f, sustainability = -2f },
                    new Color(0.22f, 0.18f, 0.16f),
                    producedFuel: FuelType.Oil, displayName: "Extracao de Petroleo",
                    tags: FossilExtraction, maxOnBoard: 1),

                // Methane leaks are Natural Gas Discovery's tradeoff, so the base S is the -1 before it.
                Create("ExtracaoGas", TileCategory.Extraction,
                    Only(TerrainRelief.Mutable, TerrainRelief.Coast), 500f,
                    new ResourceVector { money = 10f, sustainability = -1f },
                    new Color(0.70f, 0.80f, 0.88f),
                    producedFuel: FuelType.Gas, displayName: "Extracao de Gas",
                    tags: FossilExtraction),

                Create("ExtracaoUranio", TileCategory.Extraction,
                    Only(TerrainRelief.Mutable, TerrainRelief.Mountain), 800f,
                    new ResourceVector { money = 4f, sustainability = -1f },
                    new Color(0.62f, 0.85f, 0.30f),
                    producedFuel: FuelType.Uranium, displayName: "Extracao de Uranio"),

                // The board seeds these on open ground itself, already grown. Reforestation
                // puts them in the build menu: $150 to plant, nothing for 10 years while it grows.
                Create("Floresta", TileCategory.Forest, Only(TerrainRelief.Mutable), 150f,
                    new ResourceVector { sustainability = 3f },
                    new Color(0.15f, 0.37f, 0.20f),
                    AssetDatabase.LoadAssetAtPath<GameObject>(TreeModelBuilder.PrefabPath),
                    clearCost: 200f, maturesAfter: 10),
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
            TerrainBonus[] terrainBonuses = null, string[] tags = null,
            int maxOnBoard = 0, int maturesAfter = 0)
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
            asset.maxOnBoard = maxOnBoard;
            asset.maturesAfter = maturesAfter;
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
