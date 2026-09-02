using Solarpunk.Tiles;
using UnityEngine;

namespace Solarpunk.Grid
{
    /// <summary>
    /// A single hex on the board: its fixed coordinates/relief, and whatever
    /// tile is currently built on it (if any).
    /// </summary>
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        public TerrainRelief relief = TerrainRelief.Mutable;

        public TileDefinition builtTile;
        public int cityLevel = 1; // only meaningful when builtTile.category == City

        public bool CanBuild(TileDefinition definition)
        {
            if (builtTile != null) return false;
            if (definition.requiredRelief == TerrainRelief.Mutable) return true;
            return definition.requiredRelief == relief;
        }
    }
}
