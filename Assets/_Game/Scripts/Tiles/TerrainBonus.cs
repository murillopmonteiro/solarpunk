using Solarpunk.Core;
using Solarpunk.Grid;
using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// What a tile gains from the ground it stands on — a coal mine cut into a
    /// mountain is cheaper to open and sells more (design doc §2 has the same
    /// shape for wind on mountains, which is not wired up yet).
    ///
    /// Only one resource is multiplied, deliberately: scaling the whole vector
    /// would scale a tile's costs along with its output, so a mine's mountain
    /// bonus would also cost 20% more sustainability.
    /// </summary>
    [System.Serializable]
    public class TerrainBonus
    {
        public TerrainRelief relief;

        [Tooltip("Which of the five resources the output multiplier applies to.")]
        public ResourceType resource = ResourceType.Money;

        [Tooltip("Multiplier on that resource's per-turn output. 1.2 = +20%.")]
        public float outputMultiplier = 1f;

        [Tooltip("Multiplier on the build cost. 0.8 = 20% cheaper.")]
        public float costMultiplier = 1f;
    }
}
