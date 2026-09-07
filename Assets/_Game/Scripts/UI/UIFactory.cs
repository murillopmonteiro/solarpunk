using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Helpers for assembling the HUD from code.
    ///
    /// Positioning is explicit against a corner anchor rather than LayoutGroups:
    /// auto-layout silently inflated the first version of the bar to four times
    /// its intended height, and exact placement is easier to reason about here.
    ///
    /// Shape rule (applied everywhere, no exceptions): capsules for stats and
    /// buttons, 16-ish radius for panels and cards, circles for icon badges.
    ///
    /// Colour rule: one interactive accent (Leaf) marks everything actionable.
    /// The five resource colours are semantic, fixed, and never reused for
    /// anything that is not that resource.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        // Surfaces: deep blue-green ink, warm enough to sit on a sunny sky.
        public static readonly Color Ink = new Color(0.09f, 0.16f, 0.18f, 0.95f);
        public static readonly Color InkSolid = new Color(0.09f, 0.16f, 0.18f, 1f);
        public static readonly Color Surface = new Color(0.15f, 0.23f, 0.25f, 1f);
        public static readonly Color SurfaceMuted = new Color(0.12f, 0.17f, 0.19f, 1f);
        public static readonly Color Shadow = new Color(0.04f, 0.09f, 0.10f, 0.55f);

        public static readonly Color TextColor = new Color(0.96f, 0.98f, 0.96f);
        public static readonly Color MutedColor = new Color(0.62f, 0.72f, 0.71f);
        public static readonly Color FaintColor = new Color(0.44f, 0.53f, 0.53f);

        /// <summary>The single interactive accent. Anything actionable wears it.</summary>
        public static readonly Color Leaf = new Color(0.37f, 0.82f, 0.41f);
        public static readonly Color LeafDark = new Color(0.20f, 0.55f, 0.26f);
        public static readonly Color WarnColor = new Color(0.98f, 0.54f, 0.42f);

        // Fixed semantic palette for the five resources.
        public static readonly Color EnergyColor = new Color(0.30f, 0.79f, 0.94f);
        public static readonly Color MoneyColor = new Color(1.00f, 0.82f, 0.40f);
        public static readonly Color SustainColor = new Color(0.37f, 0.82f, 0.41f);
        public static readonly Color PopulationColor = new Color(0.56f, 0.62f, 0.96f);
        public static readonly Color HappinessColor = new Color(1.00f, 0.54f, 0.48f);

        private const int CardRadius = 14;
        private const int CapsuleRadius = 24;

        private static Font _font;
        private static Sprite _cardSprite;
        private static Sprite _capsuleSprite;
        private static Sprite _circleSprite;

        public static Font Font
        {
            get
            {
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        // Shapes are generated at runtime rather than pulled from UI/Skin/*.psd:
        // Resources.GetBuiltinResource only resolves in the Editor, so those
        // sprites silently fail in a player build and every panel renders square.
        private static Sprite CardSprite
        {
            get
            {
                if (_cardSprite == null) _cardSprite = BuildRoundedSprite(48, CardRadius);
                return _cardSprite;
            }
        }

        private static Sprite CapsuleSprite
        {
            get
            {
                if (_capsuleSprite == null) _capsuleSprite = BuildRoundedSprite(64, CapsuleRadius);
                return _capsuleSprite;
            }
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (_circleSprite == null) _circleSprite = BuildCircleSprite(64);
                return _circleSprite;
            }
        }

        /// <summary>GLSL-style smoothstep. Mathf.SmoothStep interpolates between two values instead.</summary>
        private static float Smooth01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static Texture2D NewTexture(int size)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static Sprite BuildRoundedSprite(int size, int radius)
        {
            Texture2D texture = NewTexture(size);
            var pixels = new Color32[size * size];
            float inner = size - radius;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    // Distance to the inset rectangle: inside its bounds this is 0,
                    // and near a corner it becomes the distance to that corner's centre.
                    float dx = px - Mathf.Clamp(px, radius, inner);
                    float dy = py - Mathf.Clamp(py, radius, inner);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 1f - Smooth01(radius - 1f, radius + 0.5f, distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite BuildCircleSprite(int size)
        {
            Texture2D texture = NewTexture(size);
            var pixels = new Color32[size * size];
            float centre = size * 0.5f;
            float radius = centre - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - centre;
                    float dy = y + 0.5f - centre;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = 1f - Smooth01(radius - 1f, radius + 0.5f, distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
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

        private static RectTransform Rounded(string name, Transform parent, Color color, Sprite sprite)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            return rect;
        }

        /// <summary>Soft-cornered surface. Panels, cards, list rows.</summary>
        public static RectTransform Card(string name, Transform parent, Color color) =>
            Rounded(name, parent, color, CardSprite);

        /// <summary>Fully rounded surface. Stat readouts and primary actions.</summary>
        public static RectTransform Capsule(string name, Transform parent, Color color) =>
            Rounded(name, parent, color, CapsuleSprite);

        public static RectTransform Circle(string name, Transform parent, Color color, float diameter)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = CircleSprite;
            image.color = color;
            image.raycastTarget = false;
            rect.sizeDelta = new Vector2(diameter, diameter);
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

        /// <summary>Fills the parent, for centring a label inside a button.</summary>
        public static void Fill(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Button Button(string name, Transform parent, Color background, bool capsule = false)
        {
            RectTransform rect = capsule
                ? Capsule(name, parent, background)
                : Card(name, parent, background);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = Color.white; // disabled rows are styled explicitly instead
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            return button;
        }

        /// <summary>
        /// Chunky game button: a solid slab dropped behind the face gives it
        /// physical depth, the way mobile builders render their corner actions.
        /// </summary>
        public static Button ChunkyButton(string name, Transform parent, Color face, Color shadow,
            Vector2 anchor, float x, float y, float w, float h, float depth = 6f)
        {
            RectTransform shadowRect = Capsule($"{name}Shadow", parent, shadow);
            Place(shadowRect, anchor, x, y - depth, w, h);
            shadowRect.GetComponent<Image>().raycastTarget = false;

            Button button = Button(name, parent, face, true);
            Place(button.GetComponent<RectTransform>(), anchor, x, y, w, h);
            return button;
        }
    }
}
