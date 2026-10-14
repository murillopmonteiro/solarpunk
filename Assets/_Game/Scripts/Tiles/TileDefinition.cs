using System;
using System.Collections.Generic;
using Solarpunk.Core;
using Solarpunk.Grid;
using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// Data-only description of a buildable tile (city, power plant, extraction
    /// or terrain feature). One asset per tile type lives in
    /// Assets/_Game/Data/TileDefinitions.
    ///
    /// Anything that varies with the hex underneath goes through
    /// <see cref="BuildCostOn"/> and <see cref="EffectOn"/> rather than reading
    /// the raw fields, so terrain bonuses can never be applied in one place and
    /// forgotten in another.
    /// </summary>
    [CreateAssetMenu(menuName = "Solarpunk/Tile Definition", fileName = "NewTileDefinition")]
    public class TileDefinition : ScriptableObject
    {
        public string displayName;
        public TileCategory category;

        [Tooltip("Reliefs this tile may be built on. Empty = no restriction, build it anywhere.")]
        public TerrainRelief[] allowedRelief = Array.Empty<TerrainRelief>();

        [Tooltip("Money cost to construct this tile, before any terrain bonus.")]
        public float buildCost;

        [Tooltip("Money charged to clear this tile off its hex, instead of the usual demolition " +
                 "refund. 0 = refunds part of the build cost as normal.")]
        public float clearCost;

        [Tooltip("Whether this tile appears in the build menu. Off for terrain features the " +
                 "board generates itself, like forests.")]
        public bool playerBuildable = true;

        [Tooltip("Most of these the board may hold at once. 0 = no limit. Research can lift it " +
                 "(Oil Tracking lifts the one-well limit).")]
        public int maxOnBoard;

        [Tooltip("Years a freshly built one stands before it yields anything (a planted forest). " +
                 "0 = yields from its first year.")]
        public int maturesAfter;

        [Tooltip("Per-turn effect on the 5 global resources while this tile is active, before " +
                 "any terrain bonus.")]
        public ResourceVector perTurnEffect;

        [Tooltip("One-off effect applied the moment this tile is built, on top of its build cost.")]
        public ResourceVector onBuildBonus;

        [Tooltip("What this tile is worth more on. At most one entry per relief.")]
        public List<TerrainBonus> terrainBonuses = new();

        [Tooltip("Extraction tiles only: the fuel this tile supplies to the island.")]
        public FuelType producedFuel;

        [Tooltip("Power plants only: a tile producing this fuel must already stand on the board " +
                 "before this plant can be built. None = no prerequisite.")]
        public FuelType requiredFuel;

        [Tooltip("Groups research effects can target, e.g. \"fossil\" or \"renewable\".")]
        public string[] tags = Array.Empty<string>();

        [Tooltip("Colour of the placeholder structure, and of this tile's badge in the build menu.")]
        public Color placeholderColor = Color.white;

        [Tooltip("Real model for this tile. When empty, a primitive placeholder is built instead.")]
        public GameObject structurePrefab;

        /// <summary>Relief rule from design doc §2 — hydro needs a waterfall, a mine needs ground worth digging.</summary>
        public bool AllowedOn(TerrainRelief relief) =>
            allowedRelief == null || allowedRelief.Length == 0 || Array.IndexOf(allowedRelief, relief) >= 0;

        /// <summary>The bonus this relief grants, or null when it grants none.</summary>
        public TerrainBonus BonusOn(TerrainRelief relief) =>
            terrainBonuses?.Find(bonus => bonus != null && bonus.relief == relief);

        /// <summary>What this tile actually costs on a given hex.</summary>
        public float BuildCostOn(TerrainRelief relief)
        {
            TerrainBonus bonus = BonusOn(relief);
            return bonus == null ? buildCost : buildCost * bonus.costMultiplier;
        }

        /// <summary>What this tile actually produces on a given hex.</summary>
        public ResourceVector EffectOn(TerrainRelief relief) => ApplyTerrainBonus(perTurnEffect, relief);

        /// <summary>
        /// Applies this hex's bonus to an output that may already carry research
        /// changes, so a researched +6 on a mountain mine is boosted like the base +20.
        /// </summary>
        public ResourceVector ApplyTerrainBonus(ResourceVector output, TerrainRelief relief)
        {
            TerrainBonus bonus = BonusOn(relief);
            if (bonus == null) return output;

            return output.With(bonus.resource, output.Get(bonus.resource) * bonus.outputMultiplier);
        }

        /// <summary>
        /// The name the player sees, in the current language. <see cref="displayName"/>
        /// stays the stable identifier code and the harness match on.
        /// </summary>
        public string LocalizedName => Loc.Asset($"tile.{name}", displayName);

        /// <summary>Second line in the build menu, e.g. "grass or mountain  ·  $500".</summary>
        public string ShortSummary() =>
            $"{TerrainReliefNames.Describe(allowedRelief)}  ·  ${buildCost:0}";
    }
}
