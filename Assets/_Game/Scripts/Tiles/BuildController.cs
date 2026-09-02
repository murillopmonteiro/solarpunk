using System;
using System.Collections.Generic;
using Solarpunk.Grid;
using Solarpunk.Managers;
using UnityEngine;

namespace Solarpunk.Tiles
{
    /// <summary>
    /// Validates and executes construction. Driven by the build panel UI —
    /// it holds no input logic of its own.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private List<TileDefinition> palette = new();

        /// <summary>Fraction of the build cost returned when demolishing.</summary>
        [SerializeField, Range(0f, 1f)] private float refundRate = 0.5f;

        public IReadOnlyList<TileDefinition> Palette => palette;

        public event Action OnBoardChanged;

        private HexCell _cityCell;

        public bool CityAlreadyBuilt => _cityCell != null;

        /// <summary>Why a given tile can't go on a given hex — null when it can.</summary>
        public string BlockReason(HexCell cell, TileDefinition definition)
        {
            if (cell == null || definition == null) return "No tile selected";
            if (!cell.IsEmpty) return "Hex occupied";

            if (definition.category == TileCategory.City && CityAlreadyBuilt)
                return "City already founded";

            if (definition.requiredRelief != TerrainRelief.Mutable && definition.requiredRelief != cell.relief)
                return $"Needs {definition.requiredRelief}";

            if (!resourceManager.CanAfford(definition.buildCost))
                return "Not enough $";

            return null;
        }

        public bool TryBuild(HexCell cell, TileDefinition definition)
        {
            string blocked = BlockReason(cell, definition);
            if (blocked != null)
            {
                Debug.Log($"Can't build {definition?.displayName} on {cell?.coordinates}: {blocked}");
                return false;
            }

            if (!resourceManager.TrySpend(definition.buildCost)) return false;

            cell.builtTile = definition;
            cell.SetStructure(StructureFactory.Create(definition));

            if (definition.category == TileCategory.City)
            {
                cell.cityLevel = 1;
                _cityCell = cell;
            }

            OnBoardChanged?.Invoke();
            return true;
        }

        public bool Demolish(HexCell cell)
        {
            if (cell == null || cell.IsEmpty) return false;

            resourceManager.Refund(cell.builtTile.buildCost * refundRate);

            if (cell.builtTile.category == TileCategory.City) _cityCell = null;

            cell.builtTile = null;
            cell.cityLevel = 1;
            cell.ClearStructure();

            OnBoardChanged?.Invoke();
            return true;
        }
    }
}
