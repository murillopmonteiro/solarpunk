using Solarpunk.Core;
using Solarpunk.Grid;
using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// Data-only description of a buildable tile (city, power plant or extraction).
    /// Create one asset per tile type from Assets/Create/Solar Coat/Tile Definition.
    /// The 8 power plant profiles from the design doc (§3) are authored as separate
    /// assets in Assets/_Game/Data/TileDefinitions.
    /// </summary>
    [CreateAssetMenu(menuName = "Solar Coat/Tile Definition", fileName = "NewTileDefinition")]
    public class TileDefinition : ScriptableObject
    {
        public string displayName;
        public TileCategory category;

        [Tooltip("Relief this tile is allowed to be built on. Mutable = no restriction.")]
        public TerrainRelief requiredRelief = TerrainRelief.Mutable;

        [Tooltip("Money cost to construct this tile.")]
        public float buildCost;

        [Tooltip("Per-turn effect on the 5 global resources while this tile is active.")]
        public ResourceVector perTurnEffect;

        [Tooltip("Power plants only: needs an active Extraction tile to operate without penalty.")]
        public bool requiresExtraction;
    }
}
