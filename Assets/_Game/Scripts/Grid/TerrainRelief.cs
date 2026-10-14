using System.Collections.Generic;
using Solarpunk.Core;

namespace Solarpunk.Grid
{
    /// <summary>
    /// Fixed spatial constraint rolled once at match setup (design doc §2).
    /// Never regenerates during a match.
    /// </summary>
    public enum TerrainRelief
    {
        Mutable,
        Waterfall,
        Mountain,
        Coast
    }

    public static class TerrainReliefNames
    {
        /// <summary>Lower-case, for sentences like "Needs grass or mountain".</summary>
        public static string Describe(TerrainRelief relief)
        {
            return relief switch
            {
                TerrainRelief.Waterfall => Loc.T("relief.waterfall"),
                TerrainRelief.Mountain => Loc.T("relief.mountain"),
                TerrainRelief.Coast => Loc.T("relief.coast"),
                _ => Loc.T("relief.grass")
            };
        }

        /// <summary>"grass or mountain" — what a tile will accept. Empty means anywhere.</summary>
        public static string Describe(IReadOnlyList<TerrainRelief> reliefs)
        {
            if (reliefs == null || reliefs.Count == 0) return Loc.T("relief.any");

            var names = new List<string>(reliefs.Count);
            foreach (TerrainRelief relief in reliefs)
            {
                string name = Describe(relief);
                if (!names.Contains(name)) names.Add(name);
            }

            return names.Count == 1 ? names[0] : string.Join(Loc.T("relief.or"), names);
        }
    }
}
