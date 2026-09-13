using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// City progression (design doc §3, "T-Cidade"): hybrid growth — an automatic
    /// tick each turn while happiness/energy are healthy, plus a manual paid
    /// upgrade that skips one level instantly. Exact scaling curves aren't
    /// nailed down in the design doc yet; tune the two AnimationCurves in the
    /// inspector as balancing progresses.
    /// </summary>
    public class CityGrowth : MonoBehaviour
    {
        public const int MaxLevel = 10;

        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private float happinessHealthyThreshold = 30f;

        [Tooltip("Money cost of a manual upgrade, indexed by current level (1-10).")]
        [SerializeField] private AnimationCurve manualUpgradeCost = AnimationCurve.Linear(1, 200, 10, 2000);

        // FormerlySerializedAs carries the value across the rename, so scenes saved
        // before this keep their numbers instead of silently deserialising to zero.
        [Tooltip("The city's per-turn G = [E, M, S, P, H]. Flat: it does not scale with level.")]
        [FormerlySerializedAs("perLevelEffect")]
        [SerializeField] private ResourceVector cityEffect;

        public bool TryAutoTick(HexCell cityCell)
        {
            if (cityCell.cityLevel >= MaxLevel) return false;
            if (resourceManager.Current.happiness < happinessHealthyThreshold) return false;
            if (resourceManager.Current.energy < 0f) return false; // currently blacked out

            cityCell.cityLevel++;
            return true;
        }

        public float ManualUpgradeCost(int currentLevel) => manualUpgradeCost.Evaluate(currentLevel);

        public bool TryManualUpgrade(HexCell cityCell)
        {
            if (cityCell.cityLevel >= MaxLevel) return false;
            if (!resourceManager.TrySpend(ManualUpgradeCost(cityCell.cityLevel))) return false;

            cityCell.cityLevel++;
            return true;
        }

        /// <summary>
        /// Constant by design. This used to return cityEffect * level, which made a
        /// level 5 city drain five times a level 1 one and ended runs on happiness
        /// around year 9, long before sustainability mattered.
        /// </summary>
        public ResourceVector GetPerTurnEffect() => cityEffect;
    }
}
