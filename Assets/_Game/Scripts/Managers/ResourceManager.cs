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
        public static ResourceManager Instance { get; private set; }

        [SerializeField] private float startingEnergy;
        [SerializeField] private float startingMoney = 500f;
        [SerializeField] private float startingSustainability = 100f;
        [SerializeField] private float startingPopulation = 10f;
        [SerializeField] private float startingHappiness = 100f;

        public ResourceVector Current { get; private set; }

        public event Action<ResourceVector> OnResourcesChanged;
        public event Action<bool> OnGameEnded; // true = victory, false = defeat

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            Current = new ResourceVector
            {
                energy = startingEnergy,
                money = startingMoney,
                sustainability = startingSustainability,
                population = startingPopulation,
                happiness = startingHappiness
            };
        }

        /// <summary>Applies one turn's worth of accumulated tile effects.</summary>
        public void ApplyTurn(ResourceVector turnDelta)
        {
            ResourceVector next = Current + turnDelta;

            // Blackout penalty: energy demand outpaced supply this turn.
            if (next.energy < 0f)
            {
                next.happiness -= Mathf.Abs(next.energy) * 0.5f;
                next.energy = 0f;
            }

            next.sustainability = Mathf.Clamp(next.sustainability, 0f, 100f);
            next.happiness = Mathf.Clamp(next.happiness, 0f, 100f);

            Current = next;
            OnResourcesChanged?.Invoke(Current);

            if (Current.sustainability <= 0f || Current.happiness <= 0f)
            {
                OnGameEnded?.Invoke(false);
            }
        }

        public void DeclareVictory() => OnGameEnded?.Invoke(true);
    }
}
