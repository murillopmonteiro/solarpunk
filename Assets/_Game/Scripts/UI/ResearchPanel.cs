using System.Collections.Generic;
using Solarpunk.Core;
using Solarpunk.Research;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Full-screen research overlay: a pannable, zoomable map of the tree on the
    /// left and the selected node's details on the right.
    ///
    /// The map is laid out from each node's position, the same grid the research
    /// tree artifact draws, so the shape of the tree is data. Clicking a card
    /// selects it; the detail panel's button is what spends the points, so a
    /// misclick on a crowded map never costs anything.
    /// </summary>
    public class ResearchPanel : MonoBehaviour
    {
        private const float Margin = 36f;
        private const float HeaderHeight = 112f;
        private const float DetailWidth = 440f;
        private const float Gap = 18f;

        // Map geometry, in canvas units at zoom 1.
        private const float StrideX = 236f;
        private const float StrideY = 128f;
        private const float CardWidth = 200f;
        private const float CardHeight = 92f;
        private const float MapPad = 90f;
        private const float LineThickness = 4f;

        private const float MinZoom = 0.4f;
        private const float MaxZoom = 1.4f;
        private const float StartZoom = 0.8f;

        // Branch labels in the four quadrants, in grid units like the nodes.
        private static readonly (string key, Vector2 at)[] Quadrants =
        {
            ("research.branchCity", new Vector2(-5.2f, -1.15f)),
            ("research.branchScience", new Vector2(-1.2f, -7.3f)),
            ("research.branchEnergy", new Vector2(5.15f, 2.6f)),
            ("research.branchIndustry", new Vector2(3.4f, 5.0f)),
        };

        private static readonly Color LineIdle = new Color(0.71f, 0.66f, 0.53f);
        private static readonly Color MapColor = new Color(0.85f, 0.82f, 0.71f);
        private static readonly Color LockedFace = new Color(0.49f, 0.55f, 0.50f);
        private static readonly Color LockedSlab = new Color(0.29f, 0.35f, 0.31f);
        private static readonly Color LockedText = new Color(0.89f, 0.91f, 0.87f);
        private static readonly Color LockedSubtext = new Color(0.78f, 0.82f, 0.77f);
        private static readonly Color QuadrantColor = new Color(0.13f, 0.22f, 0.17f, 0.10f);

        private ResearchManager _research;

        private RectTransform _root;
        private RectTransform _viewport;
        private RectTransform _content;
        private RectTransform _detail;
        private Text _pointsLabel;
        private Text _summaryLabel;

        private Vector2 _min;
        private bool _centred;
        private ResearchNode _selected;

        private readonly List<GameObject> _mapItems = new();
        private readonly List<GameObject> _detailItems = new();

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;

        public void Build(Transform canvas, ResearchManager research)
        {
            // A scene generated before the research system has nothing to wire here.
            // Leaving _root null makes every other entry point a no-op, so the rest of
            // the HUD still comes up and the console says what to do about it.
            if (research == null)
            {
                Debug.LogError("[HUD] No ResearchManager in the scene, so the research panel is off. " +
                               "This scene predates the research system — rebuild it with " +
                               "Solarpunk → Build Initial Scene.");
                return;
            }

            _research = research;

            // The backdrop is a full-screen raycast blocker as much as a dimmer:
            // without it, clicks land on the hexes behind the overlay.
            _root = UIFactory.NewRect("ResearchOverlay", canvas);
            UIFactory.Fill(_root);
            _root.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.11f, 0.7f);

            RectTransform card = UIFactory.Panel("Panel", _root);
            Stretch(card, Margin, Margin, Margin, Margin);

            BuildHeader(card);
            BuildMap(card);

            _detail = UIFactory.Card("Detail", card, UIFactory.PaperDeep);
            _detail.anchorMin = new Vector2(1f, 0f);
            _detail.anchorMax = new Vector2(1f, 1f);
            _detail.pivot = new Vector2(1f, 1f);
            _detail.offsetMin = new Vector2(-DetailWidth - 24f, 24f);
            _detail.offsetMax = new Vector2(-24f, -HeaderHeight);

            _root.gameObject.SetActive(false);
        }

        private void BuildHeader(RectTransform card)
        {
            Text title = UIFactory.Label("Title", card, Loc.T("research.title"), 36, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, UIFactory.TopLeft, 30f, -22f, 500f, 44f);

            _summaryLabel = UIFactory.Label("Summary", card, "", 16, UIFactory.MutedColor);
            UIFactory.Place(_summaryLabel.rectTransform, UIFactory.TopLeft, 32f, -70f, 900f, 22f);

            RectTransform pointsPill = UIFactory.Chip("Points", card, UIFactory.PaperDeep);
            UIFactory.Place(pointsPill, UIFactory.TopRight, -112f, -26f, 300f, 58f);

            RectTransform badge = UIFactory.HexBadge("Badge", pointsPill, UIFactory.Sun, 34f, 3f);
            UIFactory.Place(badge, UIFactory.TopLeft, 14f, -14f, 34f, 30f);

            _pointsLabel = UIFactory.Label("PointsLabel", pointsPill, "0", 22, UIFactory.TextColor,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(_pointsLabel.rectTransform, UIFactory.TopLeft, 60f, -4f, 230f, 50f);

            Button close = UIFactory.Button("Close", card, UIFactory.PaperDeep, true);
            UIFactory.Place(close.GetComponent<RectTransform>(), UIFactory.TopRight, -30f, -26f, 58f, 58f);
            close.onClick.AddListener(() => Show(false));

            Text closeLabel = UIFactory.Label("Label", close.transform, "X", 24, UIFactory.MutedColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(closeLabel.rectTransform);
        }

        /// <summary>
        /// A ScrollRect for the drag-to-pan, a RectMask2D so cards stop at the edge,
        /// and zoom handled in <see cref="Update"/> by scaling the content.
        /// </summary>
        private void BuildMap(RectTransform card)
        {
            _viewport = UIFactory.Card("Map", card, MapColor);
            Stretch(_viewport, 24f, DetailWidth + 24f + Gap, HeaderHeight, 24f);
            _viewport.gameObject.AddComponent<RectMask2D>();

            Text hint = UIFactory.Label("Hint", card, Loc.T("research.hint"), 14, UIFactory.FaintColor,
                TextAnchor.UpperRight);
            UIFactory.Place(hint.rectTransform, UIFactory.TopRight, -(DetailWidth + 24f + Gap), -88f, 700f, 20f);

            _content = UIFactory.NewRect("Content", _viewport);
            _content.anchorMin = UIFactory.TopLeft;
            _content.anchorMax = UIFactory.TopLeft;
            _content.pivot = UIFactory.TopLeft;

            MeasureTree(out _min, out Vector2 size);
            _content.sizeDelta = size;
            _content.localScale = Vector3.one * StartZoom;

            var scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = _viewport;
            scroll.content = _content;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            // The wheel zooms instead; letting it scroll as well fights the zoom.
            scroll.scrollSensitivity = 0f;
        }

        private void MeasureTree(out Vector2 min, out Vector2 size)
        {
            min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (ResearchNode node in _research.Tree)
            {
                if (node == null) continue;
                min = Vector2.Min(min, node.position);
                max = Vector2.Max(max, node.position);
            }

            foreach (var quadrant in Quadrants)
            {
                min = Vector2.Min(min, quadrant.at);
                max = Vector2.Max(max, quadrant.at);
            }

            size = new Vector2((max.x - min.x) * StrideX + CardWidth + MapPad * 2f,
                (max.y - min.y) * StrideY + CardHeight + MapPad * 2f);
        }

        /// <summary>Centre of a node's card in content space (top-left origin, y down negative).</summary>
        private Vector2 CardCentre(Vector2 grid) => new Vector2(
            MapPad + (grid.x - _min.x) * StrideX + CardWidth * 0.5f,
            -(MapPad + (grid.y - _min.y) * StrideY + CardHeight * 0.5f));

        private void Update()
        {
            if (!IsOpen) return;

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(wheel, 0f)) return;
            if (!RectTransformUtility.RectangleContainsScreenPoint(_viewport, Input.mousePosition, null)) return;

            // Zoom about the cursor: the point under it stays under it.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_content, Input.mousePosition, null,
                out Vector2 before);
            float zoom = Mathf.Clamp(_content.localScale.x * (1f + wheel * 0.12f), MinZoom, MaxZoom);
            _content.localScale = Vector3.one * zoom;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_content, Input.mousePosition, null,
                out Vector2 after);
            _content.anchoredPosition += (after - before) * zoom;
        }

        public void Show(bool open)
        {
            if (_root == null) return;
            _root.gameObject.SetActive(open);
            if (!open) return;

            if (!_centred) CentreOn(Vector2.zero);
            Refresh();
        }

        public void Toggle() => Show(!IsOpen);

        /// <summary>Selects a node as if its card were clicked. Used by the screenshot harness too.</summary>
        public void Select(ResearchNode node)
        {
            _selected = node;
            Refresh();
        }

        private void CentreOn(Vector2 grid)
        {
            // Canvas sizes settle a frame after creation; until then fall back to the reference size.
            Rect view = _viewport.rect;
            Vector2 viewSize = view.width > 1f ? view.size : new Vector2(1920f - 2f * Margin - DetailWidth, 900f);

            float zoom = _content.localScale.x;
            Vector2 centre = CardCentre(grid);
            _content.anchoredPosition = new Vector2(viewSize.x * 0.5f - centre.x * zoom,
                -viewSize.y * 0.5f - centre.y * zoom);
            _centred = view.width > 1f;
        }

        public void Refresh()
        {
            if (_root == null || !IsOpen) return;

            _pointsLabel.text = Loc.T("research.points", Mathf.FloorToInt(_research.Points), _research.PerTurn);
            _summaryLabel.text = Loc.T("research.summary", _research.UnlockedCount, _research.Tree.Count);

            if (_selected == null)
            {
                foreach (ResearchNode node in _research.Tree)
                {
                    if (node != null && node.position == Vector2.zero) _selected = node;
                }
            }

            RebuildMap();
            RebuildDetail();
        }

        // ---------------------------------------------------------------- map

        private void RebuildMap()
        {
            Clear(_mapItems);

            foreach (var quadrant in Quadrants)
            {
                Text label = UIFactory.Label("Branch", _content, Loc.T(quadrant.key).ToUpper(), 56,
                    QuadrantColor, TextAnchor.UpperLeft, FontStyle.Bold);
                Vector2 at = CardCentre(quadrant.at);
                UIFactory.Place(label.rectTransform, UIFactory.TopLeft, at.x - CardWidth * 0.5f, at.y + 30f, 700f, 70f);
                _mapItems.Add(label.gameObject);
            }

            // Lines first so the cards paint over their ends. Only the first
            // prerequisite draws a line, as in the artifact; the rest are listed in
            // the detail panel, since lines across the whole map would hide the tree.
            foreach (ResearchNode node in _research.Tree)
            {
                if (node?.prerequisites == null || node.prerequisites.Length == 0) continue;
                ResearchNode parent = node.prerequisites[0];
                if (parent != null) DrawConnector(parent, node);
            }

            if (_selected != null) DrawSelectionRing(_selected);

            foreach (ResearchNode node in _research.Tree)
            {
                if (node != null) DrawCard(node);
            }
        }

        /// <summary>
        /// Elbow from parent to child, three axis-aligned rects. City and energy
        /// nodes run sideways, so their lines leave from the side; science and
        /// industry run up and down, so theirs leave from the top or bottom.
        /// </summary>
        private void DrawConnector(ResearchNode from, ResearchNode to)
        {
            Vector2 a = CardCentre(from.position);
            Vector2 b = CardCentre(to.position);
            Vector2 d = to.position - from.position;
            float sx = Mathf.Sign(d.x);
            float sy = Mathf.Sign(-d.y); // content y is flipped against grid y

            Color color = _research.IsUnlocked(from) && _research.IsUnlocked(to) ? UIFactory.LeafDark
                : _research.IsUnlocked(from) ? UIFactory.LeafLight
                : LineIdle;

            bool sideways = Mathf.Approximately(d.y, 0f) ||
                            (!Mathf.Approximately(d.x, 0f) && Mathf.Abs(d.x) >= 1f &&
                             (to.branch == "city" || to.branch == "energy" || Mathf.Abs(d.y) < 1f));

            if (Mathf.Approximately(d.x, 0f))
            {
                Vertical(a.x, a.y + sy * CardHeight * 0.5f, b.y - sy * CardHeight * 0.5f, color);
            }
            else if (sideways)
            {
                float startX = a.x + sx * CardWidth * 0.5f;
                float endX = b.x - sx * CardWidth * 0.5f;
                float busX = b.x - sx * StrideX * 0.5f;
                Horizontal(startX, busX, a.y, color);
                if (!Mathf.Approximately(a.y, b.y)) Vertical(busX, a.y, b.y, color);
                Horizontal(busX, endX, b.y, color);
            }
            else
            {
                float startY = a.y + sy * CardHeight * 0.5f;
                float endY = b.y - sy * CardHeight * 0.5f;
                float busY = b.y - sy * StrideY * 0.5f;
                Vertical(a.x, startY, busY, color);
                Horizontal(a.x, b.x, busY, color);
                Vertical(b.x, busY, endY, color);
            }
        }

        private void Horizontal(float xa, float xb, float y, Color color)
        {
            RectTransform rect = Line(color);
            UIFactory.Place(rect, UIFactory.TopLeft, Mathf.Min(xa, xb) - LineThickness * 0.5f,
                y + LineThickness * 0.5f, Mathf.Abs(xb - xa) + LineThickness, LineThickness);
        }

        private void Vertical(float x, float ya, float yb, Color color)
        {
            RectTransform rect = Line(color);
            UIFactory.Place(rect, UIFactory.TopLeft, x - LineThickness * 0.5f, Mathf.Max(ya, yb) + LineThickness * 0.5f,
                LineThickness, Mathf.Abs(yb - ya) + LineThickness);
        }

        private RectTransform Line(Color color)
        {
            RectTransform rect = UIFactory.NewRect("Link", _content);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            _mapItems.Add(rect.gameObject);
            return rect;
        }

        private void DrawSelectionRing(ResearchNode node)
        {
            Vector2 c = CardCentre(node.position);
            RectTransform ring = UIFactory.Card("Selected", _content, UIFactory.Sun);
            ring.GetComponent<Image>().raycastTarget = false;
            UIFactory.Place(ring, UIFactory.TopLeft, c.x - CardWidth * 0.5f - 5f, c.y + CardHeight * 0.5f + 5f,
                CardWidth + 10f, CardHeight + 16f);
            _mapItems.Add(ring.gameObject);
        }

        private void DrawCard(ResearchNode node)
        {
            bool unlocked = _research.IsUnlocked(node);
            bool available = _research.BlockReason(node) == null;
            bool reachable = unlocked || _research.PrerequisitesMet(node);

            Color face = unlocked ? UIFactory.LeafLight : reachable ? UIFactory.Paper : LockedFace;
            Color slab = unlocked ? UIFactory.LeafDark : reachable ? UIFactory.Slab : LockedSlab;

            Button button = UIFactory.Button($"Node_{node.name}", _content, face);
            UIFactory.AddSlab(button.targetGraphic, slab, 6f);
            RectTransform rect = button.GetComponent<RectTransform>();
            Vector2 c = CardCentre(node.position);
            UIFactory.Place(rect, UIFactory.TopLeft, c.x - CardWidth * 0.5f, c.y + CardHeight * 0.5f,
                CardWidth, CardHeight);
            button.onClick.AddListener(() => Select(node));

            // Locked cards stay readable: most of the tree is locked most of the time,
            // and a map of unreadable cards reads as broken rather than gated.
            Text title = UIFactory.Label("Title", rect, node.LocalizedName, 19,
                reachable ? UIFactory.TextColor : LockedText, TextAnchor.UpperLeft, FontStyle.Bold);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 13;
            title.resizeTextMaxSize = 19;
            // Leaves room on the right for the tradeoff dot.
            UIFactory.Place(title.rectTransform, UIFactory.TopLeft, 14f, -10f, CardWidth - 40f, 48f);

            string status = unlocked ? Loc.T("research.done")
                : node.cost <= 0 ? Loc.T("research.free")
                : Loc.T("research.cost", node.cost);
            if (node.IsPending) status += "  ·  " + Loc.T("research.pendingShort");

            Color statusColor = unlocked ? UIFactory.LeafText
                : !reachable ? LockedSubtext
                : available ? UIFactory.LeafDark
                : UIFactory.MutedColor;

            Text state = UIFactory.Label("State", rect, status, 14, statusColor, TextAnchor.UpperLeft);
            // "RESEARCHED  ·  not active yet" outruns the card at full size; shrink rather than clip.
            state.horizontalOverflow = HorizontalWrapMode.Wrap;
            state.resizeTextForBestFit = true;
            state.resizeTextMinSize = 10;
            state.resizeTextMaxSize = 14;
            UIFactory.Place(state.rectTransform, UIFactory.TopLeft, 14f, -64f, CardWidth - 28f, 20f);

            // A small mark for "this has a cost besides points", so tradeoffs are
            // visible from the map without opening each card.
            if (!string.IsNullOrEmpty(node.tradeoff))
            {
                RectTransform dot = UIFactory.Circle("Tradeoff", rect, UIFactory.WarnColor, 11f);
                UIFactory.Place(dot, UIFactory.TopRight, -11f, -11f, 11f, 11f);
            }

            _mapItems.Add(button.gameObject);
        }

        // ---------------------------------------------------------------- detail

        private void RebuildDetail()
        {
            Clear(_detailItems);
            if (_selected == null) return;

            ResearchNode node = _selected;
            const float pad = 26f;
            float width = DetailWidth - pad * 2f;
            float y = -pad;

            y = Paragraph(node.LocalizedName, 30, UIFactory.TextColor, FontStyle.Bold, y, width) - 10f;

            bool unlocked = _research.IsUnlocked(node);
            string blocked = _research.BlockReason(node);

            if (!string.IsNullOrEmpty(node.LocalizedDescription))
            {
                y = Heading(Loc.T("research.effect"), UIFactory.FaintColor, y, width);
                y = Paragraph(node.LocalizedDescription, 17, UIFactory.MutedColor, FontStyle.Normal, y, width) - 12f;
            }

            if (!string.IsNullOrEmpty(node.tradeoff))
            {
                y = Heading(Loc.T("research.tradeoff"), UIFactory.WarnColor, y, width);
                y = Paragraph(node.tradeoff, 17, UIFactory.MutedColor, FontStyle.Normal, y, width) - 12f;
            }

            if (node.IsPending)
            {
                y = Heading(Loc.T("research.pending"), UIFactory.CostColor, y, width);
                y = Paragraph(node.pendingNote, 15, UIFactory.FaintColor, FontStyle.Normal, y, width) - 12f;
            }

            if (node.prerequisites != null && node.prerequisites.Length > 0)
            {
                var names = new List<string>();
                foreach (ResearchNode prerequisite in node.prerequisites)
                {
                    if (prerequisite == null) continue;
                    // Rich text rather than a check glyph: the built-in font has no ✓.
                    names.Add(_research.IsUnlocked(prerequisite)
                        ? $"<color=#2F7A34>{prerequisite.LocalizedName}</color>"
                        : prerequisite.LocalizedName);
                }

                y = Heading(Loc.T(node.requireAny ? "research.requiresAny" : "research.requires"),
                    UIFactory.FaintColor, y, width);
                Paragraph(string.Join("\n", names), 17, UIFactory.MutedColor, FontStyle.Normal, y, width);
            }

            // The one control that spends points, pinned to the bottom of the panel.
            string label = unlocked ? Loc.T("research.done")
                : blocked == null ? Loc.T("research.unlock", node.cost)
                : blocked;

            Button action = UIFactory.Button("Unlock", _detail, blocked == null ? UIFactory.LeafLight : UIFactory.PaperShade);
            UIFactory.Place(action.GetComponent<RectTransform>(), UIFactory.BottomLeft, pad, pad + 6f, width, 66f);
            UIFactory.AddSlab(action.targetGraphic, blocked == null ? UIFactory.LeafDark : UIFactory.Slab, 6f);
            action.interactable = blocked == null;
            action.onClick.AddListener(() =>
            {
                if (_research.TryUnlock(node)) Refresh();
            });

            Text actionLabel = UIFactory.Label("Label", action.transform, label, 22,
                blocked == null ? UIFactory.LeafText : unlocked ? UIFactory.LeafDark : UIFactory.WarnColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            actionLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            actionLabel.resizeTextForBestFit = true;
            actionLabel.resizeTextMinSize = 13;
            actionLabel.resizeTextMaxSize = 22;
            UIFactory.Fill(actionLabel.rectTransform);
            actionLabel.rectTransform.offsetMin = new Vector2(14f, 0f);
            actionLabel.rectTransform.offsetMax = new Vector2(-14f, 0f);

            _detailItems.Add(action.gameObject);
        }

        private float Heading(string text, Color color, float y, float width)
        {
            Text label = UIFactory.Label("Heading", _detail, text, 14, color, TextAnchor.UpperLeft);
            UIFactory.Place(label.rectTransform, UIFactory.TopLeft, 26f, y, width, 20f);
            _detailItems.Add(label.gameObject);
            return y - 24f;
        }

        /// <summary>A wrapped block of text, sized to what it needs. Returns the y below it.</summary>
        private float Paragraph(string text, int size, Color color, FontStyle style, float y, float width)
        {
            Text label = UIFactory.Label("Text", _detail, text, size, color, TextAnchor.UpperLeft, style);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.1f;
            UIFactory.Place(label.rectTransform, UIFactory.TopLeft, 26f, y, width, 20f);

            float height = Mathf.Ceil(label.preferredHeight);
            label.rectTransform.sizeDelta = new Vector2(width, height);
            _detailItems.Add(label.gameObject);
            return y - height;
        }

        // ---------------------------------------------------------------- helpers

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Clear(List<GameObject> items)
        {
            foreach (GameObject item in items)
            {
                if (item != null) Destroy(item);
            }
            items.Clear();
        }
    }
}
