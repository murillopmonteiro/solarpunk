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
        // A narrow card: the list scrolls instead of growing, so the board stays in view.
        public const float PanelWidth = 340f;
        public const float PanelTop = 28f;

        private const float HeaderHeight = 100f;
        private const float RowHeight = 50f;
        private const float RowGap = 5f;
        private const float SidePad = 16f;
        private const float BottomPad = 16f;
        private const float ScrollbarWidth = 6f;
        private const float ListWidth = PanelWidth - SidePad * 2f - ScrollbarWidth - 4f;

        /// <summary>Tallest the list gets before it scrolls: about five and a half rows.</summary>
        private const float MaxListHeight = 300f;

        private BuildController _buildController;
        private CityGrowth _cityGrowth;

        private RectTransform _root;
        private Text _header;
        private Text _terrainLabel;
        private RectTransform _terrainDot;
        private Image _terrainDotImage;
        private Text _sectionLabel;
        private RectTransform _viewport;
        private RectTransform _content;

        private readonly List<GameObject> _rows = new();

        public void Build(Transform canvas, BuildController buildController, CityGrowth cityGrowth)
        {
            _buildController = buildController;
            _cityGrowth = cityGrowth;

            _root = UIFactory.Panel("BuildPanel", canvas);
            UIFactory.Place(_root, UIFactory.TopRight, -34f, -PanelTop, PanelWidth, 300f);

            _header = UIFactory.Label("Header", _root, Loc.T("build.noSelection"), 24, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            // "NENHUM HEXÁGONO SELECIONADO" outruns the panel at full size; shrink rather than clip.
            _header.horizontalOverflow = HorizontalWrapMode.Wrap; // best fit only shrinks wrapping text
            _header.resizeTextForBestFit = true;
            _header.resizeTextMinSize = 15;
            _header.resizeTextMaxSize = 24;
            UIFactory.Place(_header.rectTransform, UIFactory.TopLeft, SidePad + 4f, -18f,
                PanelWidth - SidePad * 2f - 8f, 30f);

            _terrainDot = UIFactory.HexBadge("TerrainDot", _root, Color.clear, 20f, 0f);
            UIFactory.Place(_terrainDot, UIFactory.TopLeft, SidePad + 4f, -52f, 20f, 18f);
            _terrainDotImage = _terrainDot.GetComponent<Image>();

            _terrainLabel = UIFactory.Label("Terrain", _root, Loc.T("build.clickToInspect"), 15,
                UIFactory.MutedColor);
            // "Terreno livre. Aceita qualquer construção." runs to the panel edge in PT.
            _terrainLabel.horizontalOverflow = HorizontalWrapMode.Wrap; // best fit only shrinks wrapping text
            _terrainLabel.resizeTextForBestFit = true;
            _terrainLabel.resizeTextMinSize = 11;
            _terrainLabel.resizeTextMaxSize = 15;
            UIFactory.Place(_terrainLabel.rectTransform, UIFactory.TopLeft, SidePad + 32f, -50f,
                PanelWidth - SidePad * 2f - 36f, 22f);

            _sectionLabel = UIFactory.Label("Section", _root, "", 13, UIFactory.FaintColor);
            UIFactory.Place(_sectionLabel.rectTransform, UIFactory.TopLeft, SidePad + 4f, -76f,
                PanelWidth - SidePad * 2f, 18f);

            BuildList();
        }

        /// <summary>
        /// The rows live in a masked viewport, so a long list scrolls (wheel or drag)
        /// instead of growing the card down over the board.
        /// </summary>
        private void BuildList()
        {
            _viewport = UIFactory.NewRect("List", _root);
            UIFactory.Place(_viewport, UIFactory.TopLeft, SidePad, -HeaderHeight, ListWidth, 0f);
            _viewport.gameObject.AddComponent<RectMask2D>();
            // A transparent graphic, so the wheel and drags land between rows too.
            _viewport.gameObject.AddComponent<Image>().color = Color.clear;

            _content = UIFactory.NewRect("Content", _viewport);
            UIFactory.Place(_content, UIFactory.TopLeft, 0f, 0f, ListWidth, 0f);

            RectTransform track = UIFactory.Chip("Scrollbar", _root, UIFactory.PaperShade);
            UIFactory.Place(track, UIFactory.TopRight, -SidePad, -HeaderHeight, ScrollbarWidth, 0f);
            RectTransform handle = UIFactory.Chip("Handle", track, UIFactory.FaintColor);
            UIFactory.Fill(handle);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle;
            bar.targetGraphic = handle.GetComponent<Image>();
            bar.direction = Scrollbar.Direction.BottomToTop;

            var scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = _viewport;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
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
            float y = -index * (RowHeight + RowGap);
            const float width = ListWidth;

            Color background = blockedReason == null ? UIFactory.PaperDeep : UIFactory.PaperShade;
            Button button = UIFactory.Button(title, _content, background);
            RectTransform rect = button.GetComponent<RectTransform>();
            UIFactory.Place(rect, UIFactory.TopLeft, 0f, y, width, RowHeight);

            button.interactable = interactive;
            if (interactive) button.onClick.AddListener(() => onClick());

            RectTransform badge = UIFactory.HexBadge("Badge", rect,
                blockedReason == null ? chip : UIFactory.FaintColor, 34f, 3f);
            UIFactory.Place(badge, UIFactory.TopLeft, 9f, -9f, 34f, 30f);

            Color titleColor = blockedReason == null ? UIFactory.TextColor : UIFactory.FaintColor;

            Text titleText = UIFactory.Label("Title", rect, title, 17, titleColor, TextAnchor.UpperLeft,
                FontStyle.Bold);
            // "Extração de Petróleo" is long in a narrow row; shrink rather than clip.
            titleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 12;
            titleText.resizeTextMaxSize = 17;
            UIFactory.Place(titleText.rectTransform, UIFactory.TopLeft, 52f, -4f, width - 52f - 84f, 23f);

            if (!string.IsNullOrEmpty(trailing))
            {
                Color costColor = blockedReason == null ? UIFactory.CostColor : UIFactory.FaintColor;
                Text costText = UIFactory.Label("Cost", rect, trailing, 17, costColor,
                    TextAnchor.UpperRight, FontStyle.Bold);
                UIFactory.Place(costText.rectTransform, UIFactory.TopRight, -10f, -4f, 78f, 23f);
            }

            Text detailText = UIFactory.Label("Detail", rect,
                blockedReason ?? detail, 13,
                blockedReason == null ? UIFactory.MutedColor : UIFactory.WarnColor);
            // Effect lists with a terrain bonus run long; shrink rather than spill past the row.
            detailText.resizeTextForBestFit = true;
            detailText.resizeTextMinSize = 10;
            detailText.resizeTextMaxSize = 13;
            detailText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.Place(detailText.rectTransform, UIFactory.TopLeft, 52f, -27f, width - 60f, 18f);

            _rows.Add(button.gameObject);
        }

        private void Resize(int rowCount)
        {
            float listHeight = rowCount == 0 ? 0f : rowCount * (RowHeight + RowGap) - RowGap;
            float visible = Mathf.Min(listHeight, MaxListHeight);

            _content.sizeDelta = new Vector2(ListWidth, listHeight);
            _content.anchoredPosition = Vector2.zero; // a new selection starts at the top
            _viewport.sizeDelta = new Vector2(ListWidth, visible);
            ((RectTransform)_viewport.GetComponent<ScrollRect>().verticalScrollbar.transform).sizeDelta =
                new Vector2(ScrollbarWidth, visible);

            _root.sizeDelta = new Vector2(PanelWidth,
                rowCount == 0 ? HeaderHeight - 22f : HeaderHeight + visible + BottomPad);
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
