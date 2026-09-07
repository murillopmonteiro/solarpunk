using Solarpunk.Core;
using Solarpunk.Grid;
using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// Data-only description of a buildable tile (city, power plant or extraction).
    /// One asset per tile type lives in Assets/_Game/Data/TileDefinitions.
    /// </summary>
    [CreateAssetMenu(menuName = "Solarpunk/Tile Definition", fileName = "NewTileDefinition")]
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

        [Tooltip("Colour of the placeholder structure, and of this tile's badge in the build menu.")]
        public Color placeholderColor = Color.white;

        [Tooltip("Real model for this tile. When empty, a primitive placeholder is built instead.")]
        public GameObject structurePrefab;

        /// <summary>Second line in the build menu, e.g. "Coast only  ·  $1200".</summary>
        public string ShortSummary()
        {
            string relief = requiredRelief == TerrainRelief.Mutable ? "Any terrain" : $"{requiredRelief} only";
            return $"{relief}  ·  ${buildCost:0}";
        }
    }
}
