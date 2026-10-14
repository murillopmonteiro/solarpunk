using System.Collections.Generic;
using Solarpunk.Core;
using Solarpunk.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// The yearly event, full screen: dimmed board, a big card with the title, the
    /// flavour line, and what each answer does as resource chips. The board cannot
    /// be clicked and the year cannot turn until the player answers it.
    /// </summary>
    public class EventCard : MonoBehaviour
    {
        private const float Width = 1080f;
        private const float Height = 680f;
        private const float ChipWidth = 132f;
        private const float ChipHeight = 54f;
        private const float ChipGap = 12f;

        private RandomEventManager _events;
        private RectTransform _root;

        public bool IsOpen => _root != null && _root.gameObject.activeSelf;

        public void Build(Transform canvas, RandomEventManager events)
        {
            _events = events;

            // Full-screen blocker as much as a dimmer, like the research overlay.
            _root = UIFactory.NewRect("EventOverlay", canvas);
            UIFactory.Fill(_root);
            _root.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.15f, 0.11f, 0.78f);
            _root.gameObject.SetActive(false);

            if (_events == null) return;
            _events.OnEventFired += Show;
            _events.OnEventResolved += Hide;

            // A language switch rebuilds the HUD while a card may be waiting.
            if (_events.Pending != null) Show(_events.Pending);
        }

        private void OnDestroy()
        {
            if (_events == null) return;
            _events.OnEventFired -= Show;
            _events.OnEventResolved -= Hide;
        }

        private void Hide() => _root.gameObject.SetActive(false);

        private void Show(EventDefinition card)
        {
            foreach (Transform child in _root) Destroy(child.gameObject);
            _root.gameObject.SetActive(true);

            RectTransform panel = UIFactory.Panel("EventPanel", _root);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, -20f);
            panel.sizeDelta = new Vector2(Width, Height);

            Color tone = Tone(card);
            RectTransform badge = UIFactory.HexBadge("Badge", panel, tone, 120f, 8f);
            UIFactory.Place(badge, new Vector2(0.5f, 1f), 0f, 58f, 120f, 105f);
            Text mark = UIFactory.Label("Mark", badge, card.IsChoice ? "?" : tone == UIFactory.WarnColor ? "!" : "+",
                64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(mark.rectTransform);

            Text caption = UIFactory.Label("Caption", panel, Loc.T("event.caption", _events.PendingYear), 18,
                UIFactory.FaintColor, TextAnchor.UpperCenter);
            UIFactory.Place(caption.rectTransform, new Vector2(0.5f, 1f), 0f, -62f, Width - 80f, 24f);

            Text title = UIFactory.Label("Title", panel, card.LocalizedTitle, 58, UIFactory.TextColor,
                TextAnchor.UpperCenter, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, new Vector2(0.5f, 1f), 0f, -90f, Width - 80f, 74f);

            string flavour = _events.PendingVariantKey != null ? Loc.T(_events.PendingVariantKey) : card.LocalizedText;
            Text quote = UIFactory.Label("Text", panel, $"“{flavour}”", 26, UIFactory.MutedColor, TextAnchor.UpperCenter);
            quote.horizontalOverflow = HorizontalWrapMode.Wrap;
            UIFactory.Place(quote.rectTransform, new Vector2(0.5f, 1f), 0f, -176f, Width - 160f, 76f);

            string notes = Notes(card);
            if (notes.Length > 0)
            {
                Text note = UIFactory.Label("Notes", panel, notes, 19, UIFactory.CostColor, TextAnchor.UpperCenter);
                UIFactory.Place(note.rectTransform, new Vector2(0.5f, 1f), 0f, -256f, Width - 80f, 26f);
            }

            float slotWidth = (Width - 80f - 30f * (card.options.Count - 1)) / card.options.Count;
            for (int i = 0; i < card.options.Count; i++)
            {
                float x = -Width / 2f + 40f + i * (slotWidth + 30f) + slotWidth / 2f;
                BuildOption(panel, card, i, x, slotWidth);
            }
        }

        /// <summary>One answer: its chips above, its button below.</summary>
        private void BuildOption(RectTransform panel, EventDefinition card, int index, float x, float width)
        {
            EventDefinition.Option option = card.options[index];
            bool choice = card.IsChoice;

            RectTransform slot = choice ? UIFactory.Card($"Slot{index}", panel, UIFactory.PaperDeep) : UIFactory.NewRect($"Slot{index}", panel);
            UIFactory.Place(slot, new Vector2(0.5f, 0f), x, 40f, width, 340f);

            ResourceVector effect = _events.EffectOf(card, option);
            var lines = new List<string>();
            if (option.territory > 0) lines.Add(Loc.T("event.territory", option.territory));

            float chipsTop = -40f;
            int rows = Chips(slot, effect, chipsTop, width - 40f);
            if (rows == 0 && lines.Count == 0) lines.Add(Loc.T("event.nothing"));

            if (lines.Count > 0)
            {
                Text extra = UIFactory.Label("Extra", slot, string.Join("   ", lines), 22, UIFactory.TextColor,
                    TextAnchor.UpperCenter, FontStyle.Bold);
                UIFactory.Place(extra.rectTransform, new Vector2(0.5f, 1f), 0f,
                    chipsTop - rows * (ChipHeight + ChipGap), width - 40f, 30f);
            }

            // The first answer is the green one; the alternative sits in plain paper.
            bool primary = !choice || index == 0;
            Button button = UIFactory.ChunkyButton($"EventOption{index}", slot,
                primary ? UIFactory.LeafLight : UIFactory.Paper, primary ? UIFactory.LeafDark : UIFactory.Slab,
                new Vector2(0.5f, 0f), 0f, 26f, Mathf.Min(width - 40f, 420f), 92f, 8f);
            int captured = index;
            button.onClick.AddListener(() => _events.Resolve(captured));

            Text text = UIFactory.Label("Label", button.transform, Loc.T(option.labelKey), 30,
                primary ? UIFactory.LeafText : UIFactory.TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(text.rectTransform);
        }

        /// <summary>Lays out one chip per resource the effect moves, centred, wrapping. Returns the row count.</summary>
        private static int Chips(RectTransform parent, ResourceVector effect, float top, float width)
        {
            var moved = new List<(UIFactory.Glyph glyph, Color color, float value)>();
            void Add(UIFactory.Glyph g, Color c, float v)
            {
                if (Mathf.Abs(v) > 0.01f) moved.Add((g, c, v));
            }

            Add(UIFactory.Glyph.Bolt, UIFactory.EnergyColor, effect.energy);
            Add(UIFactory.Glyph.Coin, UIFactory.MoneyColor, effect.money);
            Add(UIFactory.Glyph.Leaf, UIFactory.SustainColor, effect.sustainability);
            Add(UIFactory.Glyph.People, UIFactory.PopulationColor, effect.population);
            Add(UIFactory.Glyph.Heart, UIFactory.HappinessColor, effect.happiness);
            if (moved.Count == 0) return 0;

            int perRow = Mathf.Max(1, Mathf.FloorToInt((width + ChipGap) / (ChipWidth + ChipGap)));
            int rows = Mathf.CeilToInt(moved.Count / (float)perRow);

            for (int i = 0; i < moved.Count; i++)
            {
                int row = i / perRow;
                int inRow = Mathf.Min(perRow, moved.Count - row * perRow);
                int col = i % perRow;
                float rowWidth = inRow * ChipWidth + (inRow - 1) * ChipGap;
                float x = -rowWidth / 2f + col * (ChipWidth + ChipGap) + ChipWidth / 2f;

                var (glyph, color, value) = moved[i];
                RectTransform chip = UIFactory.Chip($"Chip{i}", parent, UIFactory.Paper);
                UIFactory.Place(chip, new Vector2(0.5f, 1f), x, top - row * (ChipHeight + ChipGap), ChipWidth, ChipHeight);
                UIFactory.AddSlab(chip.GetComponent<Image>(), value < 0f ? UIFactory.WarnColor : color, 4f);

                RectTransform icon = UIFactory.Icon("Icon", chip, glyph, color, 30f);
                UIFactory.Place(icon, UIFactory.TopLeft, 14f, -12f, 30f, 30f);

                Text number = UIFactory.Label("Value", chip, StatFormat.Signed(value), 28,
                    value < 0f ? UIFactory.WarnColor : UIFactory.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIFactory.Place(number.rectTransform, UIFactory.TopLeft, 50f, 0f, ChipWidth - 54f, ChipHeight);
            }

            return rows;
        }

        /// <summary>The parts of a card that are not a resource vector, in one line.</summary>
        private string Notes(EventDefinition card)
        {
            var notes = new List<string>();

            if (card.startsWarYears > 0) notes.Add(Loc.T("event.war", card.startsWarYears));

            string tile = _events.PendingDiscountTile;
            if (tile != null && card.discount > 0f)
            {
                string tileName = Loc.Asset($"tile.{tile}", tile);
                int percent = Mathf.RoundToInt(card.discount * 100f);
                notes.Add(card.discountYears > 0
                    ? Loc.T("event.discountFor", tileName, percent, card.discountYears)
                    : Loc.T("event.discount", tileName, percent));
            }

            if (_events.IsHalved(card)) notes.Add(Loc.T("event.halved"));
            if (_events.IsCushioned(card)) notes.Add(Loc.T("event.resilient"));

            return string.Join("   ·   ", notes);
        }

        /// <summary>Gold for a dilemma, red for a blow, green for a windfall.</summary>
        private static Color Tone(EventDefinition card)
        {
            if (card.IsChoice) return UIFactory.Sun;

            ResourceVector e = card.options[0].effect;
            int losses = 0, gains = 0;
            foreach (float v in new[] { e.energy, e.money, e.sustainability, e.population, e.happiness })
            {
                if (v < 0f) losses++;
                else if (v > 0f) gains++;
            }
            return losses > gains ? UIFactory.WarnColor : UIFactory.Leaf;
        }
    }
}
