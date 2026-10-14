using Solarpunk.Core;
using Solarpunk.Events;
using Solarpunk.Grid;
using Solarpunk.Research;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// What a tile actually yields and costs once research is counted. The turn
    /// loop, the build rules and the build panel all price through here, so a
    /// research effect cannot change the simulation in one place and be missing
    /// from what the player is shown in another.
    ///
    /// Order for output: base, then research adds and multipliers, then the
    /// terrain bonus — so a mountain still boosts whatever research added.
    /// A null ResearchManager means no research, which keeps old scenes working.
    /// </summary>
    public static class TileEconomy
    {
        /// <param name="cell">The hex it stands on, for neighbour rules. Null in previews.</param>
        public static TileFacts Facts(TileDefinition definition, int cityLevel = 0, HexCell cell = null) => new TileFacts
        {
            name = definition.name,
            category = definition.category.ToString(),
            tags = definition.tags,
            cityLevel = cityLevel,
            neighbours = cell?.NeighbourKeys()
        };

        /// <summary>The yearly effect of whatever stands on this hex.</summary>
        public static ResourceVector PerTurn(HexCell cell, CityGrowth cityGrowth, ResearchManager research)
        {
            TileDefinition built = cell.builtTile;
            if (built == null || !cell.IsMature) return ResourceVector.Zero;

            bool isCity = built.category == TileCategory.City;
            ResourceVector output = isCity ? cityGrowth.GetPerTurnEffect() : built.perTurnEffect;

            // ponytail: previews price with no neighbours, so a "next to a forest" bonus shows only once built.
            if (research != null)
                output = research.ModifyOutput(output, Facts(built, isCity ? cell.cityLevel : 0, cell));

            return isCity ? output : built.ApplyTerrainBonus(output, cell.relief);
        }

        /// <summary>What a tile would yield if built on this relief, for the build menu.</summary>
        public static ResourceVector Preview(TileDefinition definition, TerrainRelief relief, ResearchManager research)
        {
            ResourceVector output = definition.perTurnEffect;
            if (research != null) output = research.ModifyOutput(output, Facts(definition));
            return definition.ApplyTerrainBonus(output, relief);
        }

        public static float BuildCost(TileDefinition definition, TerrainRelief relief, ResearchManager research)
        {
            float cost = definition.BuildCostOn(relief);
            if (research != null) cost *= research.BuildCostMultiplier(Facts(definition));

            // Event discounts (the windmill spreading, a scientific treatise). No manager, no discount.
            RandomEventManager events = RandomEventManager.Instance;
            return events == null ? cost : cost * events.CostMultiplier(definition.name);
        }

        public static float ClearCost(TileDefinition definition, ResearchManager research)
        {
            float cost = definition.clearCost;
            return research == null ? cost : cost * research.ClearCostMultiplier(Facts(definition));
        }
    }
}
