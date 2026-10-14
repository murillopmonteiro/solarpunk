using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Research;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Generates the research node assets from docs/ResearchTree.json.
    ///
    /// The tree is designed in the research tree artifact (an editable page) and
    /// exported as that file: names, costs, positions, prerequisites and the
    /// effect and tradeoff text all come from it, so redesigning the tree means
    /// replacing the file and running Solarpunk → Generate Research Tree.
    ///
    /// The one thing the file cannot carry is what a node *does* in code terms,
    /// because its effects are prose. That mapping is <see cref="Effects"/> below,
    /// keyed by node id. A node with no entry there and no reason in
    /// <see cref="Pending"/> is still generated, marked "not active yet", so a
    /// node added in the artifact shows up in game before anyone wires it.
    /// </summary>
    public static class ResearchTreeFactory
    {
        private const string OutputDir = "Assets/_Game/Data/ResearchNodes";

        public static string SourcePath => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "ResearchTree.json"));

        // ------------------------------------------------------------------ JSON shape

        [Serializable]
        private class TreeFile
        {
            public NodeJson[] nodes;
        }

        /// <summary>Field names match the artifact's tree.json, which is why they are short.</summary>
        [Serializable]
        private class NodeJson
        {
            public string id;
            public string t;
            public string b;
            public float x;
            public float y;
            public string p;
            public string[] also;
            public bool any;
            public float cost;
            public string eff;
            public string trade;
        }

        // ------------------------------------------------------------------ what each node does

        private const ResourceType E = ResourceType.Energy;
        private const ResourceType M = ResourceType.Money;
        private const ResourceType S = ResourceType.Sustainability;
        private const ResourceType P = ResourceType.Population;
        private const ResourceType H = ResourceType.Happiness;

        /// <summary>
        /// The simulation side of each node, transcribed from its effect and
        /// tradeoff text in the artifact. Numbers are deltas against today's tile
        /// stats (coal thermal +10 → +12 is "+2"), so they stack with each other.
        /// </summary>
        private static readonly Dictionary<string, ResearchEffect[]> Effects = new()
        {
            // City
            ["cityDev"] = new[] { Cap(3), Trade(Out("city", S, -1, minLevel: 3)) },
            ["university"] = new[] { PerCity(1f, 3), Trade(Out("city", M, -3, minLevel: 3)) },
            ["trade"] = new[] { Out("city", M, +5, minLevel: 3), Trade(Out("city", S, -1, minLevel: 3)) },
            ["urban"] = new[] { Out("city", H, +0.5f, minLevel: 3), Trade(Out("city", E, -1, minLevel: 3)) },
            ["cityDev2"] = new[]
            {
                Cap(5),
                // Crowding: cleared by Hygiene Investments or Entertainment.
                Trade(Out("city", H, -1, minLevel: 5, suppressedBy: new[] { "hygiene", "nmv5b6gig" }))
            },
            ["hygiene"] = new[] { Out("city", P, +2), Trade(Out("city", M, -2)) },
            ["vertical"] = new[] { Cap(7), Out("city", E, +1, minLevel: 6), Trade(Out("city", H, -0.5f, minLevel: 6)) },
            ["buildingEff"] = new[] { Out("city", E, +2) },
            ["transit"] = new[] { Out("city", S, +1) },
            ["arcology"] = new[] { Cap(10), Out("city", S, +1, minLevel: 8) },
            ["ev"] = new[] { Out("city", S, +1), Trade(Out("city", E, -2)) },

            // Science
            ["research"] = new[] { PerLevel(0.5f), Trade(Year(M, -10)) },
            ["biomass"] = new[] { Unlock("Biomassa"), Trade(Fell("Biomassa", 20, suppressedBy: new[] { "sustBiomass" })) },
            ["sustBiomass"] = new[] { Out("Biomassa", S, +2, nextTo: "Floresta") },
            ["wind"] = new[]
            {
                // Community Energy turns the -0.5 next to a city into its own +0.5.
                Unlock("Eolica"), Trade(Out("Eolica", H, -0.5f, nextTo: "city", suppressedBy: new[] { "coops" }))
            },
            ["nuclear"] = new[] { Unlock("Nuclear"), Unlock("ExtracaoUranio"), Trade(Out("Nuclear", H, -1)) },
            ["genIV"] = new[] { Out("Nuclear", H, +1), Out("ExtracaoUranio", S, +1) },
            ["geothermal"] = new[] { Unlock("Geotermica") },
            ["fusion"] = new[] { Unlock("Fusao") },
            ["solar"] = new[] { Unlock("Solar") },
            ["computing"] = new[] { ResearchScale(1.25f), Trade(Year(E, -3)) },
            ["cheapSolar"] = new[] { Cost("Solar", 0.6f) },
            ["rooftop"] = new[] { Out("city", E, +2) },
            ["superconductors"] = new[] { Out("cat:PowerPlant", E, mul: 1.05f), Trade(Out("cat:PowerPlant", M, -2)) },

            // Energy & fossil
            // No storage cap exists, so the whole yearly surplus sells at $1, not just what overflows a cap.
            ["exports"] = new[] { Export(M, 1f), Trade(Export(H, -0.5f)) },
            ["hydroPow"] = new[] { Out("Hidreletrica", E, +8), Trade(Out("Hidreletrica", S, -1)) },
            ["coalPow"] = new[] { Out("Carvao", E, +2), Trade(Out("Carvao", H, -0.5f)) },
            ["coal2"] = new[] { Out("Carvao", E, +2), Trade(Out("Carvao", M, -2)) },
            ["tidal"] = new[] { Unlock("Maremotriz"), Trade(Out("Maremotriz", S, -1)) },
            ["oilDisc"] = new[] { Unlock("Petroleo"), Unlock("ExtracaoPetroleo") },
            ["oilExt"] = new[] { Out("ExtracaoPetroleo", M, +17) },
            ["oilTrack"] = new[] { Lift("ExtracaoPetroleo") },
            ["oilExport"] = new[] { Out("ExtracaoPetroleo", M, +10) },
            ["refinery"] = new[] { Out("ExtracaoPetroleo", H, +0.5f) },
            ["safeOil"] = new[] { Out("ExtracaoPetroleo", S, +1) },
            ["gasDisc"] = new[] { Unlock("GasNatural"), Unlock("ExtracaoGas"), Trade(Out("ExtracaoGas", S, -1)) },
            ["fracking"] = new[]
            {
                Out("ExtracaoGas", M, mul: 1.5f), Trade(Out("ExtracaoGas", S, -1)), Trade(Out("ExtracaoGas", H, -0.5f))
            },
            ["wave"] = new[] { Unlock("Ondas") },
            ["oilEnergy"] = new[] { Out("Petroleo", E, +7) },
            ["filtered"] = new[] { Out("Carvao", S, +1), Out("Petroleo", S, +1) },
            ["ccs"] = new[] { Out("tag:fossil", S, mul: 0.5f), Trade(Out("tag:fossil", E, mul: 0.8f)) },
            ["smartGrid"] = new[] { Out("cat:PowerPlant", E, mul: 1.1f) },

            // Industry & ecology
            ["industrial"] = new[] { Cost("all", 0.9f), Trade(Year(S, -1)) },
            ["massProd"] = new[] { Cost("cat:PowerPlant", 0.85f), Trade(Built("all", S, -5)) },
            ["mechMining"] = new[] { Out("ExtracaoCarvao", M, +6), Trade(Out("ExtracaoCarvao", H, -0.5f)) },
            ["recycling"] = new[] { Refund(0.25f) },
            ["circular"] = new[] { Out("city", M, +4), Refund(0.25f) },
            ["conservation"] = new[] { Out("Floresta", S, +1), Trade(Clear("Floresta", 2f)) },
            ["ecotourism"] = new[] { Out("Floresta", M, +2) },
            ["gardenCity"] = new[] { Out("city", S, +1, nextTo: "Floresta"), Out("city", H, +0.5f, nextTo: "Floresta") },
            ["coops"] = new[] { Out("Eolica", H, +0.5f, nextTo: "city"), Out("Solar", H, +0.5f, nextTo: "city") },
            ["reforest"] = new[] { Unlock("Floresta") },
            ["rewilding"] = new[] { Spread("Floresta", 25) },
            // PLACEHOLDER price: the tree only says it rises each time (x1.5 per hex, in BuildController).
            ["coastal"] = new[] { Territory(600f) },
            ["carbonTax"] = new[]
            {
                Out("city", H, +0.5f),
                Trade(Out("tag:fossil", M, -3)), Trade(Out("tag:fossil-extraction", M, -3))
            },
            ["feedIn"] = new[]
            {
                Cost("Eolica", 0.75f), Cost("Solar", 0.75f), Cost("Maremotriz", 0.75f),
                Trade(Out("Eolica", M, -2)), Trade(Out("Solar", M, -2)), Trade(Out("Maremotriz", M, -2))
            },
        };

        /// <summary>Gates: nodes whose whole job is to open others. No effect, and nothing pending.</summary>
        private static readonly HashSet<string> Gates = new() { "center", "thermo", "electromag", "atomic", "semis" };

        private const string NeedsStorage = "Needs an energy storage cap, which the game doesn't have yet.";
        private const string NeedsRisk = "Needs per-tile risk rolls (accidents, quakes, spills), not built yet.";
        private const string NoNumbers = "Its effect has no numbers yet.";

        /// <summary>Why a node, or part of one, does nothing yet. Shown on its card in game.</summary>
        private static readonly Dictionary<string, string> Pending = new()
        {
            ["vertical"] = "The population boost has no number yet.",
            ["geothermal"] = "Its earthquake tradeoff needs per-tile risk rolls, not built yet.",
            ["nmv5b5btw"] = NoNumbers + " Its earthquake tradeoff needs per-tile risk rolls.",
            ["offshore"] = "Needs research to change where a tile may be built.",
            ["nuclear"] = "The accident risk needs per-tile risk rolls, not built yet.",
            ["nmv5au1sk"] = NeedsRisk,
            ["nmv5avema"] = NeedsRisk,
            ["nmv5b017x"] = NeedsRisk,
            ["nmv5az390"] = NoNumbers,
            ["repository"] = "Needs nuclear plants to carry a waste cost first.",
            ["nmv5b3u8p"] = NoNumbers,
            ["tandemSolar"] = "Waits on Solar starting weaker, as the tree suggests.",
            ["forecast"] = "Needs wind and solar output to vary year to year, not modelled yet.",
            ["smartGrid"] = "The wind and solar swings it removes are not modelled yet.",
            ["storage"] = NeedsStorage,
            ["betterStor"] = NeedsStorage,
            ["pumpedHydro"] = NeedsStorage,
            ["safeOil"] = "The deck has no oil-spill event yet, so that half has nothing to stop.",
            ["greenH2"] = "Needs Gas and Oil plants that can drop their fuel.",
            ["justTrans"] = "Needs demolition to charge or pay by tile type.",
            ["envScience"] = "Needs the HUD forecast and a tradeoff that reacts to sustainability.",
            ["mineReclaim"] = "Needs demolition to offer a cheap replant on the cleared hex.",
            ["nmv5b6gig"] = NoNumbers + " Researching it already clears City Development 2's crowding.",
            ["nmv5c081l"] = "Needs tradeoffs that can be skipped by chance, not built yet.",
        };

        // ------------------------------------------------------------------ generation

        public static string[] OrderedNames => ReadSource().Select(n => n.id).ToArray();

        /// <summary>Loads the nodes fresh off disk, in file order.</summary>
        public static List<ResearchNode> LoadAll()
        {
            var loaded = new List<ResearchNode>();

            foreach (NodeJson json in ReadSource())
            {
                var asset = AssetDatabase.LoadAssetAtPath<ResearchNode>($"{OutputDir}/{json.id}.asset");
                if (asset == null) Debug.LogError($"Research node '{json.id}' failed to load from {OutputDir}.");
                else loaded.Add(asset);
            }

            return loaded;
        }

        [MenuItem("Solarpunk/Generate Research Tree")]
        public static void GenerateAll()
        {
            NodeJson[] source = ReadSource();
            Directory.CreateDirectory(OutputDir);

            // Two passes: every asset has to exist before prerequisites can point at them.
            var byId = new Dictionary<string, ResearchNode>();
            foreach (NodeJson json in source)
            {
                var asset = ScriptableObject.CreateInstance<ResearchNode>();
                asset.displayName = json.t;
                asset.description = json.eff ?? "";
                asset.tradeoff = json.trade ?? "";
                asset.cost = Mathf.RoundToInt(json.cost);
                asset.position = new Vector2(json.x, json.y);
                asset.branch = json.b;
                asset.requireAny = json.any;
                asset.effects = Effects.TryGetValue(json.id, out ResearchEffect[] effects)
                    ? effects.Select(Copy).ToList()
                    : new List<ResearchEffect>();
                asset.pendingNote = Pending.TryGetValue(json.id, out string note) ? note
                    : effects == null && !Gates.Contains(json.id) ? "No effect wired yet."
                    : "";

                string path = $"{OutputDir}/{json.id}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(asset, path);
                byId[json.id] = asset;
            }

            foreach (NodeJson json in source)
            {
                // The line-drawing parent comes first; the panel draws only that one.
                var names = new List<string>();
                if (!string.IsNullOrEmpty(json.p)) names.Add(json.p);
                if (json.also != null) names.AddRange(json.also.Where(a => a != json.p));

                ResearchNode node = byId[json.id];
                node.prerequisites = names
                    .Select(name =>
                    {
                        if (byId.TryGetValue(name, out ResearchNode found)) return found;
                        Debug.LogWarning($"Research node '{json.id}' needs '{name}', which is not in the tree.");
                        return null;
                    })
                    .Where(found => found != null)
                    .ToArray();
                EditorUtility.SetDirty(node);
            }

            foreach (string id in Effects.Keys.Where(id => !byId.ContainsKey(id)))
                Debug.LogWarning($"Effects are wired for '{id}', but the tree has no such node.");

            PruneStrayNodes(new HashSet<string>(byId.Keys));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int active = source.Count(n => Effects.ContainsKey(n.id));
            Debug.Log($"Generated {source.Length} research nodes in {OutputDir} from {SourcePath}: " +
                      $"{active} with effects, {source.Count(n => Pending.ContainsKey(n.id) || (!Effects.ContainsKey(n.id) && !Gates.Contains(n.id)))} " +
                      "fully or partly pending.");
        }

        private static NodeJson[] ReadSource()
        {
            if (!File.Exists(SourcePath))
                throw new FileNotFoundException($"Research tree source missing. Export tree.json from the artifact to {SourcePath}.");

            var file = JsonUtility.FromJson<TreeFile>(File.ReadAllText(SourcePath, System.Text.Encoding.UTF8));
            if (file?.nodes == null || file.nodes.Length == 0)
                throw new InvalidDataException($"{SourcePath} has no nodes.");

            return file.nodes;
        }

        private static void PruneStrayNodes(HashSet<string> keep)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ResearchNode", new[] { OutputDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (keep.Contains(name)) continue;

                Debug.Log($"Removing stale research node '{name}'.");
                AssetDatabase.DeleteAsset(path);
            }
        }

        // ------------------------------------------------------------------ effect builders

        /// <summary>"city", "all", "cat:PowerPlant", "tag:fossil", or a tile asset name.</summary>
        private static (EffectTarget, string) Target(string target)
        {
            if (target == "all") return (EffectTarget.All, "");
            if (target == "city") return (EffectTarget.Category, "City");
            if (target.StartsWith("cat:")) return (EffectTarget.Category, target.Substring(4));
            if (target.StartsWith("tag:")) return (EffectTarget.Tag, target.Substring(4));
            return (EffectTarget.Tile, target);
        }

        private static ResearchEffect Make(EffectKind kind, string target = "all")
        {
            (EffectTarget t, string key) = Target(target);
            return new ResearchEffect { kind = kind, target = t, key = key };
        }

        private static ResearchEffect Out(string target, ResourceType resource, float add = 0f, float mul = 1f,
            int minLevel = 0, string[] suppressedBy = null, string nextTo = null)
        {
            ResearchEffect effect = Make(EffectKind.TileOutput, target);
            effect.resource = resource;
            effect.add = add;
            effect.multiply = mul;
            effect.minCityLevel = minLevel;
            effect.suppressedBy = suppressedBy ?? Array.Empty<string>();
            // Same syntax as a target, matched against HexCell.NeighbourKeys.
            effect.nextTo = nextTo == null ? "" : nextTo == "city" ? "cat:City" : nextTo;
            return effect;
        }

        private static ResearchEffect Fell(string target, int everyYears, string[] suppressedBy = null)
        {
            ResearchEffect effect = Make(EffectKind.FellForest, target);
            effect.add = everyYears;
            effect.suppressedBy = suppressedBy ?? Array.Empty<string>();
            return effect;
        }

        private static ResearchEffect Spread(string target, int everyYears)
        {
            ResearchEffect effect = Make(EffectKind.SpreadForest, target);
            effect.add = everyYears;
            return effect;
        }

        private static ResearchEffect Lift(string tile) => Make(EffectKind.LiftBuildLimit, tile);

        private static ResearchEffect Territory(float basePrice)
        {
            ResearchEffect effect = Make(EffectKind.BuyTerritory);
            effect.add = basePrice;
            return effect;
        }

        private static ResearchEffect Cost(string target, float mul)
        {
            ResearchEffect effect = Make(EffectKind.BuildCost, target);
            effect.multiply = mul;
            return effect;
        }

        private static ResearchEffect Clear(string target, float mul)
        {
            ResearchEffect effect = Make(EffectKind.ClearCost, target);
            effect.multiply = mul;
            return effect;
        }

        private static ResearchEffect Built(string target, ResourceType resource, float add)
        {
            ResearchEffect effect = Make(EffectKind.OnBuild, target);
            effect.resource = resource;
            effect.add = add;
            return effect;
        }

        private static ResearchEffect Year(ResourceType resource, float add)
        {
            ResearchEffect effect = Make(EffectKind.PerYear);
            effect.resource = resource;
            effect.add = add;
            return effect;
        }

        private static ResearchEffect Unlock(string tile)
        {
            ResearchEffect effect = Make(EffectKind.UnlockTile, tile);
            effect.key = tile;
            return effect;
        }

        private static ResearchEffect Cap(int level)
        {
            ResearchEffect effect = Make(EffectKind.CityLevelCap);
            effect.add = level;
            return effect;
        }

        private static ResearchEffect PerLevel(float add)
        {
            ResearchEffect effect = Make(EffectKind.ResearchPerLevel);
            effect.add = add;
            return effect;
        }

        private static ResearchEffect PerCity(float add, int minLevel)
        {
            ResearchEffect effect = Make(EffectKind.ResearchPerCity);
            effect.add = add;
            effect.minCityLevel = minLevel;
            return effect;
        }

        private static ResearchEffect ResearchScale(float mul)
        {
            ResearchEffect effect = Make(EffectKind.ResearchMultiplier);
            effect.multiply = mul;
            return effect;
        }

        private static ResearchEffect Refund(float add)
        {
            ResearchEffect effect = Make(EffectKind.DemolishRefund);
            effect.add = add;
            return effect;
        }

        private static ResearchEffect Export(ResourceType resource, float add)
        {
            ResearchEffect effect = Make(EffectKind.EnergyExport);
            effect.resource = resource;
            effect.add = add;
            return effect;
        }

        private static ResearchEffect Trade(ResearchEffect effect)
        {
            effect.isTradeoff = true;
            return effect;
        }

        /// <summary>Each asset gets its own instance; the table entries are shared templates.</summary>
        private static ResearchEffect Copy(ResearchEffect source) => new ResearchEffect
        {
            kind = source.kind,
            target = source.target,
            key = source.key,
            resource = source.resource,
            add = source.add,
            multiply = source.multiply,
            minCityLevel = source.minCityLevel,
            nextTo = source.nextTo,
            isTradeoff = source.isTradeoff,
            suppressedBy = source.suppressedBy?.ToArray() ?? Array.Empty<string>()
        };
    }
}
