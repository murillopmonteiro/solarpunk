namespace Solarpunk.Core
{
    /// <summary>
    /// The end-of-run score (Murillo's formula):
    /// Money / Population * Sustainability / 1000 * Happiness / 100 * Energy.
    /// Shown on the end screen, which does not exist yet.
    /// </summary>
    public static class WinStats
    {
        /// <summary>Zero with no population, rather than dividing by it.</summary>
        public static float Score(ResourceVector stats)
        {
            if (stats.population <= 0f) return 0f;
            return stats.money / stats.population
                   * stats.sustainability / 1000f
                   * stats.happiness / 100f
                   * stats.energy;
        }
    }
}
