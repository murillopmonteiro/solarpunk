using System;
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
    /// Look: the HUD is built from the same stuff as the models. Panels are cream
    /// plaster with a timber slab under them, the way every hex tile shows its
    /// side; corners are cut, not rounded, echoing the hex facets; badges are
    /// hexagons shaded in six facets, the way the low-poly tiles catch the light.
    ///
    /// Colour rule: Leaf marks the primary action, Sun marks research. The five
    /// resource colours are semantic, fixed, and never reused for anything that
    /// is not that resource.
    /// </summary>
    public static class UIFactory
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopRight = new Vector2(1f, 1f);
        public static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        public static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        // Surfaces: the cream walls and timber of the town models.
        public static readonly Color Paper = Hex(0xF6EEDB);
        public static readonly Color PaperDeep = Hex(0xEADFC4);
        public static readonly Color PaperShade = Hex(0xDDD0AF);
        public static readonly Color Slab = Hex(0xB49C6E);
        public static readonly Color Brass = Hex(0xB9813E);

        public static readonly Color TextColor = Hex(0x22382B);
        public static readonly Color MutedColor = Hex(0x5E6E5A);
        public static readonly Color FaintColor = Hex(0x8C977F);

        /// <summary>The primary action. Next Year, unlock, play again.</summary>
        public static readonly Color Leaf = Hex(0x5DB848);
        public static readonly Color LeafLight = Hex(0x7CCB60);
        public static readonly Color LeafDark = Hex(0x2F7A34);
        public static readonly Color LeafText = Hex(0x123A17);

        /// <summary>Research, and only research.</summary>
        public static readonly Color Sun = Hex(0xF2B33D);
        public static readonly Color SunDark = Hex(0xB9771F);
        public static readonly Color SunText = Hex(0x4A2E08);

        public static readonly Color WarnColor = Hex(0xC8553D);
        public static readonly Color CostColor = Hex(0x8A5A1E);

        // Fixed semantic palette for the five resources.
        public static readonly Color EnergyColor = Hex(0x3FB6E0);
        public static readonly Color MoneyColor = Hex(0xF2B33D);
        public static readonly Color SustainColor = Hex(0x5DB848);
        public static readonly Color PopulationColor = Hex(0x8C8FE6);
        public static readonly Color HappinessColor = Hex(0xF07A62);

        public enum Glyph { Bolt, Coin, Leaf, People, Heart }

        private const int CardCut = 16;
        private const int ChipCut = 11;

        private static Font _displayFont;
        private static Font _bodyFont;
        private static Sprite _cardSprite, _chipSprite, _trimSprite, _hexSprite, _circleSprite, _raySprite;
        private static readonly Sprite[] GlyphSprites = new Sprite[5];

        // An explicit == null, not ??=: a restart unloads assets, and only Unity's own
        // equality sees a destroyed object as null.
        private static T Cached<T>(ref T slot, Func<T> make) where T : UnityEngine.Object
        {
            if (slot == null) slot = make();
            return slot;
        }

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);

        private static Font Builtin() =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        /// <summary>Fredoka SemiBold: numbers, titles, buttons. Anything a label marks Bold.</summary>
        public static Font DisplayFont => Cached(ref _displayFont, () => Resources.Load<Font>("Fonts/Fredoka-SemiBold") ?? Builtin());

        /// <summary>Nunito Bold: captions and body text.</summary>
        public static Font BodyFont => Cached(ref _bodyFont, () => Resources.Load<Font>("Fonts/Nunito-Bold") ?? Builtin());

        // Shapes are generated at runtime rather than pulled from UI/Skin/*.psd:
        // Resources.GetBuiltinResource only resolves in the Editor, so those
        // sprites silently fail in a player build and every panel renders square.
        private static Sprite CardSprite => Cached(ref _cardSprite, () => Octagon(64, CardCut));
        private static Sprite ChipSprite => Cached(ref _chipSprite, () => Octagon(48, ChipCut));

        /// <summary>A thin cut-corner outline, inset inside a panel as its brass rule.</summary>
        private static Sprite TrimSprite => Cached(ref _trimSprite, () => Raster(64, 64, (x, y) =>
            InOctagon(x, y, 64, 64, 0f, CardCut - 6) && !InOctagon(x, y, 64, 64, 2f, CardCut - 6.8f) ? 1f : 0f,
            new Vector4(CardCut, CardCut, CardCut, CardCut)));

        private static Sprite CircleSprite => Cached(ref _circleSprite, () => Raster(64, 64, (x, y) =>
            (x - 32f) * (x - 32f) + (y - 32f) * (y - 32f) <= 31.5f * 31.5f ? 1f : 0f, Vector4.zero));

        /// <summary>
        /// Flat-top hexagon in six facets, lit from the top-left like the board.
        /// The texture is 64x56 so the hexagon keeps its true proportions.
        /// </summary>
        private static Sprite HexSprite => Cached(ref _hexSprite, () => Raster(64, 56, (x, y) =>
        {
            float dx = x - 32f, dy = y - 28f;
            const float r = 31.5f;
            if (Mathf.Abs(dy) > r * 0.866f || 1.732f * Mathf.Abs(dx) + Mathf.Abs(dy) > 1.732f * r) return 0f;
            float angle = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, 360f);
            return Facets[(int)(angle / 60f) % 6];
        }, Vector4.zero));

        // Brightness per facet, counter-clockwise from the upper-right one.
        private static readonly float[] Facets = { 0.9f, 1f, 0.95f, 0.82f, 0.7f, 0.76f };

        /// <summary>One ray of the year sun: a narrow triangle pointing up.</summary>
        private static Sprite RaySprite => Cached(ref _raySprite, () => Raster(32, 48, (x, y) =>
            Mathf.Abs(x - 16f) <= 16f * (1f - y / 48f) ? 1f : 0f, Vector4.zero));

        /// <summary>GLSL-style smoothstep. Mathf.SmoothStep interpolates between two values instead.</summary>
        public static float Smooth01(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static bool InOctagon(float x, float y, float w, float h, float inset, float cut)
        {
            float ex = Mathf.Min(x - inset, w - inset - x);
            float ey = Mathf.Min(y - inset, h - inset - y);
            return ex >= 0f && ey >= 0f && ex + ey >= cut;
        }

        private static Sprite Octagon(int size, int cut) =>
            Raster(size, size, (x, y) => InOctagon(x, y, size, size, 0f, cut) ? 1f : 0f,
                new Vector4(cut + 1, cut + 1, cut + 1, cut + 1));

        /// <summary>
        /// Rasterises a shape into a white sprite, tinted later by the Image colour.
        /// <paramref name="shade"/> returns 0 outside the shape and a brightness inside;
        /// 4x4 supersampling gives the edges their anti-aliasing.
        /// </summary>
        private static Sprite Raster(int w, int h, Func<float, float, float> shade, Vector4 border)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int hits = 0;
                    float brightness = 0f;
                    for (int sy = 0; sy < 4; sy++)
                    {
                        for (int sx = 0; sx < 4; sx++)
                        {
                            float s = shade(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f);
                            if (s <= 0f) continue;
                            hits++;
                            brightness += s;
                        }
                    }

                    byte g = hits == 0 ? (byte)255 : (byte)Mathf.RoundToInt(brightness / hits * 255f);
                    pixels[y * w + x] = new Color32(g, g, g, (byte)(hits * 255 / 16));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
        }

        // ---------------------------------------------------------------- glyphs

        /// <summary>Icons drawn on a 24-unit grid, y down, the way the mockup's SVGs were.</summary>
        private static Sprite GlyphSprite(Glyph glyph)
        {
            int i = (int)glyph;
            if (GlyphSprites[i] != null) return GlyphSprites[i];
            // Coin: a disc with a ring cut into it. Leaf: two overlapping discs, a vein
            // along the diagonal. People: two heads over two shoulders. Heart: two
            // lobes over a point.

            Func<float, float, bool> inside = glyph switch
            {
                Glyph.Bolt => (u, v) => InPolygon(u, v, Bolt),
                Glyph.Coin => (u, v) => Disk(u, v, 12f, 12f, 9.5f) && !(Disk(u, v, 12f, 12f, 7f) && !Disk(u, v, 12f, 12f, 5.4f)),
                Glyph.Leaf => (u, v) => Disk(u, v, 7.5f, 7.5f, 11.5f) && Disk(u, v, 16.5f, 16.5f, 11.5f)
                                        && !(Mathf.Abs(u - (24f - v)) < 0.9f && u > 7f && u < 18f),
                Glyph.People => (u, v) => Disk(u, v, 9f, 7f, 3.4f) || Disk(u, v, 16.5f, 8.5f, 2.8f)
                                          || (v <= 21f && Ellipse(u, v, 9f, 21f, 6.6f, 7.5f))
                                          || (v <= 21f && u > 14f && Ellipse(u, v, 17f, 21f, 5f, 6.6f)),
                _ => (u, v) => Disk(u, v, 7.8f, 9.4f, 4.9f) || Disk(u, v, 16.2f, 9.4f, 4.9f)
                               || InPolygon(u, v, HeartPoint)
            };

            GlyphSprites[i] = Raster(64, 64, (x, y) => inside(x / 64f * 24f, (1f - y / 64f) * 24f) ? 1f : 0f,
                Vector4.zero);
            return GlyphSprites[i];
        }

        private static readonly Vector2[] Bolt =
            { new(13.5f, 1.5f), new(3.5f, 14f), new(10f, 14f), new(9f, 22.5f), new(19f, 9.5f), new(12.5f, 9.5f) };

        private static readonly Vector2[] HeartPoint = { new(3.2f, 11.3f), new(20.8f, 11.3f), new(12f, 21.5f) };

        private static bool Disk(float u, float v, float cx, float cy, float r) =>
            (u - cx) * (u - cx) + (v - cy) * (v - cy) <= r * r;

        private static bool Ellipse(float u, float v, float cx, float cy, float rx, float ry) =>
            (u - cx) * (u - cx) / (rx * rx) + (v - cy) * (v - cy) / (ry * ry) <= 1f;

        private static bool InPolygon(float u, float v, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((poly[i].y > v) != (poly[j].y > v) &&
                    u < (poly[j].x - poly[i].x) * (v - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                    inside = !inside;
            }
            return inside;
        }

        // ---------------------------------------------------------------- building blocks

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

        private static RectTransform Shaped(string name, Transform parent, Color color, Sprite sprite)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            return rect;
        }

        /// <summary>
        /// The tile-side slab: a copy of the shape dropped straight down in a darker
        /// colour. A Shadow effect rather than a sibling, so it follows any resize.
        /// </summary>
        public static void AddSlab(Component target, Color color, float depth)
        {
            var slab = target.gameObject.AddComponent<Shadow>();
            slab.effectColor = color;
            slab.effectDistance = new Vector2(0f, -depth);
            slab.useGraphicAlpha = true;
        }

        /// <summary>Cut-corner surface. Rows, cards, map nodes.</summary>
        public static RectTransform Card(string name, Transform parent, Color color) =>
            Shaped(name, parent, color, CardSprite);

        /// <summary>Smaller cut. Pills, small buttons, delta tags.</summary>
        public static RectTransform Chip(string name, Transform parent, Color color) =>
            Shaped(name, parent, color, ChipSprite);

        /// <summary>A top-level HUD panel: cream face, timber slab beneath, brass rule inside.</summary>
        public static RectTransform Panel(string name, Transform parent)
        {
            RectTransform rect = Card(name, parent, Paper);
            AddSlab(rect, Slab, 7f);

            RectTransform trim = Shaped("Trim", rect, new Color(Brass.r, Brass.g, Brass.b, 0.38f), TrimSprite);
            Fill(trim);
            trim.offsetMin = new Vector2(6f, 6f);
            trim.offsetMax = new Vector2(-6f, -6f);
            trim.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        /// <summary>Faceted hexagon badge, tinted a little lighter so the top facets read as lit.</summary>
        public static RectTransform HexBadge(string name, Transform parent, Color color, float width, float slab = 4f)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = HexSprite;
            image.color = Color.Lerp(color, Color.white, 0.12f);
            image.raycastTarget = false;
            rect.sizeDelta = new Vector2(width, width * 0.875f);
            if (slab > 0f) AddSlab(image, Color.Lerp(color, Color.black, 0.45f), slab);
            return rect;
        }

        public static RectTransform Icon(string name, Transform parent, Glyph glyph, Color color, float size)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = GlyphSprite(glyph);
            image.color = color;
            image.raycastTarget = false;
            rect.sizeDelta = new Vector2(size, size);
            return rect;
        }

        /// <summary>A sun ray, pivoted on its base so it can be spun around a centre.</summary>
        public static Image Ray(string name, Transform parent, Color color, float width, float length)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RaySprite;
            image.color = color;
            image.raycastTarget = false;
            rect.sizeDelta = new Vector2(width, length);
            return image;
        }

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

        /// <summary>Bold labels set in Fredoka, the rest in Nunito; both files carry their own weight.</summary>
        public static Text Label(string name, Transform parent, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            RectTransform rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = style == FontStyle.Bold ? DisplayFont : BodyFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = FontStyle.Normal;
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

        public static Button Button(string name, Transform parent, Color background, bool chip = false)
        {
            RectTransform rect = chip ? Chip(name, parent, background) : Card(name, parent, background);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.disabledColor = Color.white; // disabled rows are styled explicitly instead
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            return button;
        }

        /// <summary>
        /// Chunky game button: a slab under the face gives it the same physical
        /// depth as the hex tiles.
        /// </summary>
        public static Button ChunkyButton(string name, Transform parent, Color face, Color slab,
            Vector2 anchor, float x, float y, float w, float h, float depth = 7f)
        {
            Button button = Button(name, parent, face);
            Place(button.GetComponent<RectTransform>(), anchor, x, y, w, h);
            AddSlab(button.targetGraphic, slab, depth);
            return button;
        }
    }
}
