using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// Temporary input layer standing in for a real build UI: number keys
    /// 1-9 and 0 select a tile from <see cref="palette"/>, left-click a hex
    /// to place it. Only one City tile is allowed per match (it grows via
    /// <see cref="CityGrowth"/> instead of being rebuilt).
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private TileDefinition[] palette;

        private static readonly KeyCode[] SelectKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0
        };

        private int _selectedIndex;
        private HexCell _cityCell;

        private void Update()
        {
            for (int i = 0; i < SelectKeys.Length && i < palette.Length; i++)
            {
                if (Input.GetKeyDown(SelectKeys[i]))
                {
                    _selectedIndex = i;
                    Debug.Log($"Selected tile: {palette[_selectedIndex].displayName}");
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryBuildAtMouse();
            }
        }

        private void TryBuildAtMouse()
        {
            if (palette.Length == 0) return;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            var cell = hit.collider.GetComponent<HexCell>();
            if (cell == null) return;

            TileDefinition definition = palette[_selectedIndex];
            TryBuild(cell, definition);
        }

        private void TryBuild(HexCell cell, TileDefinition definition)
        {
            if (definition.category == TileCategory.City && _cityCell != null)
            {
                Debug.Log("City already built — it grows on its own, it can't be placed again.");
                return;
            }

            if (!cell.CanBuild(definition))
            {
                Debug.Log($"Can't build {definition.displayName} on {cell.coordinates}: occupied or wrong relief.");
                return;
            }

            if (resourceManager.Current.money < definition.buildCost)
            {
                Debug.Log($"Not enough money for {definition.displayName} (needs {definition.buildCost}).");
                return;
            }

            resourceManager.ApplyTurn(new ResourceVector { money = -definition.buildCost });
            cell.builtTile = definition;

            if (definition.category == TileCategory.City)
            {
                cell.cityLevel = 1;
                _cityCell = cell;
            }

            Debug.Log($"Built {definition.displayName} on {cell.coordinates}.");
        }
    }
}
