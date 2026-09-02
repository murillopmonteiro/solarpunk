using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>Small helpers for assembling the placeholder HUD from code.</summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color(0.07f, 0.10f, 0.11f, 0.90f);
        public static readonly Color PanelSoftColor = new Color(0.12f, 0.16f, 0.17f, 0.95f);
        public static readonly Color TextColor = new Color(0.92f, 0.95f, 0.92f);
        public static readonly Color MutedColor = new Color(0.60f, 0.67f, 0.64f);
        public static readonly Color AccentColor = new Color(0.45f, 0.82f, 0.55f);
        public static readonly Color WarnColor = new Color(0.92f, 0.55f, 0.42f);

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

        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            RectTransform rect = NewRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        /// <summary>Stretch a rect to its parent with the given pixel insets.</summary>
        public static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static Text Label(string name, Transform parent, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            colors.fadeDuration = 0.06f;
            button.colors = colors;

            return button;
        }

        public static VerticalLayoutGroup VerticalLayout(RectTransform rect, int spacing, RectOffset padding)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup HorizontalLayout(RectTransform rect, int spacing, RectOffset padding)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        public static LayoutElement FixedHeight(RectTransform rect, float height)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            return element;
        }
    }
}
