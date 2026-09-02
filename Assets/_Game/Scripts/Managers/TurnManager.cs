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
        private const int VictoryTurn = 300;

        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private CityGrowth cityGrowth;

        public int CurrentTurn { get; private set; }
        public event Action<int> OnTurnAdvanced;

        public void AdvanceTurn()
        {
            ResourceVector turnDelta = ResourceVector.Zero;

            foreach (var cell in gridManager.Cells)
            {
                var hex = cell.Value;
                if (hex.builtTile == null) continue;

                if (hex.builtTile.category == TileCategory.City)
                {
                    turnDelta += cityGrowth.GetEffectForLevel(hex.cityLevel);
                    cityGrowth.TryAutoTick(hex);
                }
                else
                {
                    turnDelta += hex.builtTile.perTurnEffect;
                }
            }

            // TODO: roll a random event here and fold its effect into turnDelta.

            resourceManager.ApplyTurn(turnDelta);

            CurrentTurn++;
            OnTurnAdvanced?.Invoke(CurrentTurn);

            if (CurrentTurn >= VictoryTurn)
            {
                resourceManager.DeclareVictory();
            }
        }
    }
}
