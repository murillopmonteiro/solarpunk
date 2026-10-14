using System;

namespace Solarpunk.Core
{
    /// <summary>
    /// Per-turn effect on the 5 global resources. Every tile (city, power plant,
    /// extraction) is defined by one of these, as described in the design doc.
    /// </summary>
    [Serializable]
    public struct ResourceVector
    {
        public float energy;
        public float money;
        public float sustainability;
        public float population;
        public float happiness;

        public static ResourceVector Zero => new ResourceVector();

        public static ResourceVector operator +(ResourceVector a, ResourceVector b)
        {
            return new ResourceVector
            {
                energy = a.energy + b.energy,
                money = a.money + b.money,
                sustainability = a.sustainability + b.sustainability,
                population = a.population + b.population,
                happiness = a.happiness + b.happiness
            };
        }

        public static ResourceVector operator *(ResourceVector a, float scale)
        {
            return new ResourceVector
            {
                energy = a.energy * scale,
                money = a.money * scale,
                sustainability = a.sustainability * scale,
                population = a.population * scale,
                happiness = a.happiness * scale
            };
        }

        /// <summary>A copy with one component replaced. The struct itself stays immutable in use.</summary>
        public ResourceVector With(ResourceType type, float value)
        {
            ResourceVector copy = this;
            switch (type)
            {
                case ResourceType.Energy: copy.energy = value; break;
                case ResourceType.Money: copy.money = value; break;
                case ResourceType.Sustainability: copy.sustainability = value; break;
                case ResourceType.Population: copy.population = value; break;
                case ResourceType.Happiness: copy.happiness = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            return copy;
        }

        public float Get(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Energy: return energy;
                case ResourceType.Money: return money;
                case ResourceType.Sustainability: return sustainability;
                case ResourceType.Population: return population;
                case ResourceType.Happiness: return happiness;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
