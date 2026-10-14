using System;
using Solarpunk.Core;

namespace Solarpunk.Research
{
    /// <summary>What a research effect changes.</summary>
    public enum EffectKind
    {
        /// <summary>A matched tile's yearly output: <c>add</c> to one resource, then <c>multiply</c> it.</summary>
        TileOutput,

        /// <summary>A matched tile's build cost, scaled by <c>multiply</c>.</summary>
        BuildCost,

        /// <summary>A one-off change applied each time a matched tile is built.</summary>
        OnBuild,

        /// <summary>A flat yearly change to one resource, not tied to any tile.</summary>
        PerYear,

        /// <summary>Research earned per city level, raised by <c>add</c>.</summary>
        ResearchPerLevel,

        /// <summary>Every city at <c>minCityLevel</c> or above earns <c>add</c> research a year.</summary>
        ResearchPerCity,

        /// <summary>The year's whole research income, scaled by <c>multiply</c>.</summary>
        ResearchMultiplier,

        /// <summary>Raises the highest level a city may grow to, to <c>add</c>.</summary>
        CityLevelCap,

        /// <summary>Makes the tile named by <c>key</c> buildable. Until then it is locked.</summary>
        UnlockTile,

        /// <summary>Adds <c>add</c> to the fraction of the build cost a demolition refunds.</summary>
        DemolishRefund,

        /// <summary>A matched tile's clearing fee (forests), scaled by <c>multiply</c>.</summary>
        ClearCost,

        /// <summary>
        /// The year's energy surplus is sold instead of stored. On Money, <c>add</c> is the
        /// price per unit sold; on any other resource, <c>add</c> lands once in a year that exports.
        /// </summary>
        EnergyExport
    }

    /// <summary>Which tiles an effect applies to.</summary>
    public enum EffectTarget
    {
        All,

        /// <summary><c>key</c> is a tile asset name, e.g. "Carvao".</summary>
        Tile,

        /// <summary><c>key</c> is a TileCategory name, e.g. "City" or "PowerPlant".</summary>
        Category,

        /// <summary><c>key</c> is a tag from TileDefinition.tags, e.g. "fossil".</summary>
        Tag
    }

    /// <summary>
    /// One thing an unlocked research node does. Data, like the node itself: the
    /// tree factory writes these, and <see cref="ResearchManager"/> is the only
    /// code that reads them, so a new effect of an existing kind is an asset edit.
    ///
    /// A tradeoff is an effect like any other, flagged so the panel can show it
    /// apart from the benefit.
    /// </summary>
    [Serializable]
    public class ResearchEffect
    {
        public EffectKind kind;
        public EffectTarget target;

        [UnityEngine.Tooltip("Tile asset name, category or tag, depending on target. For UnlockTile, the tile.")]
        public string key;

        public ResourceType resource;
        public float add;
        public float multiply = 1f;

        [UnityEngine.Tooltip("City effects only: applies to cities at this level or above. 0 = every city.")]
        public int minCityLevel;

        [UnityEngine.Tooltip("True when this effect is the node's cost rather than its benefit.")]
        public bool isTradeoff;

        [UnityEngine.Tooltip("Node asset names that switch this effect off once any of them is unlocked.")]
        public string[] suppressedBy = Array.Empty<string>();

        public bool Matches(TileFacts tile)
        {
            if (minCityLevel > 0 && (tile.category != "City" || tile.cityLevel < minCityLevel)) return false;

            return target switch
            {
                EffectTarget.All => true,
                EffectTarget.Tile => tile.name == key,
                EffectTarget.Category => tile.category == key,
                EffectTarget.Tag => tile.tags != null && Array.IndexOf(tile.tags, key) >= 0,
                _ => false
            };
        }
    }

    /// <summary>
    /// What research needs to know about a tile to decide whether an effect
    /// applies to it. Plain data, so Research does not depend on Tiles.
    /// </summary>
    public struct TileFacts
    {
        public string name;
        public string category;
        public string[] tags;
        public int cityLevel;
    }
}
