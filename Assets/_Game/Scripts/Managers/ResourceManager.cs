using System;
using Solarpunk.Core;
using UnityEngine;

namespace Solarpunk.Managers
{
    /// <summary>
    /// Tracks the 5 global resource stats and evaluates the win/loss
    /// conditions from the design doc (§5).
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        /// <summary>
        /// Sustainability runs on a 0-1000 budget, not 0-100. The balance triads are
        /// written as whole numbers per year (a coal setup drains 5), and 1000 is what
        /// makes that arithmetic land: 1000 / 5 = collapse at year 200.
        /// </summary>
        public const float SustainabilityMax = 1000f;

        private const float HappinessMax = 100f;

        [SerializeField] private float startingEnergy = 20f;
        [SerializeField] private float startingMoney = 2000f;
        [SerializeField] private float startingSustainability = SustainabilityMax;
        [SerializeField] private float startingPopulation = 10f;
        [SerializeField] private float startingHappiness = HappinessMax;

        public ResourceVector Current { get; private set; }
        public bool GameOver { get; private set; }

        /// <summary>Admin mode: sustainability or happiness at zero no longer ends the run.</summary>
        public bool Invincible { get; set; }

        public event Action<ResourceVector> OnResourcesChanged;
        public event Action<bool> OnGameEnded; // true = victory, false = defeat

        private void Awake()
        {
            Current = new ResourceVector
            {
                energy = startingEnergy,
                money = startingMoney,
                sustainability = startingSustainability,
                population = startingPopulation,
                happiness = startingHappiness
            };
        }

        private void Start() => OnResourcesChanged?.Invoke(Current);

        /// <summary>
        /// Immediate spend (construction). Does not run turn resolution. Never refused:
        /// money may go negative, and a negative balance is debt, with no limit or interest.
        /// </summary>
        public void Spend(float cost)
        {
            ResourceVector next = Current;
            next.money -= cost;
            Current = next;
            OnResourcesChanged?.Invoke(Current);
        }

        /// <summary>Immediate refund (demolition).</summary>
        public void Refund(float amount)
        {
            ResourceVector next = Current;
            next.money += amount;
            Current = next;
            OnResourcesChanged?.Invoke(Current);
        }

        /// <summary>
        /// One-off adjustment such as a tile's on-build bonus. Clamps like a turn
        /// does, but skips the blackout penalty and the loss check, since nothing
        /// here represents a year passing.
        /// </summary>
        public void ApplyInstant(ResourceVector delta)
        {
            ResourceVector next = Current + delta;
            next.sustainability = Mathf.Clamp(next.sustainability, 0f, SustainabilityMax);
            next.happiness = Mathf.Clamp(next.happiness, 0f, HappinessMax);
            next.population = Mathf.Max(0f, next.population);
            next.energy = Mathf.Max(0f, next.energy);

            Current = next;
            OnResourcesChanged?.Invoke(Current);
        }

        /// <summary>
        /// Admin mode: puts one stat at an exact number. Held to the same ranges a
        /// year enforces (sustainability 0-1000, happiness 0-100, population and
        /// energy not below 0; money is free, debt included), or the next turn would
        /// snap it back. No defeat check: nothing here is a year passing.
        /// </summary>
        public void SetStat(ResourceType type, float value)
        {
            value = type switch
            {
                ResourceType.Sustainability => Mathf.Clamp(value, 0f, SustainabilityMax),
                ResourceType.Happiness => Mathf.Clamp(value, 0f, HappinessMax),
                ResourceType.Money => value,
                _ => Mathf.Max(0f, value)
            };

            Current = Current.With(type, value);
            OnResourcesChanged?.Invoke(Current);
        }

        /// <summary>Applies one turn's worth of accumulated tile effects.</summary>
        public void ApplyTurn(ResourceVector turnDelta)
        {
            if (GameOver) return;

            ResourceVector next = Current + turnDelta;

            // Blackout penalty: demand outpaced supply and the buffer ran dry.
            if (next.energy < 0f)
            {
                next.happiness -= Mathf.Abs(next.energy) * 0.5f;
                next.energy = 0f;
            }

            next.sustainability = Mathf.Clamp(next.sustainability, 0f, SustainabilityMax);
            next.happiness = Mathf.Clamp(next.happiness, 0f, HappinessMax);
            next.population = Mathf.Max(0f, next.population);

            Current = next;
            OnResourcesChanged?.Invoke(Current);

            if (!Invincible && (Current.sustainability <= 0f || Current.happiness <= 0f))
            {
                GameOver = true;
                OnGameEnded?.Invoke(false);
            }
        }

        public void DeclareVictory()
        {
            if (GameOver) return;
            GameOver = true;
            OnGameEnded?.Invoke(true);
        }
    }
}
