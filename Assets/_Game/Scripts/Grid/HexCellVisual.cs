using UnityEngine;

namespace Solarpunk.Grid
{
    /// <summary>Placeholder colored-block visual for a hex cell, standing in for real art.</summary>
    [RequireComponent(typeof(HexCell))]
    public class HexCellVisual : MonoBehaviour
    {
        [SerializeField] private Renderer cellRenderer;

        private void Start()
        {
            var cell = GetComponent<HexCell>();
            if (cellRenderer == null) cellRenderer = GetComponentInChildren<Renderer>();
            if (cellRenderer == null) return;

            Color color = cell.relief switch
            {
                TerrainRelief.Waterfall => new Color(0.2f, 0.5f, 0.9f),
                TerrainRelief.Mountain => new Color(0.5f, 0.5f, 0.5f),
                TerrainRelief.Coast => new Color(0.9f, 0.85f, 0.55f),
                _ => new Color(0.35f, 0.7f, 0.35f)
            };

            cellRenderer.material.color = color;
        }
    }
}
