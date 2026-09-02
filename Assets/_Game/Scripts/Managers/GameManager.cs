using Solarpunk.Grid;
using UnityEngine;

namespace Solarpunk.Managers
{
    /// <summary>
    /// Entry point. Generates the board in Awake so every other component sees
    /// a populated grid by the time its Start runs.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;

        private void Awake() => gridManager.GenerateGrid();

        private void Start() => resourceManager.OnGameEnded += HandleGameEnded;

        private void HandleGameEnded(bool victory)
        {
            Debug.Log(victory
                ? $"Victory — survived to turn {TurnManager.VictoryTurn}."
                : "Defeat — sustainability or happiness hit 0.");
        }

        private void OnDestroy()
        {
            if (resourceManager != null) resourceManager.OnGameEnded -= HandleGameEnded;
        }
    }
}
