namespace Solarpunk.Tiles
{
    public enum TileCategory
    {
        City,
        PowerPlant,
        Extraction,

        /// <summary>
        /// Terrain feature the board starts with rather than something the player
        /// builds. Occupies its hex until cleared, and clearing costs money.
        /// </summary>
        Forest
    }
}
