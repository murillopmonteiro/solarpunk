using System;
using System.Collections.Generic;
using Solarpunk.Grid;
using Solarpunk.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Right-hand inspector for the selected hex: what the terrain is, what's on
    /// it, and what can go there. Every option shows its cost and its yearly
    /// effect up front, so the choice can be made without trial and error.
    /// </summary>
    public class BuildPanel : MonoBehaviour
    {
        public const float PanelWidth = 348f;

        private const float HeaderHeight = 92f;
        private const float RowHeight = 46f;
        private const float RowGap = 5f;
        private const float SidePad = 12f;
        private const float BottomPad = 12f;

        private BuildController _buildController;
        private CityGrowth _cityGrowth;

        private RectTransform _root;
        private Text _header;
        private Text _terrainLabel;
        private RectTransform _terrainSwatch;
        private Image _terrainSwatchImage;
        private Text _sectionLabel;

        private readonly List<GameObject> _rows = new();

        public void Build(Transform canvas, BuildController buildController, CityGrowth cityGrowth)
        {
            _buildController = buildController;
            _cityGrowth = cityGrowth;

            _root = UIFactory.Panel("BuildPanel", canvas, UIFactory.PanelColor);
            UIFactory.Place(_root, UIFactory.TopRight, -16f, -(ResourceBar.BarHeight + 14f), PanelWidth, 300f);

            _header = UIFactory.Label("Header", _root, "NO HEX SELECTED", 16, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(_header.rectTransform, UIFactory.TopLeft, SidePad, -12f, PanelWidth - SidePad * 2f, 22f);

            _terrainSwatch = UIFactory.Panel("TerrainSwatch", _root, Color.clear);
            UIFactory.Place(_terrainSwatch, UIFactory.TopLeft, SidePad, -38f, 10f, 10f);
            _terrainSwatchImage = _terrainSwatch.GetComponent<Image>();
            _terrainSwatchImage.raycastTarget = false;

            _terrainLabel = UIFactory.Label("Terrain", _root, "Click a hexagon to inspect it", 12,
                UIFactory.MutedColor);
            UIFactory.Place(_terrainLabel.rectTransform, UIFactory.TopLeft, SidePad + 16f, -38f,
                PanelWidth - SidePad * 2f - 16f, 18f);

            UIFactory.Divider(_root, SidePad, -60f, PanelWidth - SidePad * 2f);

            // Sits between the divider and the first row — must clear both.
            _sectionLabel = UIFactory.Label("Section", _root, "", 11, UIFactory.FaintColor);
            UIFactory.Place(_sectionLabel.rectTransform, UIFactory.TopLeft, SidePad, -70f,
                PanelWidth - SidePad * 2f, 15f);
        }

        public void Show(HexCell cell)
        {
            ClearRows();

            if (cell == null)
            {
                _header.text = "NO HEX SELECTED";
                _terrainLabel.text = "Click a hexagon to inspect it";
                _terrainSwatchImage.color = Color.clear;
                _sectionLabel.text = "";
                Resize(0);
                return;
            }

            _header.text = $"HEX {cell.coordinates}";
            _terrainLabel.text = TerrainDescription(cell.relief);
            _terrainSwatchImage.color = HexCell.ColorForRelief(cell.relief);

            int rowCount = cell.IsEmpty ? ShowBuildOptions(cell) : ShowBuiltTile(cell);
            Resize(rowCount);
        }

        private static string TerrainDescription(TerrainRelief relief)
        {
            return relief switch
            {
                TerrainRelief.Waterfall => "Waterfall — the only hydro site",
                TerrainRelief.Mountain => "Mountain — favours wind",
                TerrainRelief.Coast => "Coast — the only tidal site",
                _ => "Open ground — no restriction"
            };
        }

        private int ShowBuildOptions(HexCell cell)
        {
            _sectionLabel.text = "BUILD ON THIS HEX";

            int index = 0;
            foreach (TileDefinition definition in _buildController.Palette)
            {
                if (definition == null) continue;

                string blocked = _buildController.BlockReason(cell, definition);
                string detail = definition.category == TileCategory.City
                    ? "Grows each year, powers your population"
                    : StatFormat.EffectSummary(definition.perTurnEffect);

                CreateRow(index++, definition.displayName, $"${definition.buildCost:0}", detail, blocked,
                    definition.placeholderColor, () => _buildController.TryBuild(cell, definition));
            }

            return index;
        }

        private int ShowBuiltTile(HexCell cell)
        {
            TileDefinition built = cell.builtTile;
            bool isCity = built.category == TileCategory.City;

            _sectionLabel.text = isCity
                ? $"BUILT — {built.displayName.ToUpper()}  ·  LEVEL {cell.cityLevel}"
                : $"BUILT — {built.displayName.ToUpper()}";

            int index = 0;

            string effect = isCity
                ? StatFormat.EffectSummary(_cityGrowth.GetEffectForLevel(cell.cityLevel))
                : StatFormat.EffectSummary(built.perTurnEffect);
            CreateRow(index++, "Current output", "", effect, null, built.placeholderColor, null);

            if (isCity && cell.cityLevel < CityGrowth.MaxLevel)
            {
                float cost = _cityGrowth.ManualUpgradeCost(cell.cityLevel);
                CreateRow(index++, $"Upgrade to level {cell.cityLevel + 1}", $"${cost:0}",
                    "Instant, skips waiting for growth", null, UIFactory.AccentColor,
                    () => _cityGrowth.TryManualUpgrade(cell));
            }

            CreateRow(index++, "Demolish", $"+${built.buildCost * 0.5f:0}",
                "Clears the hex and refunds half the cost", null, UIFactory.WarnColor,
                () => _buildController.Demolish(cell));

            return index;
        }

        /// <param name="onClick">null makes the row a read-only info strip.</param>
        private void CreateRow(int index, string title, string trailing, string detail, string blockedReason,
            Color chip, Action onClick)
        {
            bool interactive = onClick != null && blockedReason == null;
            float y = -(HeaderHeight + index * (RowHeight + RowGap));
            float width = PanelWidth - SidePad * 2f;

            Color background = blockedReason == null ? UIFactory.RowColor : UIFactory.RowDisabledColor;
            Button button = UIFactory.Button(title, _root, background);
            RectTransform rect = button.GetComponent<RectTransform>();
            UIFactory.Place(rect, UIFactory.TopLeft, SidePad, y, width, RowHeight);

            button.interactable = interactive;
            if (interactive) button.onClick.AddListener(() => onClick());

            UIFactory.Swatch(rect, 0f, 0f, 3f, RowHeight, blockedReason == null ? chip : UIFactory.FaintColor);

            Color titleColor = blockedReason == null ? UIFactory.TextColor : UIFactory.FaintColor;

            // Title on the upper line, cost right-aligned beside it.
            Text titleText = UIFactory.Label("Title", rect, title, 14, titleColor, TextAnchor.UpperLeft,
                FontStyle.Bold);
            UIFactory.Place(titleText.rectTransform, UIFactory.TopLeft, 13f, -7f, width - 110f, 19f);

            if (!string.IsNullOrEmpty(trailing))
            {
                Color costColor = blockedReason == null ? UIFactory.TextColor : UIFactory.FaintColor;
                Text costText = UIFactory.Label("Cost", rect, trailing, 14, costColor, TextAnchor.UpperRight);
                UIFactory.Place(costText.rectTransform, UIFactory.TopRight, -11f, -7f, 100f, 19f);
            }

            // Detail on the lower line — the yearly effect, or why it's blocked.
            Text detailText = UIFactory.Label("Detail", rect,
                blockedReason ?? detail, 11,
                blockedReason == null ? UIFactory.MutedColor : UIFactory.WarnColor);
            UIFactory.Place(detailText.rectTransform, UIFactory.TopLeft, 13f, -26f, width - 24f, 16f);

            _rows.Add(button.gameObject);
        }

        private void Resize(int rowCount)
        {
            float height = rowCount == 0
                ? HeaderHeight - 6f
                : HeaderHeight + rowCount * (RowHeight + RowGap) - RowGap + BottomPad;

            _root.sizeDelta = new Vector2(PanelWidth, height);
        }

        private void ClearRows()
        {
            foreach (GameObject row in _rows)
            {
                if (row != null) Destroy(row);
            }
            _rows.Clear();
        }
    }
}
