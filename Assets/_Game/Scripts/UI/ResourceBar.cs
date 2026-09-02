using Solarpunk.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Top bar: the 5 global stats with their projected per-turn change, the
    /// current year, and the advance-turn button.
    /// </summary>
    public class ResourceBar : MonoBehaviour
    {
        private const float BarHeight = 74f;

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
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -BarHeight);
            bar.offsetMax = Vector2.zero;

            UIFactory.HorizontalLayout(bar, 6, new RectOffset(14, 14, 8, 8));

            _energy = CreateStat(bar, "Energy", "ENERGY", UIFactory.AccentColor);
            _money = CreateStat(bar, "Money", "MONEY", new Color(0.95f, 0.85f, 0.45f));
            _sustainability = CreateStat(bar, "Sustainability", "SUSTAINABILITY", new Color(0.50f, 0.85f, 0.70f));
            _population = CreateStat(bar, "Population", "POPULATION", new Color(0.65f, 0.78f, 0.95f));
            _happiness = CreateStat(bar, "Happiness", "HAPPINESS", new Color(0.95f, 0.60f, 0.65f));

            // --- Year + next-turn control ---
            RectTransform turnBlock = UIFactory.Panel("TurnBlock", bar, UIFactory.PanelSoftColor);
            var turnElement = turnBlock.gameObject.AddComponent<LayoutElement>();
            turnElement.minWidth = 190f;
            turnElement.preferredWidth = 190f;
            turnElement.flexibleWidth = 0f;
            UIFactory.HorizontalLayout(turnBlock, 8, new RectOffset(10, 10, 6, 6));

            _turnLabel = UIFactory.Label("Turn", turnBlock, "YEAR 0", 15, UIFactory.TextColor,
                TextAnchor.MiddleLeft, FontStyle.Bold);

            NextTurnButton = UIFactory.Button("NextTurn", turnBlock, UIFactory.AccentColor);
            var buttonElement = NextTurnButton.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>();
            buttonElement.minWidth = 96f;
            buttonElement.preferredWidth = 96f;

            Text buttonLabel = UIFactory.Label("Label", NextTurnButton.transform, "NEXT YEAR ▶", 13,
                new Color(0.05f, 0.12f, 0.07f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(buttonLabel.rectTransform, 0f, 0f, 0f, 0f);
        }

        private static StatWidget CreateStat(Transform parent, string name, string caption, Color accent)
        {
            RectTransform block = UIFactory.Panel(name, parent, UIFactory.PanelSoftColor);
            UIFactory.VerticalLayout(block, 0, new RectOffset(12, 8, 7, 5));

            UIFactory.Label("Caption", block, caption, 10, UIFactory.MutedColor);
            var widget = new StatWidget
            {
                Value = UIFactory.Label("Value", block, "0", 21, accent, TextAnchor.MiddleLeft, FontStyle.Bold),
                Delta = UIFactory.Label("Delta", block, "", 11, UIFactory.MutedColor)
            };

            UIFactory.FixedHeight(widget.Value.rectTransform, 26f);
            UIFactory.FixedHeight(widget.Delta.rectTransform, 14f);
            return widget;
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
                widget.Delta.text = "—";
                widget.Delta.color = UIFactory.MutedColor;
                return;
            }

            widget.Delta.text = $"{(delta > 0f ? "+" : "")}{delta:0.#}/yr";
            widget.Delta.color = delta > 0f ? UIFactory.AccentColor : UIFactory.WarnColor;
        }

        public void SetTurn(int turn)
        {
            if (_turnLabel != null) _turnLabel.text = $"YEAR {turn}";
        }
    }
}
