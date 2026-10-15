using Solarpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// The resource ribbon across the top: one section per global stat, each a
    /// faceted badge, the current value, and a tag with the projected yearly change.
    /// One panel rather than five so it reads as a single instrument.
    /// </summary>
    public class ResourceStack : MonoBehaviour
    {
        public const float Left = 300f;
        public const float Top = 28f;
        public const float Height = 120f;
        private const float SectionWidth = 226f;
        private const float SidePad = 12f;

        private static readonly Color DividerColor = new Color(0.725f, 0.506f, 0.243f, 0.28f);
        private static readonly Color UpTag = new Color(0.867f, 0.937f, 0.788f);
        private static readonly Color DownTag = new Color(0.973f, 0.851f, 0.812f);
        private static readonly Color DownText = new Color(0.706f, 0.263f, 0.173f);

        private struct Section
        {
            public Text Value;
            public Text Delta;
            public Image Tag;
        }

        private Section _energy, _money, _sustainability, _population, _happiness;

        public void Build(Transform canvas)
        {
            RectTransform ribbon = UIFactory.Panel("Resources", canvas);
            UIFactory.Place(ribbon, UIFactory.TopLeft, Left, -Top, SectionWidth * 5f + SidePad * 2f, Height);

            _energy = CreateSection(ribbon, 0, UIFactory.Glyph.Bolt, Loc.T("res.energy"), UIFactory.EnergyColor);
            _money = CreateSection(ribbon, 1, UIFactory.Glyph.Coin, Loc.T("res.money"), UIFactory.MoneyColor);
            _sustainability = CreateSection(ribbon, 2, UIFactory.Glyph.Leaf, Loc.T("res.sustain"), UIFactory.SustainColor);
            _population = CreateSection(ribbon, 3, UIFactory.Glyph.People, Loc.T("res.people"), UIFactory.PopulationColor);
            _happiness = CreateSection(ribbon, 4, UIFactory.Glyph.Heart, Loc.T("res.happy"), UIFactory.HappinessColor);
        }

        private static Section CreateSection(Transform ribbon, int index, UIFactory.Glyph glyph, string caption,
            Color accent)
        {
            float x = SidePad + index * SectionWidth;

            if (index > 0)
            {
                RectTransform divider = UIFactory.NewRect("Divider", ribbon);
                divider.gameObject.AddComponent<Image>().color = DividerColor;
                UIFactory.Place(divider, UIFactory.TopLeft, x, -26f, 2f, Height - 52f);
            }

            RectTransform badge = UIFactory.HexBadge($"{caption}Badge", ribbon, accent, 62f);
            UIFactory.Place(badge, UIFactory.TopLeft, x + 14f, -30f, 62f, 54f);

            RectTransform icon = UIFactory.Icon("Icon", badge, glyph, Color.white, 30f);
            UIFactory.Place(icon, new Vector2(0.5f, 0.5f), 0f, 0f, 30f, 30f);

            Text captionText = UIFactory.Label("Caption", ribbon, caption.ToUpper(), 14, UIFactory.FaintColor);
            UIFactory.Place(captionText.rectTransform, UIFactory.TopLeft, x + 90f, -18f, 130f, 18f);

            Text value = UIFactory.Label("Value", ribbon, "0", 38, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(value.rectTransform, UIFactory.TopLeft, x + 89f, -34f, 136f, 46f);

            RectTransform tag = UIFactory.Chip("Tag", ribbon, UpTag);
            UIFactory.Place(tag, UIFactory.TopLeft, x + 90f, -80f, 112f, 26f);

            Text delta = UIFactory.Label("Delta", tag, "", 16, UIFactory.LeafDark, TextAnchor.MiddleCenter);
            UIFactory.Fill(delta.rectTransform);

            return new Section { Value = value, Delta = delta, Tag = tag.GetComponent<Image>() };
        }

        public void SetResources(ResourceVector current, ResourceVector perTurn, float energyCap)
        {
            Apply(_energy, current.energy, perTurn.energy, "");
            // The cap rides small after the stock, "42/54", so three digits of each still fit.
            if (_energy.Value != null) _energy.Value.text += $"<size=22>/{energyCap:0}</size>";
            Apply(_money, current.money, perTurn.money, "$");
            Apply(_sustainability, current.sustainability, perTurn.sustainability, "");
            Apply(_population, current.population, perTurn.population, "");
            Apply(_happiness, current.happiness, perTurn.happiness, "");
        }

        private static void Apply(Section section, float value, float delta, string prefix)
        {
            if (section.Value == null) return;

            // Debt reads "-$150" in red, not "$-150".
            section.Value.text = value < 0f ? $"-{prefix}{-value:0}" : $"{prefix}{value:0}";
            section.Value.color = value < 0f ? UIFactory.WarnColor : UIFactory.TextColor;

            if (Mathf.Abs(delta) < 0.01f)
            {
                section.Delta.text = Loc.T("res.steady");
                section.Delta.color = UIFactory.MutedColor;
                section.Tag.color = UIFactory.PaperDeep;
                return;
            }

            section.Delta.text = Loc.T("res.perYear", StatFormat.Signed(delta));
            section.Delta.color = delta > 0f ? UIFactory.LeafDark : DownText;
            section.Tag.color = delta > 0f ? UpTag : DownTag;
        }
    }
}
