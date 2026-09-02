using System.Collections.Generic;
using UnityEngine;

namespace Solarpunk.Grid
{
    /// <summary>
    /// Builds the hex board at match start. Territory is fixed for the whole
    /// match once generated (design doc: 10u², grows only via events or paid
    /// expansion — not implemented yet).
    /// </summary>
    public class HexGridManager : MonoBehaviour
    {
        [SerializeField] private HexCell hexCellPrefab;
        [SerializeField] private int gridRadius = 5;
        [SerializeField] private float hexSize = 1f;

        [Range(0f, 1f)] [SerializeField] private float waterfallChance = 0.03f;
        [Range(0f, 1f)] [SerializeField] private float mountainChance = 0.12f;
        [Range(0f, 1f)] [SerializeField] private float coastChance = 0.1f;

        private readonly Dictionary<HexCoordinates, HexCell> _cells = new();

        public IReadOnlyDictionary<HexCoordinates, HexCell> Cells => _cells;

        public void GenerateGrid()
        {
            foreach (var cell in _cells.Values)
            {
                if (cell != null) Destroy(cell.gameObject);
            }
            _cells.Clear();

            for (int q = -gridRadius; q <= gridRadius; q++)
            {
                int r1 = Mathf.Max(-gridRadius, -q - gridRadius);
                int r2 = Mathf.Min(gridRadius, -q + gridRadius);
                for (int r = r1; r <= r2; r++)
                {
                    CreateCell(new HexCoordinates(q, r));
                }
            }
        }

        private void CreateCell(HexCoordinates coords)
        {
            HexCell cell = Instantiate(hexCellPrefab, transform);
            cell.transform.localPosition = coords.ToWorldPosition(hexSize);
            cell.coordinates = coords;
            cell.relief = RollRelief();
            cell.name = $"Hex {coords}";
            _cells[coords] = cell;
        }

        private TerrainRelief RollRelief()
        {
            float roll = Random.value;
            if (roll < waterfallChance) return TerrainRelief.Waterfall;
            roll -= waterfallChance;
            if (roll < mountainChance) return TerrainRelief.Mountain;
            roll -= mountainChance;
            if (roll < coastChance) return TerrainRelief.Coast;
            return TerrainRelief.Mutable;
        }

        public bool TryGetCell(HexCoordinates coords, out HexCell cell) => _cells.TryGetValue(coords, out cell);
    }
}
