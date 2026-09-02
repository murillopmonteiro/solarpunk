using Solarpunk.Grid;
using UnityEngine;

namespace Solarpunk.Managers
{
    /// <summary>Entry point: generates the board and listens for the end-of-game event.</summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;

        private void Start()
        {
            gridManager.GenerateGrid();
            resourceManager.OnGameEnded += HandleGameEnded;
        }

        private void HandleGameEnded(bool victory)
        {
            Debug.Log(victory ? "Victory! Survived to turn 300." : "Defeat: sustainability or happiness hit 0.");
            // TODO: hook up end-game UI.
        }

        private void OnDestroy()
        {
            if (resourceManager != null)
                resourceManager.OnGameEnded -= HandleGameEnded;
        }
    }
}
