namespace Solarpunk.Core
{
    /// <summary>
    /// Rules where one stat's level bends another stat's yearly change (Murillo's
    /// list). Takes the stats at the start of the year and the year's delta from
    /// the tiles, and returns the delta with the rules applied.
    /// </summary>
    public static class IntertwinedStats
    {
        /// <summary>Share of population lost a year while in debt.</summary>
        public const float DebtPopulationLoss = 0.05f;

        public const float LowSustainability = 100f;
        public const float LowHappiness = 25f;
        public const float VeryLowHappiness = 10f;
        public const float UnhappinessPopulationPenalty = 10f;
        public const float EnergyDeficitHappinessPenalty = 5f;

        /// <summary>The energy deficit rule is off before this year.</summary>
        public const int EnergyDeficitFromYear = 50;

        public static ResourceVector Apply(ResourceVector current, ResourceVector delta, int year)
        {
            // In debt: population shrinks 5% a year.
            if (current.money < 0f) delta.population -= current.population * DebtPopulationLoss;

            // Sustainability under 100 pulls population growth down by half the shortfall.
            if (current.sustainability < LowSustainability)
                delta.population += (current.sustainability - LowSustainability) / 2f;

            // Unhappy: -10 growth, but this one cannot push growth below zero.
            if (current.happiness < LowHappiness && delta.population > 0f)
                delta.population = System.Math.Max(0f, delta.population - UnhappinessPopulationPenalty);

            // Very unhappy: another -10, with no floor.
            if (current.happiness < VeryLowHappiness) delta.population -= UnhappinessPopulationPenalty;

            // The grid runs a deficit: -5 happiness a year, from year 50.
            if (delta.energy < 0f && year >= EnergyDeficitFromYear)
                delta.happiness -= EnergyDeficitHappinessPenalty;

            return delta;
        }
    }
}
