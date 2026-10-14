using System;
using System.Globalization;
using Solarpunk.Core;
using Solarpunk.Events;
using Solarpunk.Managers;
using Solarpunk.Research;
using UnityEngine;
using UnityEngine.UI;

namespace Solarpunk.UI
{
    /// <summary>
    /// Admin mode: a column of cheats for testing a run without playing it out.
    /// F1 toggles it. Every button goes through the same manager calls the game
    /// uses, so the HUD updates through its usual events.
    /// </summary>
    public class AdminPanel : MonoBehaviour
    {
        public const KeyCode ToggleKey = KeyCode.F1;

        private const float Width = 300f;
        private const float HeaderHeight = 58f;
        private const float RowHeight = 40f;
        private const float RowGap = 8f;
        private const float SidePad = 18f;

        /// <summary>Static so a language switch, which rebuilds the canvas, keeps the panel open.</summary>
        private static bool _open;

        private RectTransform _root;
        private Text _invincibleLabel;
        private ResourceManager _resources;

        public void Build(Transform canvas, ResourceManager resources, TurnManager turns,
            ResearchManager research, RandomEventManager events)
        {
            _resources = resources;

            _root = UIFactory.Panel("AdminPanel", canvas);

            Text title = UIFactory.Label("Title", _root, Loc.T("admin.title"), 20, UIFactory.WarnColor,
                TextAnchor.UpperLeft, FontStyle.Bold);
            UIFactory.Place(title.rectTransform, UIFactory.TopLeft, SidePad + 6f, -18f, Width - SidePad * 2f, 28f);

            int row = 0;
            Row(row++, Loc.T("admin.money"), () => resources.ApplyInstant(new ResourceVector { money = 1000f }));
            if (research != null)
            {
                Row(row++, Loc.T("admin.research"), () => research.AddPoints(500f));
                Row(row++, Loc.T("admin.unlockAll"), research.UnlockAll);
            }
            Row(row++, Loc.T("admin.refill"), () => resources.ApplyInstant(new ResourceVector
            {
                // Overshoots on purpose: ApplyInstant clamps both to their maximum.
                sustainability = ResourceManager.SustainabilityMax, happiness = ResourceManager.SustainabilityMax
            }));
            Row(row++, Loc.T("admin.skipYears"), () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    // A card drawn on the way would stop the year; take its first answer.
                    if (events != null && events.Pending != null) events.Resolve(0);
                    turns.AdvanceTurn();
                }
            });
            if (events != null && events.Deck.Count > 0)
            {
                Row(row++, Loc.T("admin.event"), () =>
                {
                    if (events.Pending == null)
                        events.Force(events.Deck[UnityEngine.Random.Range(0, events.Deck.Count)], turns.CurrentTurn);
                });
            }
            _invincibleLabel = Row(row++, "", () =>
            {
                resources.Invincible = !resources.Invincible;
                RefreshInvincible();
            });
            RefreshInvincible();
            SetStatRow(row++);

            UIFactory.Place(_root, UIFactory.TopLeft, 34f, -232f, Width,
                HeaderHeight + row * (RowHeight + RowGap) - RowGap + SidePad);
            _root.gameObject.SetActive(_open);
        }

        public void Toggle()
        {
            _open = !_open;
            _root.gameObject.SetActive(_open);
        }

        private void RefreshInvincible() =>
            _invincibleLabel.text = Loc.T(_resources.Invincible ? "admin.invincibleOn" : "admin.invincibleOff");

        // ---------------------------------------------------------------- set a stat exactly

        private static readonly (ResourceType type, string key)[] Stats =
        {
            (ResourceType.Energy, "res.energy"), (ResourceType.Money, "res.money"),
            (ResourceType.Sustainability, "res.sustain"), (ResourceType.Population, "res.people"),
            (ResourceType.Happiness, "res.happy")
        };

        /// <summary>Static for the same reason as <see cref="_open"/>: survives a language switch.</summary>
        private static int _statIndex;

        private Text _statLabel;
        private Text _statPlaceholder;
        private InputField _statField;

        /// <summary>
        /// [stat ▸] [number] [SET]. The stat button cycles through the five; the box
        /// shows the current value greyed out until something is typed. Enter sets too.
        /// </summary>
        private void SetStatRow(int index)
        {
            const float gap = 6f;
            const float statWidth = 108f;
            const float setWidth = 52f;
            float y = -(HeaderHeight + index * (RowHeight + RowGap));
            float fieldWidth = Width - SidePad * 2f - statWidth - setWidth - gap * 2f;

            Button cycle = UIFactory.Button("StatCycle", _root, UIFactory.PaperDeep);
            UIFactory.Place(cycle.GetComponent<RectTransform>(), UIFactory.TopLeft, SidePad, y, statWidth, RowHeight);
            _statLabel = UIFactory.Label("Label", cycle.transform, "", 14, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(_statLabel.rectTransform);
            cycle.onClick.AddListener(() =>
            {
                _statIndex = (_statIndex + 1) % Stats.Length;
                RefreshStat();
            });

            RectTransform box = UIFactory.Chip("StatValue", _root, UIFactory.Paper);
            UIFactory.Place(box, UIFactory.TopLeft, SidePad + statWidth + gap, y, fieldWidth, RowHeight);
            _statField = box.gameObject.AddComponent<InputField>();
            _statField.targetGraphic = box.GetComponent<Image>();
            _statField.contentType = InputField.ContentType.DecimalNumber;

            Text typed = UIFactory.Label("Text", box, "", 16, UIFactory.TextColor, TextAnchor.MiddleCenter,
                FontStyle.Bold);
            typed.supportRichText = false;
            UIFactory.Fill(typed.rectTransform);
            _statPlaceholder = UIFactory.Label("Placeholder", box, "", 16, UIFactory.FaintColor,
                TextAnchor.MiddleCenter);
            UIFactory.Fill(_statPlaceholder.rectTransform);
            _statField.textComponent = typed;
            _statField.placeholder = _statPlaceholder;
            _statField.onEndEdit.AddListener(_ =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ApplyStat();
            });

            Button set = UIFactory.Button("StatSet", _root, UIFactory.Sun);
            UIFactory.Place(set.GetComponent<RectTransform>(), UIFactory.TopLeft, Width - SidePad - setWidth, y,
                setWidth, RowHeight);
            Text setLabel = UIFactory.Label("Label", set.transform, Loc.T("admin.set"), 15, UIFactory.SunText,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(setLabel.rectTransform);
            set.onClick.AddListener(ApplyStat);

            _resources.OnResourcesChanged += OnResourcesChanged;
            RefreshStat();
        }

        private void OnDestroy()
        {
            if (_resources != null) _resources.OnResourcesChanged -= OnResourcesChanged;
        }

        private void OnResourcesChanged(ResourceVector _) => RefreshStat();

        /// <summary>Reads the box and puts the selected stat at that number. Blank or junk does nothing.</summary>
        public void ApplyStat()
        {
            // The box takes "1.5"; a Portuguese "1,5" is read the same way.
            string text = _statField.text.Replace(',', '.');
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)) return;

            _resources.SetStat(Stats[_statIndex].type, value);
            _statField.text = "";
        }

        private void RefreshStat()
        {
            if (_statLabel == null) return;
            _statLabel.text = Loc.T(Stats[_statIndex].key) + "  >";
            _statPlaceholder.text = _resources.Current.Get(Stats[_statIndex].type).ToString("0.#", CultureInfo.InvariantCulture);
        }

        private Text Row(int index, string label, Action onClick)
        {
            Button button = UIFactory.Button($"Admin{index}", _root, UIFactory.PaperDeep);
            UIFactory.Place(button.GetComponent<RectTransform>(), UIFactory.TopLeft, SidePad,
                -(HeaderHeight + index * (RowHeight + RowGap)), Width - SidePad * 2f, RowHeight);
            button.onClick.AddListener(() => onClick());

            Text text = UIFactory.Label("Label", button.transform, label, 16, UIFactory.TextColor,
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Fill(text.rectTransform);
            return text;
        }
    }
}
