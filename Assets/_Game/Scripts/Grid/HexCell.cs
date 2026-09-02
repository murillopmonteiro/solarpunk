using Solarpunk.Tiles;
using UnityEngine;

namespace Solarpunk.Grid
{
    /// <summary>
    /// A single hex on the board: fixed coordinates + relief, whatever tile is
    /// currently built on it, and its placeholder visuals.
    /// </summary>
    public class HexCell : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public HexCoordinates coordinates;
        public TerrainRelief relief = TerrainRelief.Mutable;

        public TileDefinition builtTile;
        public int cityLevel = 1; // only meaningful when builtTile.category == City

        [SerializeField] private Renderer tileRenderer;

        private Color _baseColor;
        private bool _selected;
        private GameObject _structure;
        private float _restingY;

        public bool IsEmpty => builtTile == null;

        private void Awake()
        {
            if (tileRenderer == null) tileRenderer = GetComponentInChildren<Renderer>();
        }

        /// <summary>Called by the grid manager right after instantiation.</summary>
        public void Initialize(HexCoordinates coords, TerrainRelief cellRelief)
        {
            coordinates = coords;
            relief = cellRelief;
            _restingY = transform.localPosition.y;
            _baseColor = ColorForRelief(cellRelief);
            ApplyColor(_baseColor);
        }

        public static Color ColorForRelief(TerrainRelief relief)
        {
            return relief switch
            {
                TerrainRelief.Waterfall => new Color(0.25f, 0.55f, 0.85f),
                TerrainRelief.Mountain => new Color(0.52f, 0.50f, 0.48f),
                TerrainRelief.Coast => new Color(0.88f, 0.82f, 0.55f),
                _ => new Color(0.38f, 0.66f, 0.36f)
            };
        }

        public void SetSelected(bool selected)
        {
            if (_selected == selected) return;
            _selected = selected;

            ApplyColor(selected ? Color.Lerp(_baseColor, Color.white, 0.45f) : _baseColor);

            Vector3 p = transform.localPosition;
            p.y = _restingY + (selected ? 0.12f : 0f);
            transform.localPosition = p;
        }

        public void SetStructure(GameObject structure)
        {
            ClearStructure();
            _structure = structure;
            if (_structure != null) _structure.transform.SetParent(transform, false);
        }

        public void ClearStructure()
        {
            if (_structure != null) Destroy(_structure);
            _structure = null;
        }

        /// <summary>Relief rule from design doc §2 — hydro needs a waterfall, tidal needs coast, etc.</summary>
        public bool CanBuild(TileDefinition definition)
        {
            if (definition == null || builtTile != null) return false;
            if (definition.requiredRelief == TerrainRelief.Mutable) return true;
            return definition.requiredRelief == relief;
        }

        private void ApplyColor(Color color)
        {
            if (tileRenderer == null) return;

            // Instanced material — fine at this scale, and keeps edit-mode tinting off the shared asset.
            Material material = Application.isPlaying ? tileRenderer.material : tileRenderer.sharedMaterial;
            if (material == null) return;

            if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);
        }
    }
}
