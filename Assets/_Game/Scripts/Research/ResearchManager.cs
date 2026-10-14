using System;
using System.Collections.Generic;
using Solarpunk.Core;
using UnityEngine;

namespace Solarpunk.Research
{
    /// <summary>
    /// Holds research points, what they have bought, and what that changes.
    ///
    /// Research sits *outside* ResourceVector on purpose. The design doc has the
    /// city producing it per level, and the city's five-resource effect is flat
    /// by design — so paying research out of city level is what makes level buy
    /// something without touching G.
    ///
    /// Every gameplay consequence of an unlock is a <see cref="ResearchEffect"/>
    /// on the node, and this class is the one place that interprets them. The
    /// turn loop, the build rules and the city ask it through the query methods
    /// below rather than reading nodes themselves.
    /// </summary>
    public class ResearchManager : MonoBehaviour
    {
        [Tooltip("Every node in the tree. Order is irrelevant — layout comes from each node's position.")]
        [SerializeField] private List<ResearchNode> tree = new();

        [Tooltip("Points a city produces per year, per level, before research raises it. PLACEHOLDER.")]
        [SerializeField] private float pointsPerCityLevel = 1f;

        public IReadOnlyList<ResearchNode> Tree => tree;

        public float Points { get; private set; }

        /// <summary>What the board earned last turn, kept for the HUD's "/yr" readout.</summary>
        public float PerTurn { get; private set; }

        public event Action OnResearchChanged;

        private readonly HashSet<ResearchNode> _unlocked = new();
        private readonly HashSet<string> _unlockedNames = new();

        private void Awake() => UnlockFreeRoots();

        /// <summary>
        /// The hub costs nothing and needs nothing, so it is owned from year 0.
        /// Public for edit-mode callers, where Awake does not run.
        /// </summary>
        public void UnlockFreeRoots()
        {
            foreach (ResearchNode node in tree)
            {
                if (node != null && node.cost <= 0 && (node.prerequisites == null || node.prerequisites.Length == 0))
                    MarkUnlocked(node);
            }
        }

        public bool IsUnlocked(ResearchNode node) => node != null && _unlocked.Contains(node);

        public bool IsUnlocked(string nodeName) => _unlockedNames.Contains(nodeName);

        public int UnlockedCount => _unlocked.Count;

        public bool PrerequisitesMet(ResearchNode node)
        {
            if (node == null) return false;
            if (node.prerequisites == null || node.prerequisites.Length == 0) return true;

            bool any = false;
            bool all = true;
            foreach (ResearchNode prerequisite in node.prerequisites)
            {
                if (prerequisite == null) continue;
                if (_unlocked.Contains(prerequisite)) any = true;
                else all = false;
            }

            return node.requireAny ? any : all;
        }

        /// <summary>Why this node can't be unlocked — null when it can.</summary>
        public string BlockReason(ResearchNode node)
        {
            if (node == null) return Loc.T("block.noNode");
            if (_unlocked.Contains(node)) return Loc.T("block.researched");
            if (!PrerequisitesMet(node)) return Loc.T("block.needs", MissingPrerequisites(node));
            if (Points < node.cost) return Loc.T("block.research");

            return null;
        }

        public bool TryUnlock(ResearchNode node)
        {
            if (BlockReason(node) != null) return false;

            Points -= node.cost;
            MarkUnlocked(node);
            OnResearchChanged?.Invoke();
            return true;
        }

        private void MarkUnlocked(ResearchNode node)
        {
            _unlocked.Add(node);
            _unlockedNames.Add(node.name);
        }

        /// <summary>Test and harness hook: grants points without a year passing.</summary>
        public void AddPoints(float amount)
        {
            Points += amount;
            OnResearchChanged?.Invoke();
        }

        // ---------------------------------------------------------------- research income

        /// <summary>Points the board earns a year, given the level of every city on it.</summary>
        public float PointsPerTurn(IReadOnlyList<int> cityLevels)
        {
            float perLevel = pointsPerCityLevel;
            float flat = 0f;
            float scale = 1f;

            foreach (ResearchEffect effect in ActiveEffects())
            {
                switch (effect.kind)
                {
                    case EffectKind.ResearchPerLevel: perLevel += effect.add; break;
                    case EffectKind.ResearchMultiplier: scale *= effect.multiply; break;
                    case EffectKind.ResearchPerCity:
                        foreach (int level in cityLevels)
                        {
                            if (level >= effect.minCityLevel) flat += effect.add;
                        }
                        break;
                }
            }

            int totalLevels = 0;
            foreach (int level in cityLevels) totalLevels += level;

            return (totalLevels * perLevel + flat) * scale;
        }

        /// <summary>One year's research income. Called by the turn loop.</summary>
        public void ApplyTurn(IReadOnlyList<int> cityLevels)
        {
            PerTurn = PointsPerTurn(cityLevels);
            Points += PerTurn;
            OnResearchChanged?.Invoke();
        }

        // ---------------------------------------------------------------- simulation queries

        /// <summary>A tile's yearly output after research: every add first, then every multiplier.</summary>
        public ResourceVector ModifyOutput(ResourceVector output, TileFacts tile)
        {
            var multipliers = new List<ResearchEffect>();

            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind != EffectKind.TileOutput || !effect.Matches(tile)) continue;

                if (!Mathf.Approximately(effect.add, 0f))
                    output = output.With(effect.resource, output.Get(effect.resource) + effect.add);
                if (!Mathf.Approximately(effect.multiply, 1f)) multipliers.Add(effect);
            }

            foreach (ResearchEffect effect in multipliers)
                output = output.With(effect.resource, output.Get(effect.resource) * effect.multiply);

            return output;
        }

        public float BuildCostMultiplier(TileFacts tile) => Product(EffectKind.BuildCost, tile);

        public float ClearCostMultiplier(TileFacts tile) => Product(EffectKind.ClearCost, tile);

        /// <summary>One-off change charged when a tile is built, on top of its own founding bonus.</summary>
        public ResourceVector OnBuildEffect(TileFacts tile)
        {
            ResourceVector total = ResourceVector.Zero;
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == EffectKind.OnBuild && effect.Matches(tile))
                    total = total.With(effect.resource, total.Get(effect.resource) + effect.add);
            }
            return total;
        }

        /// <summary>Yearly changes that belong to no tile, e.g. the labs' running cost.</summary>
        public ResourceVector PerYear()
        {
            ResourceVector total = ResourceVector.Zero;
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == EffectKind.PerYear)
                    total = total.With(effect.resource, total.Get(effect.resource) + effect.add);
            }
            return total;
        }

        /// <summary>
        /// What exporting a year's energy surplus does: the surplus leaves the energy
        /// stock, money comes in at the export price, and the export tradeoffs land.
        /// Zero when no export research is owned or there is no surplus to sell.
        /// </summary>
        public ResourceVector Export(float surplus)
        {
            ResourceVector total = ResourceVector.Zero;
            if (surplus <= 0f) return total;

            bool exports = false;
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind != EffectKind.EnergyExport) continue;
                exports = true;
                float amount = effect.resource == ResourceType.Money ? surplus * effect.add : effect.add;
                total = total.With(effect.resource, total.Get(effect.resource) + amount);
            }

            if (exports) total.energy -= surplus;
            return total;
        }

        /// <summary>Extra fraction of the build cost a demolition refunds.</summary>
        public float RefundBonus()
        {
            float bonus = 0f;
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == EffectKind.DemolishRefund) bonus += effect.add;
            }
            return bonus;
        }

        /// <summary>The highest level a city may reach: the base, or what research has raised it to.</summary>
        public int CityLevelCap(int baseCap)
        {
            int cap = baseCap;
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == EffectKind.CityLevelCap) cap = Mathf.Max(cap, Mathf.RoundToInt(effect.add));
            }
            return cap;
        }

        public bool LiftsBuildLimit(string tileName)
        {
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == EffectKind.LiftBuildLimit && effect.key == tileName) return true;
            }
            return false;
        }

        /// <summary>
        /// Years between firings of a periodic rule (FellForest, SpreadForest) on this
        /// tile; 0 when no owned research gives it one.
        /// </summary>
        public int Period(EffectKind kind, TileFacts tile)
        {
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == kind && effect.Matches(tile)) return Mathf.Max(0, Mathf.RoundToInt(effect.add));
            }
            return 0;
        }

        /// <summary>Price of the first bought territory hex; 0 when buying land is not researched.</summary>
        public float TerritoryBasePrice()
        {
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == EffectKind.BuyTerritory) return effect.add;
            }
            return 0f;
        }

        /// <summary>
        /// The node that unlocks this tile, or null when the tile is free from the
        /// start. A tile no node mentions is never locked, so adding one to the
        /// palette cannot hide it by accident.
        /// </summary>
        public ResearchNode Unlocker(string tileName)
        {
            foreach (ResearchNode node in tree)
            {
                if (node == null) continue;
                foreach (ResearchEffect effect in node.effects)
                {
                    if (effect.kind == EffectKind.UnlockTile && effect.key == tileName) return node;
                }
            }
            return null;
        }

        public bool IsTileLocked(string tileName)
        {
            ResearchNode unlocker = Unlocker(tileName);
            return unlocker != null && !_unlocked.Contains(unlocker);
        }

        private float Product(EffectKind kind, TileFacts tile)
        {
            float product = 1f;
            foreach (ResearchEffect effect in ActiveEffects())
            {
                if (effect.kind == kind && effect.Matches(tile)) product *= effect.multiply;
            }
            return product;
        }

        /// <summary>Effects of every unlocked node, minus any that a later unlock switched off.</summary>
        private IEnumerable<ResearchEffect> ActiveEffects()
        {
            foreach (ResearchNode node in _unlocked)
            {
                if (node.effects == null) continue;

                foreach (ResearchEffect effect in node.effects)
                {
                    if (effect != null && !IsSuppressed(effect)) yield return effect;
                }
            }
        }

        private bool IsSuppressed(ResearchEffect effect)
        {
            if (effect.suppressedBy == null) return false;
            foreach (string nodeName in effect.suppressedBy)
            {
                if (_unlockedNames.Contains(nodeName)) return true;
            }
            return false;
        }

        private string MissingPrerequisites(ResearchNode node)
        {
            var names = new List<string>();
            foreach (ResearchNode prerequisite in node.prerequisites)
            {
                if (prerequisite != null && !_unlocked.Contains(prerequisite)) names.Add(prerequisite.LocalizedName);
            }

            return string.Join(node.requireAny ? Loc.T("block.or") : " + ", names);
        }
    }
}
