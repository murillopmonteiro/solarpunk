using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Helpers for assembling the HUD from code.
    ///
    /// Everything is positioned explicitly against a corner anchor rather than
    /// with LayoutGroups: auto-layout silently inflated the first version of the
    /// bar to four times its intended height, and exact placement is easier to
    /// reason about for a fixed HUD.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);

        public static readonly Color PanelColor = new Color(0.08f, 0.10f, 0.12f, 0.94f);
        public static readonly Color RowColor = new Color(0.14f, 0.17f, 0.19f, 1f);
        public static readonly Color RowDisabledColor = new Color(0.11f, 0.12f, 0.13f, 1f);
        public static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.09f);
        public static readonly Color TextColor = new Color(0.93f, 0.96f, 0.94f);
        public static readonly Color MutedColor = new Color(0.56f, 0.63f, 0.62f);
        public static readonly Color FaintColor = new Color(0.40f, 0.45f, 0.45f);
        public static readonly Color AccentColor = new Color(0.44f, 0.83f, 0.55f);
        public static readonly Color WarnColor = new Color(0.93f, 0.56f, 0.42f);

        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Pin a rect to one corner of its parent at an exact size and offset.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, float x, float y, float w, float h)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        /// <summary>Full-width strip pinned to the top of its parent.</summary>
        public static RectTransform PlaceTopStrip(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
            return rect;
        }

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            RectTransform rect = NewRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        public static Text Label(string name, Transform parent, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(string name, Transform parent, Color background)
        {
            RectTransform rect = Panel(name, parent, background);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = Color.white; // disabled rows are styled explicitly instead
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            return button;
        }

        /// <summary>Thin horizontal rule, positioned from the parent's top-left.</summary>
        public static void Divider(Transform parent, float x, float y, float width)
        {
            RectTransform rect = Panel("Divider", parent, DividerColor);
            Place(rect, TopLeft, x, y, width, 1f);
            rect.GetComponent<Image>().raycastTarget = false;
        }

        /// <summary>Small colour swatch used for terrain keys and tile-type chips.</summary>
        public static void Swatch(Transform parent, float x, float y, float w, float h, Color color)
        {
            RectTransform rect = Panel("Swatch", parent, color);
            Place(rect, TopLeft, x, y, w, h);
            rect.GetComponent<Image>().raycastTarget = false;
        }
    }
}
