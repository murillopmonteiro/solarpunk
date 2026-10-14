using System;
using System.Collections.Generic;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Events;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Generates the yearly event cards, Murillo's deck: eight from Era 1 (agricultural
    /// and pre-industrial) and ten general ones. Text lives in <see cref="Loc"/> under
    /// event.&lt;name&gt;; this table holds only the numbers and the draw rules.
    ///
    /// Effects are G = [E, M, S, P, H] deltas, applied once when the player answers.
    /// </summary>
    public static class EventDeckFactory
    {
        private const string OutputDir = "Assets/_Game/Data/Events";

        /// <summary>Every power plant, for the treatise that discounts one at random.</summary>
        private const string AllPlants = "Carvao|Petroleo|Nuclear|Eolica|Solar|Hidreletrica|Maremotriz|Biomassa";

        private static ResourceVector G(float e = 0, float m = 0, float s = 0, float p = 0, float h = 0) =>
            new ResourceVector { energy = e, money = m, sustainability = s, population = p, happiness = h };

        private static EventDefinition.Option Opt(string key, ResourceVector effect, int territory = 0) =>
            new EventDefinition.Option { labelKey = key, effect = effect, territory = territory };

        private static EventDefinition.Option Ok(ResourceVector effect, int territory = 0) =>
            Opt("event.ok", effect, territory);

        private static EventDefinition.Option Decline => Opt("event.decline", G());

        /// <summary>Asset name, English title, and the rest set on the asset.</summary>
        private static readonly (string name, string title, Action<EventDefinition> set)[] Plan =
        {
            // Era 1: agricultural revolution and pre-industrial
            ("Moinho", "The Windmill Spreads", c =>
            {
                c.options.Add(Ok(G(e: 10)));
                c.discountTiles = "Eolica"; c.discount = 0.2f; c.discountYears = 0;
                c.once = true; // a permanent discount must not stack
            }),
            ("RodaDagua", "Improved Water Wheel", c =>
            {
                c.options.Add(Ok(G(e: 12, m: 5)));
                c.requiresTile = "Hidreletrica";
            }),
            ("RotacaoCulturas", "Crop Rotation", c => c.options.Add(Ok(G(p: 8, s: 5)))),
            ("CanalEstrada", "Canal and Road", c => c.options.Add(Ok(G(m: 15, h: 3)))),
            ("FeiraCidade", "Town Fair", c =>
            {
                c.options.Add(Ok(G(m: 10, h: 5)));
                c.requiresTile = "City";
            }),
            ("Hortas", "Community Gardens and Orchards", c =>
            {
                c.options.Add(Opt("event.accept", G(m: -10, s: 8, h: 6, p: 3)));
                c.options.Add(Decline);
            }),
            ("Textil", "Textile Mill", c =>
            {
                c.options.Add(Opt("event.accept", G(m: 25, p: 5, h: -8, s: -4)));
                c.options.Add(Decline);
            }),
            ("Bloqueio", "Trade Blockade", c =>
            {
                c.options.Add(Opt("event.Bloqueio.import", G(m: -20, e: 10)));
                c.options.Add(Opt("event.Bloqueio.local", G(e: 3, s: 3, h: 2)));
            }),

            // General
            ("Guerra", "War Declared", c =>
            {
                c.options.Add(Ok(G(p: -10, m: -30)));
                c.startsWarYears = 10;
            }),
            ("Desastre", "Natural Disaster", c =>
            {
                c.options.Add(Ok(G(p: -30, m: -100)));
                c.resilient = true;
                c.variants = 3;
            }),
            ("OfertaTerritorio", "Territory Offer", c =>
            {
                c.options.Add(Opt("event.OfertaTerritorio.buy", G(m: -200), territory: 1));
                c.options.Add(Decline);
                c.minMoney = 200f;
            }),
            ("TerritorioConquistado", "Territory Conquered", c =>
            {
                c.options.Add(Ok(G(h: 3, s: -3), territory: 1));
                c.warOnly = true;
            }),
            ("BaixasCivis", "Civilian Casualties", c =>
            {
                c.options.Add(Ok(G(p: -15, h: -8)));
                c.warOnly = true;
            }),
            ("Peste", "Crop Blight", c =>
            {
                c.options.Add(Ok(G(p: -8, s: -4, m: -10)));
                c.halvedAfter = "RotacaoCulturas";
            }),
            ("Insatisfacao", "Popular Unrest", c =>
            {
                c.options.Add(Opt("event.Insatisfacao.endure", G(h: -12)));
                c.options.Add(Opt("event.Insatisfacao.pay", G(m: -30, h: -4)));
                c.lowHappinessWeight = 3f;
            }),
            ("Descoberta", "Scientific Discovery", c =>
            {
                c.options.Add(Ok(G(e: 8, s: 5)));
                c.discountTiles = AllPlants; c.discount = 0.1f; c.discountYears = 10;
            }),
            ("BabyBoom", "Baby Boom", c => c.options.Add(Ok(G(p: 15, h: 4, s: -3)))),
            ("Ouro", "Gold Reserves", c => c.options.Add(Ok(G(m: 100, s: -3)))),
        };

        public static string[] OrderedNames => Plan.Select(p => p.name).ToArray();

        /// <summary>Loads the deck fresh off disk, in plan order.</summary>
        public static List<EventDefinition> LoadAll()
        {
            var loaded = new List<EventDefinition>();

            foreach (string name in OrderedNames)
            {
                var asset = AssetDatabase.LoadAssetAtPath<EventDefinition>($"{OutputDir}/{name}.asset");
                if (asset == null) Debug.LogError($"Event '{name}' failed to load from {OutputDir}.");
                else loaded.Add(asset);
            }

            return loaded;
        }

        [MenuItem("Solarpunk/Generate Event Deck")]
        public static void GenerateAll()
        {
            System.IO.Directory.CreateDirectory(OutputDir);

            foreach (var (name, title, set) in Plan)
            {
                var asset = ScriptableObject.CreateInstance<EventDefinition>();
                asset.displayName = title;
                set(asset);

                string path = $"{OutputDir}/{name}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(asset, path);
            }

            PruneStrayEvents();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Generated {Plan.Length} events in {OutputDir}.");
        }

        private static void PruneStrayEvents()
        {
            var keep = new HashSet<string>(OrderedNames);

            foreach (string guid in AssetDatabase.FindAssets("t:EventDefinition", new[] { OutputDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (keep.Contains(name)) continue;

                Debug.Log($"Removing stale event '{name}'.");
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
