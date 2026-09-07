using Solarpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Top-left stack of resource capsules, one per global stat: a coloured icon
    /// badge, the current value, and the projected yearly change. Stacked rather
    /// than spread across a full-width bar so the board stays unobstructed.
    /// </summary>
    public class ResourceStack : MonoBehaviour
    {
        private const float PillWidth = 292f;
        private const float PillHeight = 50f;
        private const float PillGap = 7f;
        private const float Margin = 18f;

        private struct Pill
        {
            public Text Value;
            public Text Delta;
        }

        private Pill _energy, _money, _sustainability, _population, _happiness;

        public void Build(Transform canvas)
        {
            float y = -Margin;

            _energy = CreatePill(canvas, "Energy", "E", "ENERGY", UIFactory.EnergyColor, y);
            y -= PillHeight + PillGap;
            _money = CreatePill(canvas, "Money", "$", "MONEY", UIFactory.MoneyColor, y);
            y -= PillHeight + PillGap;
            _sustainability = CreatePill(canvas, "Sustainability", "S", "SUSTAIN", UIFactory.SustainColor, y);
            y -= PillHeight + PillGap;
            _population = CreatePill(canvas, "Population", "P", "PEOPLE", UIFactory.PopulationColor, y);
            y -= PillHeight + PillGap;
            _happiness = CreatePill(canvas, "Happiness", "♥", "HAPPY", UIFactory.HappinessColor, y);
        }

        private static Pill CreatePill(Transform canvas, string name, string glyph, string caption,
            Color accent, float y)
        {
            RectTransform pill = UIFactory.Capsule(name, canvas, UIFactory.Ink);
            UIFactory.Place(pill, UIFactory.TopLeft, Margin, y, PillWidth, PillHeight);

            RectTransform badge = UIFactory.Circle("Badge", pill, accent, 36f);
            UIFactory.Place(badge, UIFactory.TopLeft, 7f, -7f, 36f, 36f);

            Text glyphText = UIFactory.Label("Glyph", badge, glyph, 19, new Color(0.06f, 0.13f, 0.12f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(glyphText.rectTransform);

            Text captionText = UIFactory.Label("Caption", pill, caption, 10, UIFactory.FaintColor);
            UIFactory.Place(captionText.rectTransform, UIFactory.TopLeft, 52f, -6f, 120f, 13f);

            Text value = UIFactory.Label("Value", pill, "0", 21, UIFactory.TextColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(value.rectTransform, UIFactory.TopLeft, 51f, -18f, 120f, 26f);

            Text delta = UIFactory.Label("Delta", pill, "", 13, UIFactory.MutedColor,
                TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(delta.rectTransform, UIFactory.TopRight, -14f, -10f, 108f, 30f);

            return new Pill { Value = value, Delta = delta };
        }

        public void SetResources(ResourceVector current, ResourceVector perTurn)
        {
            Apply(_energy, current.energy, perTurn.energy, "");
            Apply(_money, current.money, perTurn.money, "$");
            Apply(_sustainability, current.sustainability, perTurn.sustainability, "");
            Apply(_population, current.population, perTurn.population, "");
            Apply(_happiness, current.happiness, perTurn.happiness, "");
        }

        private static void Apply(Pill pill, float value, float delta, string prefix)
        {
            if (pill.Value == null) return;

            pill.Value.text = $"{prefix}{value:0}";

            if (Mathf.Abs(delta) < 0.01f)
            {
                pill.Delta.text = "steady";
                pill.Delta.color = UIFactory.FaintColor;
                return;
            }

            pill.Delta.text = $"{StatFormat.Signed(delta)} /yr";
            pill.Delta.color = delta > 0f ? UIFactory.Leaf : UIFactory.WarnColor;
        }
    }
}
