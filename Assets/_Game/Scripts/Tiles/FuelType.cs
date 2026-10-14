using Solarpunk.Core;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// What an extraction tile pulls out of the ground, and what a fossil or
    /// nuclear plant needs a supply of before it can be built (design doc
    /// §3 "T-Extração": extraction feeds the fossil/nuclear plants).
    ///
    /// Adding a fuel is a two-step job: a value here, and an extraction tile in
    /// TileDataFactory that produces it. No logic changes anywhere else —
    /// BuildController matches producer to consumer by this enum alone.
    ///
    /// Values are explicit because these are serialised into the tile assets:
    /// reordering the list without them would silently repoint every tile at a
    /// different fuel.
    /// </summary>
    public enum FuelType
    {
        None = 0,
        Coal = 1,
        Oil = 2,
        Uranium = 3
    }

    public static class FuelTypes
    {
        /// <summary>Lower-case, for sentences like "Needs coal extraction".</summary>
        public static string Describe(FuelType fuel)
        {
            return fuel switch
            {
                FuelType.Coal => Loc.T("fuel.coal"),
                FuelType.Oil => Loc.T("fuel.oil"),
                FuelType.Uranium => Loc.T("fuel.uranium"),
                _ => Loc.T("fuel.none")
            };
        }
    }
}
