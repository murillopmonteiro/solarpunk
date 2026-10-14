using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Research;
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
        /// <summary>The highest level the city model goes to, whatever research says.</summary>
        public const int MaxLevel = 10;

        [SerializeField] private ResourceManager resourceManager;

        [Tooltip("Raises the level cap as City Development and its successors are researched. " +
                 "Without it cities grow all the way to MaxLevel.")]
        [SerializeField] private ResearchManager researchManager;

        [Tooltip("Highest level a city reaches before any research. The tree raises it to 3, 5, 7, 10.")]
        [SerializeField] private int baseLevelCap = 2;

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
            if (cityCell.cityLevel >= LevelCap) return false;
            if (resourceManager.Current.happiness < happinessHealthyThreshold) return false;
            if (resourceManager.Current.energy < 0f) return false; // currently blacked out

            cityCell.cityLevel++;
            return true;
        }

        /// <summary>The highest level any city may reach right now.</summary>
        public int LevelCap => researchManager == null
            ? MaxLevel
            : Mathf.Min(MaxLevel, researchManager.CityLevelCap(baseLevelCap));

        public float ManualUpgradeCost(int currentLevel) => manualUpgradeCost.Evaluate(currentLevel);

        public bool TryManualUpgrade(HexCell cityCell)
        {
            if (cityCell.cityLevel >= LevelCap) return false;
            resourceManager.Spend(ManualUpgradeCost(cityCell.cityLevel));

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
