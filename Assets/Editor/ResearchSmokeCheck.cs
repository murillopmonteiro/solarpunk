using System.Collections.Generic;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Research;
using Solarpunk.Tiles;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Unlocks real nodes from the generated tree and checks the numbers they
    /// produce against the effect text in the artifact. Not a substitute for the
    /// test suite in PR #1, which is not on master yet; it exists so the tree can
    /// be verified headless today:
    /// -executeMethod Solarpunk.EditorTools.ResearchSmokeCheck.Run
    /// </summary>
    public static class ResearchSmokeCheck
    {
        private static readonly List<string> Failures = new();
        private static int _passed;

        [MenuItem("Solarpunk/Check Research Effects")]
        public static void Run()
        {
            Failures.Clear();
            _passed = 0;

            List<ResearchNode> nodes = ResearchTreeFactory.LoadAll();
            Dictionary<string, TileDefinition> tiles = TileDataFactory.LoadAll().ToDictionary(t => t.name);
            Dictionary<string, ResearchNode> byId = nodes.ToDictionary(n => n.name);

            var go = new GameObject("ResearchSmokeCheck");
            try
            {
                ResearchManager research = go.AddComponent<ResearchManager>();
                var so = new SerializedObject(research);
                SerializedProperty tree = so.FindProperty("tree");
                tree.ClearArray();
                for (int i = 0; i < nodes.Count; i++)
                {
                    tree.InsertArrayElementAtIndex(i);
                    tree.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                research.UnlockFreeRoots();

                Check("hub owned from year 0", research.IsUnlocked("center"));

                // Start palette: City, coal mine, coal plant, hydro. Everything else waits on research.
                foreach (string open in new[] { "City", "ExtracaoCarvao", "Carvao", "Hidreletrica", "Floresta" })
                    Check($"{open} open at start", !research.IsTileLocked(open));
                foreach (string locked in new[] { "Biomassa", "Eolica", "Solar", "Nuclear", "Maremotriz", "Petroleo" })
                    Check($"{locked} locked at start", research.IsTileLocked(locked));

                Check("city cap 2 before research", research.CityLevelCap(2) == 2);

                var cities = new List<int> { 3, 3 };
                Near("research income, two level-3 cities, no research", research.PointsPerTurn(cities), 6f);

                // Coal Power also needs Thermodynamics, which needs Research.
                Unlock(research, byId, "research", "thermo", "storage", "coalPow");
                TileFacts coal = TileEconomy.Facts(tiles["Carvao"]);
                ResourceVector coalOut = research.ModifyOutput(tiles["Carvao"].perTurnEffect, coal);
                Near("Coal Power: coal plant energy 10 -> 12", coalOut.energy, 12f);
                Near("Coal Power tradeoff: coal plant happiness -0.5", coalOut.happiness, -0.5f);

                Unlock(research, byId, "cityDev");
                Check("City Development: cap 3", research.CityLevelCap(2) == 3);
                ResourceVector city = new ResourceVector { energy = -8, money = -2, sustainability = -1, population = 5 };
                Near("City Development tradeoff: level-3 city sustainability -1 -> -2",
                    research.ModifyOutput(city, CityFacts(3)).sustainability, -2f);
                Near("...but not on a level-2 city", research.ModifyOutput(city, CityFacts(2)).sustainability, -1f);

                // "Any one of": Trade Market alone opens City Development 2.
                Unlock(research, byId, "trade");
                Check("City Development 2 open with only Trade Market", research.PrerequisitesMet(byId["cityDev2"]));
                Unlock(research, byId, "cityDev2");
                Near("City Development 2 crowding: level-5 city happiness -1",
                    research.ModifyOutput(city, CityFacts(5)).happiness, -1f);
                Unlock(research, byId, "urban", "hygiene");
                Near("Hygiene clears the crowding (Urbanization's +0.5 remains)",
                    research.ModifyOutput(city, CityFacts(5)).happiness, 0.5f);

                Near("Research: 1.5 per level", research.PointsPerTurn(cities), 9f);
                Near("Research tradeoff: -10 money a year", research.PerYear().money, -10f);
                Unlock(research, byId, "university");
                Near("University: +1 per level-3 city", research.PointsPerTurn(cities), 11f);

                Near("No export before Energy Exports", research.Export(30f).money, 0f);
                Unlock(research, byId, "exports"); // storage is already owned above
                ResourceVector sold = research.Export(30f);
                Near("Energy Exports: surplus leaves the stock", sold.energy, -30f);
                Near("Energy Exports: $1 per unit", sold.money, 30f);
                Near("Energy Exports tradeoff: -0.5 happiness", sold.happiness, -0.5f);
                Near("...nothing sold in a deficit year", research.Export(-4f).happiness, 0f);

                Unlock(research, byId, "biomass");
                Check("Biomass unlocks Biomassa", !research.IsTileLocked("Biomassa"));

                Unlock(research, byId, "industrial", "mechMining");
                TileDefinition mine = tiles["ExtracaoCarvao"];
                ResourceVector mineOut = mine.ApplyTerrainBonus(
                    research.ModifyOutput(mine.perTurnEffect, TileEconomy.Facts(mine)), TerrainRelief.Mountain);
                Near("Mechanized Mining on a mountain: (20 + 6) x 1.2", mineOut.money, 31.2f);
                Near("Industrial Revolution: build cost x0.9", research.BuildCostMultiplier(coal), 0.9f);
                Near("Industrial Revolution tradeoff: -1 sustainability a year", research.PerYear().sustainability, -1f);

                Unlock(research, byId, "massProd");
                Near("Mass Production: plant cost x0.9 x0.85", research.BuildCostMultiplier(coal), 0.765f);
                Near("Mass Production tradeoff: -5 sustainability per building",
                    research.OnBuildEffect(coal).sustainability, -5f);
                Near("...cities don't get the plant discount",
                    research.BuildCostMultiplier(CityFacts(1)), 0.9f);

                Check("every node has a position-safe prerequisite list", nodes.All(n => n.prerequisites.All(p => p != null)));
                Check("80+ nodes generated", nodes.Count >= 80);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }

            foreach (string failure in Failures) Debug.LogError($"[ResearchCheck] FAIL {failure}");
            Debug.Log($"[ResearchCheck] {_passed} passed, {Failures.Count} failed.");
            if (Application.isBatchMode) EditorApplication.Exit(Failures.Count == 0 ? 0 : 1);
        }

        private static TileFacts CityFacts(int level) => new TileFacts
        {
            name = "City", category = "City", tags = System.Array.Empty<string>(), cityLevel = level
        };

        private static void Unlock(ResearchManager research, Dictionary<string, ResearchNode> byId, params string[] ids)
        {
            foreach (string id in ids)
            {
                if (!byId.TryGetValue(id, out ResearchNode node)) { Failures.Add($"no node '{id}'"); continue; }
                research.AddPoints(node.cost);
                if (!research.TryUnlock(node)) Failures.Add($"could not unlock '{id}': {research.BlockReason(node)}");
            }
        }

        private static void Check(string label, bool ok)
        {
            if (ok) { _passed++; Debug.Log($"[ResearchCheck] ok   {label}"); }
            else Failures.Add(label);
        }

        private static void Near(string label, float actual, float expected) =>
            Check($"{label} (got {actual:0.###}, want {expected:0.###})", Mathf.Abs(actual - expected) < 0.001f);
    }
}
