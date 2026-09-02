using System.Collections.Generic;
using Solarpunk.Grid;
using Solarpunk.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Right-hand contextual panel: shows the selected hex, and either the
    /// buildable options for it or what's already there (with a demolish, and
    /// for the city an upgrade, control).
    /// </summary>
    public class BuildPanel : MonoBehaviour
    {
        private const float PanelWidth = 310f;
        private const float RowHeight = 44f;

        private BuildController _buildController;
        private CityGrowth _cityGrowth;

        private RectTransform _root;
        private RectTransform _content;
        private Text _header;
        private Text _subheader;
        private Text _emptyHint;

        private readonly List<GameObject> _rows = new();

        public void Build(Transform canvas, BuildController buildController, CityGrowth cityGrowth)
        {
            _buildController = buildController;
            _cityGrowth = cityGrowth;

            _root = UIFactory.Panel("BuildPanel", canvas, UIFactory.PanelColor);
            _root.anchorMin = new Vector2(1f, 0f);
            _root.anchorMax = new Vector2(1f, 1f);
            _root.pivot = new Vector2(1f, 1f);
            _root.offsetMin = new Vector2(-PanelWidth, 16f);
            _root.offsetMax = new Vector2(-16f, -86f);

            _header = UIFactory.Label("Header", _root, "NO HEX SELECTED", 16, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            _header.rectTransform.anchorMin = new Vector2(0f, 1f);
            _header.rectTransform.anchorMax = new Vector2(1f, 1f);
            _header.rectTransform.pivot = new Vector2(0.5f, 1f);
            _header.rectTransform.offsetMin = new Vector2(16f, -34f);
            _header.rectTransform.offsetMax = new Vector2(-16f, -12f);

            _subheader = UIFactory.Label("Subheader", _root, "Click a hexagon to begin.", 12, UIFactory.MutedColor,
                TextAnchor.UpperLeft);
            _subheader.rectTransform.anchorMin = new Vector2(0f, 1f);
            _subheader.rectTransform.anchorMax = new Vector2(1f, 1f);
            _subheader.rectTransform.pivot = new Vector2(0.5f, 1f);
            _subheader.rectTransform.offsetMin = new Vector2(16f, -58f);
            _subheader.rectTransform.offsetMax = new Vector2(-16f, -38f);

            _content = UIFactory.NewRect("Content", _root);
            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.offsetMin = new Vector2(10f, 10f);
            _content.offsetMax = new Vector2(-10f, -66f);
            UIFactory.VerticalLayout(_content, 6, new RectOffset(0, 0, 0, 0));

            _emptyHint = UIFactory.Label("EmptyHint", _content, "", 12, UIFactory.MutedColor, TextAnchor.UpperLeft);
            UIFactory.FixedHeight(_emptyHint.rectTransform, 40f);
        }

        public void Show(HexCell cell)
        {
            ClearRows();

            if (cell == null)
            {
                _header.text = "NO HEX SELECTED";
                _subheader.text = "Click a hexagon to begin.";
                _emptyHint.text = "";
                return;
            }

            _header.text = $"HEX {cell.coordinates}";
            _subheader.text = $"Terrain: {cell.relief}";

            if (cell.IsEmpty) ShowBuildOptions(cell);
            else ShowBuiltTile(cell);
        }

        private void ShowBuildOptions(HexCell cell)
        {
            _emptyHint.text = "BUILD HERE";

            foreach (TileDefinition definition in _buildController.Palette)
            {
                if (definition == null) continue;

                string blocked = _buildController.BlockReason(cell, definition);
                CreateRow(
                    definition.displayName,
                    definition.ShortSummary(),
                    blocked,
                    definition.placeholderColor,
                    () => _buildController.TryBuild(cell, definition));
            }
        }

        private void ShowBuiltTile(HexCell cell)
        {
            TileDefinition built = cell.builtTile;
            _emptyHint.text = built.category == TileCategory.City
                ? $"BUILT: {built.displayName}  (level {cell.cityLevel})"
                : $"BUILT: {built.displayName}";

            if (built.category == TileCategory.City && cell.cityLevel < CityGrowth.MaxLevel)
            {
                float cost = _cityGrowth.ManualUpgradeCost(cell.cityLevel);
                CreateRow(
                    $"Upgrade to level {cell.cityLevel + 1}",
                    $"Instant  ·  ${cost:0}",
                    null,
                    UIFactory.AccentColor,
                    () => _cityGrowth.TryManualUpgrade(cell));
            }

            CreateRow(
                "Demolish",
                "Clears the hex, refunds half",
                null,
                UIFactory.WarnColor,
                () => _buildController.Demolish(cell));
        }

        private void CreateRow(string title, string subtitle, string blockedReason, Color swatch,
            UnityEngine.Events.UnityAction onClick)
        {
            bool enabled = blockedReason == null;

            Button button = UIFactory.Button(title, _content, UIFactory.PanelSoftColor);
            UIFactory.FixedHeight(button.GetComponent<RectTransform>(), RowHeight);
            button.interactable = enabled;
            if (enabled) button.onClick.AddListener(onClick);

            // Colour chip so build options read at a glance.
            RectTransform chip = UIFactory.Panel("Chip", button.transform, swatch);
            chip.anchorMin = new Vector2(0f, 0.5f);
            chip.anchorMax = new Vector2(0f, 0.5f);
            chip.pivot = new Vector2(0f, 0.5f);
            chip.anchoredPosition = new Vector2(9f, 0f);
            chip.sizeDelta = new Vector2(5f, RowHeight - 16f);
            chip.GetComponent<Image>().raycastTarget = false;

            Text titleText = UIFactory.Label("Title", button.transform, title, 13,
                enabled ? UIFactory.TextColor : UIFactory.MutedColor, TextAnchor.LowerLeft, FontStyle.Bold);
            titleText.rectTransform.anchorMin = Vector2.zero;
            titleText.rectTransform.anchorMax = Vector2.one;
            titleText.rectTransform.offsetMin = new Vector2(22f, RowHeight * 0.42f);
            titleText.rectTransform.offsetMax = new Vector2(-10f, -5f);

            Text subtitleText = UIFactory.Label("Subtitle", button.transform,
                enabled ? subtitle : $"{subtitle}   ·   {blockedReason}", 11,
                enabled ? UIFactory.MutedColor : UIFactory.WarnColor, TextAnchor.UpperLeft);
            subtitleText.rectTransform.anchorMin = Vector2.zero;
            subtitleText.rectTransform.anchorMax = Vector2.one;
            subtitleText.rectTransform.offsetMin = new Vector2(22f, 5f);
            subtitleText.rectTransform.offsetMax = new Vector2(-10f, -RowHeight * 0.55f);

            _rows.Add(button.gameObject);
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
