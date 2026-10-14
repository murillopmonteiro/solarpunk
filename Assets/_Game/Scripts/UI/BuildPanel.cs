using System;
using System.Collections.Generic;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Tiles;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Right-hand inspector for the selected hex: what the terrain is, what sits
    /// on it, and what can go there. Every option carries its cost and its yearly
    /// effect, so a choice can be made without trial and error.
    /// </summary>
    public class BuildPanel : MonoBehaviour
    {
        // Sits beside the resource ribbon, so the ten-row build list clears the action stack.
        public const float PanelWidth = 410f;
        public const float PanelTop = 28f;

        private const float HeaderHeight = 126f;
        private const float RowHeight = 58f;
        private const float RowGap = 6f;
        private const float SidePad = 18f;
        private const float BottomPad = 20f;

        private BuildController _buildController;
        private CityGrowth _cityGrowth;

        private RectTransform _root;
        private Text _header;
        private Text _terrainLabel;
        private RectTransform _terrainDot;
        private Image _terrainDotImage;
        private Text _sectionLabel;

        private readonly List<GameObject> _rows = new();

        public void Build(Transform canvas, BuildController buildController, CityGrowth cityGrowth)
        {
            _buildController = buildController;
            _cityGrowth = cityGrowth;

            _root = UIFactory.Panel("BuildPanel", canvas);
            UIFactory.Place(_root, UIFactory.TopRight, -34f, -PanelTop, PanelWidth, 300f);

            _header = UIFactory.Label("Header", _root, Loc.T("build.noSelection"), 30, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            // "NENHUM HEXÁGONO SELECIONADO" outruns the panel at full size; shrink rather than clip.
            _header.horizontalOverflow = HorizontalWrapMode.Wrap; // best fit only shrinks wrapping text
            _header.resizeTextForBestFit = true;
            _header.resizeTextMinSize = 18;
            _header.resizeTextMaxSize = 30;
            UIFactory.Place(_header.rectTransform, UIFactory.TopLeft, SidePad + 6f, -22f,
                PanelWidth - SidePad * 2f - 12f, 38f);

            _terrainDot = UIFactory.HexBadge("TerrainDot", _root, Color.clear, 24f, 0f);
            UIFactory.Place(_terrainDot, UIFactory.TopLeft, SidePad + 6f, -64f, 24f, 21f);
            _terrainDotImage = _terrainDot.GetComponent<Image>();

            _terrainLabel = UIFactory.Label("Terrain", _root, Loc.T("build.clickToInspect"), 18,
                UIFactory.MutedColor);
            // "Terreno livre. Aceita qualquer construção." runs to the panel edge in PT.
            _terrainLabel.horizontalOverflow = HorizontalWrapMode.Wrap; // best fit only shrinks wrapping text
            _terrainLabel.resizeTextForBestFit = true;
            _terrainLabel.resizeTextMinSize = 13;
            _terrainLabel.resizeTextMaxSize = 18;
            UIFactory.Place(_terrainLabel.rectTransform, UIFactory.TopLeft, SidePad + 40f, -62f,
                PanelWidth - SidePad * 2f - 46f, 24f);

            _sectionLabel = UIFactory.Label("Section", _root, "", 14, UIFactory.FaintColor);
            UIFactory.Place(_sectionLabel.rectTransform, UIFactory.TopLeft, SidePad + 6f, -98f,
                PanelWidth - SidePad * 2f, 20f);
        }

        public void Show(HexCell cell)
        {
            ClearRows();

            if (cell == null)
            {
                _header.text = Loc.T("build.noSelection");
                _terrainLabel.text = Loc.T("build.clickToInspect");
                _terrainDotImage.color = Color.clear;
                _sectionLabel.text = "";
                Resize(ShowTerritoryOffer());
                return;
            }

            _header.text = Loc.T("build.hex", cell.coordinates);
            _terrainLabel.text = TerrainDescription(cell.relief);
            _terrainDotImage.color = HexCell.ColorForRelief(cell.relief);

            int rowCount = cell.IsEmpty ? ShowBuildOptions(cell) : ShowBuiltTile(cell);
            Resize(rowCount);
        }

        private static string TerrainDescription(TerrainRelief relief)
        {
            return relief switch
            {
                TerrainRelief.Waterfall => Loc.T("build.descWaterfall"),
                TerrainRelief.Mountain => Loc.T("build.descMountain"),
                TerrainRelief.Coast => Loc.T("build.descCoast"),
                _ => Loc.T("build.descOpen")
            };
        }

        private int ShowBuildOptions(HexCell cell)
        {
            _sectionLabel.text = Loc.T("build.buildHere");

            // Eighteen tiles do not fit the column, so only what this hex could ever take
            // is listed: wrong terrain and not-yet-researched tiles are left out (the
            // research tree shows what is coming). What can go here comes first; blocked
            // options sink below, palette order kept within each.
            var options = _buildController.Palette
                .Where(d => d != null && d.AllowedOn(cell.relief))
                .Where(d => _buildController.Research == null || !_buildController.Research.IsTileLocked(d.name))
                .Select(d => (definition: d, blocked: _buildController.BlockReason(cell, d)))
                .OrderBy(o => o.blocked != null);

            int index = 0;
            foreach ((TileDefinition definition, string blocked) in options)
            {
                string detail = definition.category == TileCategory.City
                    ? Loc.T("build.cityDetail")
                    : StatFormat.EffectSummary(_buildController.PreviewOn(definition, cell.relief));

                // Both figures are what this hex charges and yields, so a terrain bonus
                // shows up as a cheaper price and a bigger number, not just as a label.
                if (definition.BonusOn(cell.relief) != null)
                    detail += "   ·   " + Loc.T("build.bonus", TerrainReliefNames.Describe(cell.relief).ToUpper());

                CreateRow(index++, definition.LocalizedName, $"${_buildController.CostOn(definition, cell.relief):0}",
                    detail, blocked, definition.placeholderColor,
                    () => _buildController.TryBuild(cell, definition));
            }

            return index;
        }

        private int ShowBuiltTile(HexCell cell)
        {
            TileDefinition built = cell.builtTile;
            bool isCity = built.category == TileCategory.City;

            string builtName = built.LocalizedName.ToUpper();
            _sectionLabel.text = isCity ? Loc.T("build.builtCity", builtName, cell.cityLevel)
                : built.category == TileCategory.Forest ? Loc.T("build.growing", builtName)
                : Loc.T("build.built", builtName);

            int index = 0;

            string effect = cell.IsMature
                ? StatFormat.EffectSummary(_buildController.OutputOf(cell))
                : Loc.T("build.maturesIn", cell.YearsToMaturity);
            if (!isCity && built.BonusOn(cell.relief) != null)
                effect += "   ·   " + Loc.T("build.bonus", TerrainReliefNames.Describe(cell.relief).ToUpper());

            CreateRow(index++, Loc.T("build.currentOutput"), "", effect, null, built.placeholderColor, null);

            if (isCity && cell.cityLevel < _cityGrowth.LevelCap)
            {
                float cost = _cityGrowth.ManualUpgradeCost(cell.cityLevel);
                CreateRow(index++, Loc.T("build.upgrade", cell.cityLevel + 1), $"${cost:0}",
                    Loc.T("build.upgradeDetail"), null, UIFactory.Leaf,
                    () => _cityGrowth.TryManualUpgrade(cell));
            }
            else if (isCity && cell.cityLevel < CityGrowth.MaxLevel)
            {
                // Capped by research, not by the model: say so, or the missing row reads as a bug.
                CreateRow(index++, Loc.T("build.capped", _cityGrowth.LevelCap), "",
                    Loc.T("build.cappedDetail"), null, UIFactory.FaintColor, null);
            }

            // A forest is felled at a price; anything the player paid for pays back.
            bool paidClearing = built.clearCost > 0f;
            string clearTitle = paidClearing ? Loc.T("build.clearLand") : Loc.T("build.demolish");
            string clearPrice = paidClearing
                ? $"-${_buildController.ClearCostOf(built):0}"
                : $"+${_buildController.RefundFor(cell):0}";
            string clearDetail = paidClearing
                ? Loc.T("build.clearDetail")
                : Loc.T("build.demolishDetail");

            CreateRow(index++, clearTitle, clearPrice, clearDetail,
                _buildController.DemolishBlockReason(cell), UIFactory.WarnColor,
                () => _buildController.Demolish(cell));

            return index;
        }

        /// <summary>With nothing selected: the one board-wide action, buying a coast hex, once researched.</summary>
        private int ShowTerritoryOffer()
        {
            float price = _buildController.TerritoryPrice;
            if (price <= 0f) return 0;

            CreateRow(0, Loc.T("build.buyTerritory"), $"${price:0}", Loc.T("build.buyTerritoryDetail"), null,
                HexCell.ColorForRelief(TerrainRelief.Coast), () => _buildController.TryBuyTerritory());
            return 1;
        }

        /// <param name="onClick">null makes the row a read-only info card.</param>
        private void CreateRow(int index, string title, string trailing, string detail, string blockedReason,
            Color chip, Action onClick)
        {
            bool interactive = onClick != null && blockedReason == null;
            float y = -(HeaderHeight + index * (RowHeight + RowGap));
            float width = PanelWidth - SidePad * 2f;

            Color background = blockedReason == null ? UIFactory.PaperDeep : UIFactory.PaperShade;
            Button button = UIFactory.Button(title, _root, background);
            RectTransform rect = button.GetComponent<RectTransform>();
            UIFactory.Place(rect, UIFactory.TopLeft, SidePad, y, width, RowHeight);

            button.interactable = interactive;
            if (interactive) button.onClick.AddListener(() => onClick());

            RectTransform badge = UIFactory.HexBadge("Badge", rect,
                blockedReason == null ? chip : UIFactory.FaintColor, 42f, 3f);
            UIFactory.Place(badge, UIFactory.TopLeft, 11f, -9f, 42f, 37f);

            Color titleColor = blockedReason == null ? UIFactory.TextColor : UIFactory.FaintColor;

            Text titleText = UIFactory.Label("Title", rect, title, 20, titleColor, TextAnchor.UpperLeft,
                FontStyle.Bold);
            UIFactory.Place(titleText.rectTransform, UIFactory.TopLeft, 64f, -5f, width - 170f, 27f);

            if (!string.IsNullOrEmpty(trailing))
            {
                Color costColor = blockedReason == null ? UIFactory.CostColor : UIFactory.FaintColor;
                Text costText = UIFactory.Label("Cost", rect, trailing, 20, costColor,
                    TextAnchor.UpperRight, FontStyle.Bold);
                UIFactory.Place(costText.rectTransform, UIFactory.TopRight, -14f, -5f, 104f, 27f);
            }

            Text detailText = UIFactory.Label("Detail", rect,
                blockedReason ?? detail, 14,
                blockedReason == null ? UIFactory.MutedColor : UIFactory.WarnColor);
            // Effect lists with a terrain bonus run long; shrink rather than spill past the row.
            detailText.resizeTextForBestFit = true;
            detailText.resizeTextMinSize = 11;
            detailText.resizeTextMaxSize = 14;
            detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.Place(detailText.rectTransform, UIFactory.TopLeft, 64f, -32f, width - 76f, 20f);

            _rows.Add(button.gameObject);
        }

        private void Resize(int rowCount)
        {
            float height = rowCount == 0
                ? HeaderHeight - 30f
                : HeaderHeight + rowCount * (RowHeight + RowGap) - RowGap + BottomPad;

            _root.sizeDelta = new Vector2(PanelWidth, height);

            // Late game on open ground still lists ~14 tiles; shrink the whole card to stay
            // above the Research / Next Year stack rather than run under it.
            _root.localScale = Vector3.one * Mathf.Min(1f, MaxHeight / height);
        }

        /// <summary>Tallest the card may be at reference resolution and still clear the bottom-right buttons.</summary>
        private const float MaxHeight = 780f;

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
