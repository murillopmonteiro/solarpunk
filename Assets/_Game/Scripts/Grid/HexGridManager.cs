using System.Collections.Generic;
using UnityEngine;

namespace Solarpunk.Grid
{
    /// <summary>
    /// Builds the board at match start. For this first playable test the layout
    /// is a hand-authored 10-hex island (rows of 3/4/3) with a fixed relief
    /// spread, so every relief restriction from the design doc is reachable:
    /// 1 waterfall (hydro), 2 coast (tidal), 2 mountain (wind), 5 open.
    /// Randomised generation returns once the systems are proven.
    /// </summary>
    public class HexGridManager : MonoBehaviour
    {
        [SerializeField] private HexCell hexCellPrefab;
        [SerializeField] private float hexSize = 1f;
        [SerializeField] private float spacing = 1.04f;

        private readonly Dictionary<HexCoordinates, HexCell> _cells = new();

        public IReadOnlyDictionary<HexCoordinates, HexCell> Cells => _cells;

        private readonly struct CellPlan
        {
            public readonly int Q;
            public readonly int R;
            public readonly TerrainRelief Relief;

            public CellPlan(int q, int r, TerrainRelief relief)
            {
                Q = q;
                R = r;
                Relief = relief;
            }
        }

        private static readonly CellPlan[] Layout =
        {
            new CellPlan(0, -1, TerrainRelief.Mountain),
            new CellPlan(1, -1, TerrainRelief.Mutable),
            new CellPlan(2, -1, TerrainRelief.Coast),

            new CellPlan(-1, 0, TerrainRelief.Mutable),
            new CellPlan(0, 0, TerrainRelief.Mutable),
            new CellPlan(1, 0, TerrainRelief.Waterfall),
            new CellPlan(2, 0, TerrainRelief.Coast),

            new CellPlan(-1, 1, TerrainRelief.Mountain),
            new CellPlan(0, 1, TerrainRelief.Mutable),
            new CellPlan(1, 1, TerrainRelief.Mutable),
        };

        public void GenerateGrid()
        {
            foreach (var existing in _cells.Values)
            {
                if (existing != null) Destroy(existing.gameObject);
            }
            _cells.Clear();

            // Centre the island on the origin so camera framing is stable.
            Vector3 sum = Vector3.zero;
            foreach (var plan in Layout)
            {
                sum += new HexCoordinates(plan.Q, plan.R).ToWorldPosition(hexSize);
            }
            Vector3 centroid = sum / Layout.Length;

            foreach (var plan in Layout)
            {
                var coords = new HexCoordinates(plan.Q, plan.R);
                HexCell cell = Instantiate(hexCellPrefab, transform);
                cell.transform.localPosition = (coords.ToWorldPosition(hexSize) - centroid) * spacing;
                cell.transform.localScale = Vector3.one * hexSize;
                cell.name = $"Hex {coords} {plan.Relief}";
                cell.Initialize(coords, plan.Relief);
                _cells[coords] = cell;
            }
        }

        public bool TryGetCell(HexCoordinates coords, out HexCell cell) => _cells.TryGetValue(coords, out cell);
    }
}
