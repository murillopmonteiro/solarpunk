using Solarpunk.Core;
using Solarpunk.Managers;
using UnityEngine;

namespace Solarpunk.Debugging
{
    /// <summary>
    /// Stand-in for real UI: press Space to advance a turn, watch resource
    /// values print to the Console. Delete once a proper HUD exists.
    /// </summary>
    public class DebugControls : MonoBehaviour
    {
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private ResourceManager resourceManager;

        private void OnEnable()
        {
            resourceManager.OnResourcesChanged += LogResources;
            resourceManager.OnGameEnded += LogGameEnded;
        }

        private void OnDisable()
        {
            resourceManager.OnResourcesChanged -= LogResources;
            resourceManager.OnGameEnded -= LogGameEnded;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                turnManager.AdvanceTurn();
            }
        }

        private void LogResources(ResourceVector r)
        {
            Debug.Log($"Turn {turnManager.CurrentTurn} — Energy {r.energy:0.#} | Money {r.money:0.#} | " +
                      $"Sustainability {r.sustainability:0.#} | Population {r.population:0.#} | Happiness {r.happiness:0.#}");
        }

        private void LogGameEnded(bool victory)
        {
            Debug.Log(victory ? "VICTORY — survived to turn 300." : "DEFEAT — sustainability or happiness hit 0.");
        }
    }
}
