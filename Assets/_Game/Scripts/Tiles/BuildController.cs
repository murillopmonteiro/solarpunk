using System;
using System.Collections.Generic;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Research;
using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// Validates and executes construction. Driven by the build panel UI —
    /// it holds no input logic of its own.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private CityGrowth cityGrowth;

        [Tooltip("Locks tiles behind research and prices them after it. Optional: without it every tile is open.")]
        [SerializeField] private ResearchManager researchManager;

        [SerializeField] private List<TileDefinition> palette = new();

        /// <summary>Fraction of the build cost returned when demolishing, before research.</summary>
        [SerializeField, Range(0f, 1f)] private float refundRate = 0.5f;

        public IReadOnlyList<TileDefinition> Palette => palette;

        public ResearchManager Research => researchManager;

        /// <summary>Fraction of the build cost handed back on an ordinary demolition, research included.</summary>
        public float RefundRate =>
            Mathf.Clamp01(refundRate + (researchManager != null ? researchManager.RefundBonus() : 0f));

        public event Action OnBoardChanged;

        /// <summary>What this tile costs on this hex, terrain and research included.</summary>
        public float CostOn(TileDefinition definition, TerrainRelief relief) =>
            TileEconomy.BuildCost(definition, relief, researchManager);

        /// <summary>What this tile would yield on this hex, terrain and research included.</summary>
        public ResourceVector PreviewOn(TileDefinition definition, TerrainRelief relief) =>
            TileEconomy.Preview(definition, relief, researchManager);

        /// <summary>What the tile standing on this hex yields now.</summary>
        public ResourceVector OutputOf(HexCell cell) => TileEconomy.PerTurn(cell, cityGrowth, researchManager);

        public float ClearCostOf(TileDefinition definition) => TileEconomy.ClearCost(definition, researchManager);

        public float RefundFor(HexCell cell) => CostOn(cell.builtTile, cell.relief) * RefundRate;

        /// <summary>
        /// True when at least one tile on the board produces this fuel. Read off
        /// the board rather than kept as a counter, so it cannot drift out of
        /// sync with what is actually standing there.
        /// </summary>
        public bool HasFuelSupply(FuelType fuel)
        {
            if (fuel == FuelType.None) return true;

            foreach (var pair in gridManager.Cells)
            {
                TileDefinition built = pair.Value.builtTile;
                if (built != null && built.producedFuel == fuel) return true;
            }

            return false;
        }

        /// <summary>Why a given tile can't go on a given hex — null when it can.</summary>
        public string BlockReason(HexCell cell, TileDefinition definition)
        {
            if (cell == null || definition == null) return Loc.T("block.noTile");
            if (!cell.IsEmpty) return Loc.T("block.occupied");

            // Checked before terrain: "research Solar first" is the more useful answer
            // than "needs open ground" for a tile the player cannot have anywhere yet.
            if (researchManager != null && researchManager.IsTileLocked(definition.name))
                return Loc.T("block.needsResearch", researchManager.Unlocker(definition.name).LocalizedName);

            if (!definition.AllowedOn(cell.relief))
                return Loc.T("block.needs", TerrainReliefNames.Describe(definition.allowedRelief));

            // Design doc §3: extraction feeds the fossil and nuclear plants. Enforced
            // here at build time only — the doc also wants a running penalty when the
            // supply is later demolished, which needs the turn loop and isn't built yet.
            if (!HasFuelSupply(definition.requiredFuel))
                return Loc.T("block.needsExtraction", FuelTypes.Describe(definition.requiredFuel));

            // No money check: building past zero puts the treasury into debt.
            return null;
        }

        public bool TryBuild(HexCell cell, TileDefinition definition)
        {
            string blocked = BlockReason(cell, definition);
            if (blocked != null)
            {
                Debug.Log($"Can't build {definition?.displayName} on {cell?.coordinates}: {blocked}");
                return false;
            }

            resourceManager.Spend(CostOn(definition, cell.relief));

            cell.builtTile = definition;
            cell.SetStructure(StructureFactory.Create(definition));

            // Founding bonus, e.g. the city's one-off happiness lift, plus whatever
            // research charges per building (Mass Production's embodied carbon).
            ResourceVector oneOff = definition.onBuildBonus;
            if (researchManager != null) oneOff += researchManager.OnBuildEffect(TileEconomy.Facts(definition));
            resourceManager.ApplyInstant(oneOff);

            // Cities are not capped: found as many as the island has room for.
            // Each one runs its own level and its own flat per-turn effect.
            if (definition.category == TileCategory.City) cell.cityLevel = 1;

            OnBoardChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Why the tile on this hex cannot be cleared away — null when it can. Only
        /// tiles that charge to clear (forests) can be blocked; demolishing something
        /// the player paid for always pays back.
        /// </summary>
        public string DemolishBlockReason(HexCell cell)
        {
            return cell == null || cell.IsEmpty ? Loc.T("block.nothingToClear") : null;
        }

        public bool Demolish(HexCell cell)
        {
            if (DemolishBlockReason(cell) != null) return false;

            // A forest is cleared at a price; anything the player built refunds part of it.
            if (cell.builtTile.clearCost > 0f)
            {
                resourceManager.Spend(ClearCostOf(cell.builtTile));
            }
            else
            {
                // Refunds what building it here costs now, terrain and research included.
                resourceManager.Refund(RefundFor(cell));
            }

            cell.builtTile = null;
            cell.cityLevel = 1;
            cell.ClearStructure();

            OnBoardChanged?.Invoke();
            return true;
        }
    }
}
