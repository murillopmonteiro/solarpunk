using System;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Tiles;
using UnityEngine;

namespace Solarpunk.Managers
{
    /// <summary>
    /// Drives the turn loop. One turn = one in-game year (design doc §"Resolvido
    /// nessa rodada"). Match ends in victory at turn 300 if the player hasn't
    /// already lost.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public const int VictoryTurn = 300;

        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private CityGrowth cityGrowth;

        public int CurrentTurn { get; private set; }
        public event Action<int> OnTurnAdvanced;

        /// <summary>Sum of every built tile's per-turn effect — also used for the HUD preview.</summary>
        public ResourceVector CalculateTurnDelta()
        {
            ResourceVector delta = ResourceVector.Zero;

            foreach (var pair in gridManager.Cells)
            {
                HexCell hex = pair.Value;
                if (hex.builtTile == null) continue;

                delta += hex.builtTile.category == TileCategory.City
                    ? cityGrowth.GetEffectForLevel(hex.cityLevel)
                    : hex.builtTile.perTurnEffect;
            }

            return delta;
        }

        public void AdvanceTurn()
        {
            if (resourceManager.GameOver) return;

            resourceManager.ApplyTurn(CalculateTurnDelta());

            // TODO: roll a random event here (design doc: one per turn).

            foreach (var pair in gridManager.Cells)
            {
                HexCell hex = pair.Value;
                if (hex.builtTile != null && hex.builtTile.category == TileCategory.City)
                {
                    cityGrowth.TryAutoTick(hex);
                }
            }

            CurrentTurn++;
            OnTurnAdvanced?.Invoke(CurrentTurn);

            if (CurrentTurn >= VictoryTurn) resourceManager.DeclareVictory();
        }
    }
}
