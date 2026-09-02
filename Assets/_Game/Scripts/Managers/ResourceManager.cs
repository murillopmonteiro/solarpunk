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
        [SerializeField] private float startingEnergy = 20f;
        [SerializeField] private float startingMoney = 2000f;
        [SerializeField] private float startingSustainability = 100f;
        [SerializeField] private float startingPopulation = 10f;
        [SerializeField] private float startingHappiness = 100f;

        public ResourceVector Current { get; private set; }
        public bool GameOver { get; private set; }

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

        public bool CanAfford(float cost) => Current.money >= cost;

        /// <summary>Immediate spend (construction). Does not run turn resolution.</summary>
        public bool TrySpend(float cost)
        {
            if (!CanAfford(cost)) return false;

            ResourceVector next = Current;
            next.money -= cost;
            Current = next;
            OnResourcesChanged?.Invoke(Current);
            return true;
        }

        /// <summary>Immediate refund (demolition).</summary>
        public void Refund(float amount)
        {
            ResourceVector next = Current;
            next.money += amount;
            Current = next;
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

            next.sustainability = Mathf.Clamp(next.sustainability, 0f, 100f);
            next.happiness = Mathf.Clamp(next.happiness, 0f, 100f);
            next.population = Mathf.Max(0f, next.population);

            Current = next;
            OnResourcesChanged?.Invoke(Current);

            if (Current.sustainability <= 0f || Current.happiness <= 0f)
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
