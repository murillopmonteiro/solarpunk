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
}
