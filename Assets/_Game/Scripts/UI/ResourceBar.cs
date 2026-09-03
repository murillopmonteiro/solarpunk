using Solarpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Compact top strip: the 5 global stats with their projected yearly change,
    /// the current year, and the advance-turn button.
    /// </summary>
    public class ResourceBar : MonoBehaviour
    {
        public const float BarHeight = 56f;

        private const float CellWidth = 198f;
        private const float CellGap = 6f;
        private const float Margin = 16f;

        private struct StatWidget
        {
            public Text Value;
            public Text Delta;
        }

        private StatWidget _energy, _money, _sustainability, _population, _happiness;
        private Text _turnLabel;

        public Button NextTurnButton { get; private set; }

        public void Build(Transform canvas)
        {
            RectTransform bar = UIFactory.Panel("ResourceBar", canvas, UIFactory.PanelColor);
            UIFactory.PlaceTopStrip(bar, BarHeight);

            float x = Margin;
            _energy = CreateStat(bar, "Energy", "ENERGY", x, UIFactory.AccentColor);
            x += CellWidth + CellGap;
            _money = CreateStat(bar, "Money", "MONEY", x, new Color(0.95f, 0.85f, 0.45f));
            x += CellWidth + CellGap;
            _sustainability = CreateStat(bar, "Sustainability", "SUSTAINABILITY", x, new Color(0.48f, 0.86f, 0.72f));
            x += CellWidth + CellGap;
            _population = CreateStat(bar, "Population", "POPULATION", x, new Color(0.62f, 0.78f, 0.96f));
            x += CellWidth + CellGap;
            _happiness = CreateStat(bar, "Happiness", "HAPPINESS", x, new Color(0.95f, 0.62f, 0.66f));

            BuildTurnControls(bar);
        }

        private static StatWidget CreateStat(Transform bar, string name, string caption, float x, Color accent)
        {
            RectTransform cell = UIFactory.NewRect(name, bar);
            UIFactory.Place(cell, UIFactory.TopLeft, x, -7f, CellWidth, 42f);

            UIFactory.Swatch(cell, 0f, -4f, 3f, 34f, accent);

            Text captionText = UIFactory.Label("Caption", cell, caption, 11, UIFactory.MutedColor);
            UIFactory.Place(captionText.rectTransform, UIFactory.TopLeft, 12f, -1f, CellWidth - 12f, 14f);

            Text value = UIFactory.Label("Value", cell, "0", 22, accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(value.rectTransform, UIFactory.TopLeft, 11f, -15f, 96f, 27f);

            // Right-aligned on the value's baseline so the number and its trend read as one unit.
            Text delta = UIFactory.Label("Delta", cell, "", 12, UIFactory.MutedColor, TextAnchor.LowerRight);
            UIFactory.Place(delta.rectTransform, UIFactory.TopLeft, CellWidth - 92f, -17f, 88f, 24f);

            return new StatWidget { Value = value, Delta = delta };
        }

        private void BuildTurnControls(RectTransform bar)
        {
            const float blockWidth = 300f;

            RectTransform block = UIFactory.NewRect("TurnBlock", bar);
            UIFactory.Place(block, UIFactory.TopRight, -Margin, -7f, blockWidth, 42f);

            _turnLabel = UIFactory.Label("Turn", block, "YEAR 0", 17, UIFactory.TextColor,
                TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Place(_turnLabel.rectTransform, UIFactory.TopLeft, 4f, -4f, 130f, 34f);

            NextTurnButton = UIFactory.Button("NextTurn", block, UIFactory.AccentColor);
            UIFactory.Place(NextTurnButton.GetComponent<RectTransform>(), UIFactory.TopRight, 0f, -4f, 150f, 34f);

            Text buttonLabel = UIFactory.Label("Label", NextTurnButton.transform, "NEXT YEAR  >", 14,
                new Color(0.04f, 0.11f, 0.06f), TextAnchor.MiddleCenter, FontStyle.Bold);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.offsetMin = Vector2.zero;
            buttonLabel.rectTransform.offsetMax = Vector2.zero;
        }

        public void SetResources(ResourceVector current, ResourceVector perTurn)
        {
            Apply(_energy, current.energy, perTurn.energy, "");
            Apply(_money, current.money, perTurn.money, "$");
            Apply(_sustainability, current.sustainability, perTurn.sustainability, "");
            Apply(_population, current.population, perTurn.population, "");
            Apply(_happiness, current.happiness, perTurn.happiness, "");
        }

        private static void Apply(StatWidget widget, float value, float delta, string prefix)
        {
            if (widget.Value == null) return;

            widget.Value.text = $"{prefix}{value:0}";

            if (Mathf.Abs(delta) < 0.01f)
            {
                widget.Delta.text = "no change";
                widget.Delta.color = UIFactory.FaintColor;
                return;
            }

            widget.Delta.text = $"{StatFormat.Signed(delta)} / year";
            widget.Delta.color = delta > 0f ? UIFactory.AccentColor : UIFactory.WarnColor;
        }

        public void SetTurn(int turn)
        {
            if (_turnLabel != null) _turnLabel.text = $"YEAR {turn}";
        }
    }
}
