using System.Collections.Generic;
using Solarpunk.Tiles;
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

        [Tooltip("Terrain feature every open-ground hex starts covered in, bar the one clearing.")]
        [SerializeField] private TileDefinition forestDefinition;

        [SerializeField] private float hexSize = 1f;
        [SerializeField] private float spacing = 1.04f;

        private readonly Dictionary<HexCoordinates, HexCell> _cells = new();
        private Vector3 _centroid;

        public IReadOnlyDictionary<HexCoordinates, HexCell> Cells => _cells;

        private readonly struct CellPlan
        {
            public readonly int Q;
            public readonly int R;
            public readonly TerrainRelief Relief;

            /// <summary>Open ground starts forested unless this is set. Exactly one hex is.</summary>
            public readonly bool StartsCleared;

            public CellPlan(int q, int r, TerrainRelief relief, bool startsCleared = false)
            {
                Q = q;
                R = r;
                Relief = relief;
                StartsCleared = startsCleared;
            }
        }

        private static readonly CellPlan[] Layout =
        {
            new CellPlan(0, -1, TerrainRelief.Mountain),
            new CellPlan(1, -1, TerrainRelief.Mutable),
            new CellPlan(2, -1, TerrainRelief.Coast),

            new CellPlan(-1, 0, TerrainRelief.Mutable),
            new CellPlan(0, 0, TerrainRelief.Mutable, startsCleared: true),
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
            _centroid = sum / Layout.Length;

            foreach (var plan in Layout)
            {
                Spawn(new HexCoordinates(plan.Q, plan.R), plan.Relief,
                    plan.Relief == TerrainRelief.Mutable && !plan.StartsCleared);
            }
        }

        private HexCell Spawn(HexCoordinates coords, TerrainRelief relief, bool forested)
        {
            HexCell cell = Instantiate(hexCellPrefab, transform);
            cell.transform.localPosition = (coords.ToWorldPosition(hexSize) - _centroid) * spacing;
            cell.transform.localScale = Vector3.one * hexSize;
            cell.name = $"Hex {coords} {relief}";
            cell.Initialize(coords, relief);
            if (forested) SeedForest(cell);
            _cells[coords] = cell;
            return cell;
        }

        private static readonly HexCoordinates[] Directions =
        {
            new(1, 0), new(1, -1), new(0, -1), new(-1, 0), new(-1, 1), new(0, 1)
        };

        /// <summary>
        /// One new hex on the shore, touching the island (events: war conquest, a
        /// territory purchase). Its relief is drawn at random, open ground twice as
        /// likely as rock or coast; open ground arrives wooded like the rest.
        /// Waterfall is left out: it only takes hydro, and a second one is a map decision.
        /// </summary>
        public HexCell AddTerritory(System.Random rng)
        {
            var free = new List<HexCoordinates>();
            foreach (HexCoordinates c in _cells.Keys)
            {
                foreach (HexCoordinates d in Directions)
                {
                    var n = new HexCoordinates(c.q + d.q, c.r + d.r);
                    if (!_cells.ContainsKey(n) && !free.Contains(n)) free.Add(n);
                }
            }
            if (free.Count == 0) return null;

            TerrainRelief[] reliefs = { TerrainRelief.Mutable, TerrainRelief.Mutable, TerrainRelief.Mountain, TerrainRelief.Coast };
            TerrainRelief relief = reliefs[rng.Next(reliefs.Length)];
            return Spawn(free[rng.Next(free.Count)], relief, relief == TerrainRelief.Mutable);
        }

        /// <summary>
        /// The island arrives already wooded: every open-ground hex but the one
        /// clearing carries a forest, which the player pays to clear before building
        /// there. Relief-restricted hexes (water, coast, mountain) grow nothing.
        /// </summary>
        private void SeedForest(HexCell cell)
        {
            if (forestDefinition == null) return;

            cell.builtTile = forestDefinition;
            cell.SetStructure(StructureFactory.Create(forestDefinition));
        }

        public bool TryGetCell(HexCoordinates coords, out HexCell cell) => _cells.TryGetValue(coords, out cell);
    }
}
