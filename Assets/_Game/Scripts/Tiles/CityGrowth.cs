using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using UnityEngine;

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

        [Tooltip("Per-level per-turn resource effect, indexed by city level (1-10).")]
        [SerializeField] private ResourceVector perLevelEffect;

        public bool TryAutoTick(HexCell cityCell)
        {
            if (cityCell.cityLevel >= MaxLevel) return false;
            if (resourceManager.Current.happiness < happinessHealthyThreshold) return false;
            if (resourceManager.Current.energy < 0f) return false; // currently blacked out

            cityCell.cityLevel++;
            return true;
        }

        public bool TryManualUpgrade(HexCell cityCell)
        {
            if (cityCell.cityLevel >= MaxLevel) return false;

            float cost = manualUpgradeCost.Evaluate(cityCell.cityLevel);
            if (resourceManager.Current.money < cost) return false;

            resourceManager.ApplyTurn(new ResourceVector { money = -cost });
            cityCell.cityLevel++;
            return true;
        }

        public ResourceVector GetEffectForLevel(int level) => perLevelEffect * level;
    }
}
