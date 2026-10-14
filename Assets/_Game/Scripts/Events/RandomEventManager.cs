using System;
using System.Collections.Generic;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Grid;
using Solarpunk.Managers;
using UnityEngine;

namespace Solarpunk.Events
{
    /// <summary>
    /// Rolls one event per year (design doc: "A cada turno, evento aleatório dispara").
    ///
    /// An ordinary year has a <see cref="chancePerYear"/> chance of a card and every
    /// <see cref="guaranteedEveryYears"/>th year draws one regardless. The card is drawn
    /// by weight from the ones eligible this year, then held as <see cref="Pending"/>
    /// until the player answers it; the turn loop will not advance past an unanswered
    /// card. Nothing is applied until then, so the numbers on the card are the numbers
    /// the player gets.
    /// </summary>
    public class RandomEventManager : MonoBehaviour
    {
        /// <summary>Sustainability at or above this absorbs part of a resilient card's losses.</summary>
        public const float ResilientAbove = ResourceManager.SustainabilityMax * 0.7f;
        public const float ResilienceCut = 0.3f;
        public const float LowHappiness = 40f;

        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private HexGridManager gridManager;

        [SerializeField] private List<EventDefinition> deck = new();

        [Tooltip("Chance of an event in an ordinary year. 0.05 means one in twenty.")]
        [Range(0f, 1f)]
        [SerializeField] private float chancePerYear = 0.05f;

        [Tooltip("Every Nth year fires an event regardless of the roll. 0 disables the guarantee.")]
        [SerializeField] private int guaranteedEveryYears = 5;

        [Tooltip("Fixed seed for a reproducible run. 0 varies each time.")]
        [SerializeField] private int seed;

        /// <summary>The scene's manager, for <see cref="Solarpunk.Tiles.TileEconomy"/> to price discounts.</summary>
        public static RandomEventManager Instance { get; private set; }

        public IReadOnlyList<EventDefinition> Deck => deck;
        public float ChancePerYear => chancePerYear;
        public int GuaranteedEveryYears => guaranteedEveryYears;

        /// <summary>The card waiting for the player, or null.</summary>
        public EventDefinition Pending { get; private set; }

        public int PendingYear { get; private set; } = -1;

        /// <summary>Loc key of the flavour line drawn with the pending card, or null.</summary>
        public string PendingVariantKey { get; private set; }

        /// <summary>Tile asset name the pending card discounts, or null.</summary>
        public string PendingDiscountTile { get; private set; }

        public EventDefinition LastEvent { get; private set; }
        public int LastEventYear { get; private set; } = -1;
        public int FiredCount { get; private set; }
        public int WarUntilYear { get; private set; } = -1;
        public bool AtWar => _year <= WarUntilYear;

        /// <summary>Raised when a card is drawn and waits for an answer.</summary>
        public event Action<EventDefinition> OnEventFired;

        /// <summary>Raised once the player has answered the pending card.</summary>
        public event Action OnEventResolved;

        private readonly struct Discount
        {
            public readonly string Tile;
            public readonly float Multiplier;
            public readonly int UntilYear;

            public Discount(string tile, float multiplier, int untilYear)
            {
                Tile = tile;
                Multiplier = multiplier;
                UntilYear = untilYear;
            }
        }

        private readonly HashSet<string> _drawn = new();
        private readonly List<Discount> _discounts = new();
        private System.Random _rng;
        private int _year;

        private void Awake()
        {
            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Resets the roller. Awake calls this in play mode; call it directly in edit
        /// mode, where Awake does not run for a component created in code.
        /// </summary>
        public void Initialize()
        {
            _rng = seed == 0 ? new System.Random() : new System.Random(seed);
            Pending = null;
            LastEvent = null;
            LastEventYear = -1;
            FiredCount = 0;
            WarUntilYear = -1;
            _year = 0;
            _drawn.Clear();
            _discounts.Clear();
        }

        public bool IsGuaranteedYear(int year) =>
            guaranteedEveryYears > 0 && year > 0 && year % guaranteedEveryYears == 0;

        /// <summary>
        /// Resolves one year. Returns the card drawn, or null for a quiet year.
        /// Called by the turn loop with the year being resolved.
        /// </summary>
        public EventDefinition RollForYear(int year)
        {
            _rng ??= new System.Random();
            _year = year;

            if (Pending != null) return null;
            if (!IsGuaranteedYear(year) && _rng.NextDouble() >= chancePerYear) return null;

            EventDefinition picked = Draw();
            if (picked != null) Fire(picked, year);
            return picked;
        }

        /// <summary>Puts a specific card up, bypassing the roll. For the screenshot harness.</summary>
        public void Force(EventDefinition definition, int year)
        {
            _year = year;
            Fire(definition, year);
        }

        public bool IsEligible(EventDefinition card)
        {
            if (card == null || card.options.Count == 0) return false;
            if (card.once && _drawn.Contains(card.name)) return false;
            if (card.warOnly && !AtWar) return false;
            if (card.startsWarYears > 0 && AtWar) return false;
            if (resourceManager != null && resourceManager.Current.money < card.minMoney) return false;
            if (!string.IsNullOrEmpty(card.requiresTile) && !TileStands(card.requiresTile)) return false;
            return true;
        }

        private bool TileStands(string tileName) =>
            gridManager != null && gridManager.Cells.Values.Any(c => c.builtTile != null && c.builtTile.name == tileName);

        private float WeightOf(EventDefinition card)
        {
            bool unhappy = resourceManager != null && resourceManager.Current.happiness < LowHappiness;
            return unhappy && card.lowHappinessWeight > 0f ? card.lowHappinessWeight : card.weight;
        }

        private EventDefinition Draw()
        {
            List<EventDefinition> eligible = deck.Where(IsEligible).ToList();
            float total = eligible.Sum(WeightOf);
            if (total <= 0f) return null;

            double roll = _rng.NextDouble() * total;
            foreach (EventDefinition card in eligible)
            {
                roll -= WeightOf(card);
                if (roll < 0) return card;
            }
            return eligible[^1];
        }

        private void Fire(EventDefinition card, int year)
        {
            Pending = card;
            PendingYear = year;
            PendingVariantKey = card.variants > 0 ? $"event.{card.name}.v{_rng.Next(card.variants)}" : null;

            string[] tiles = string.IsNullOrEmpty(card.discountTiles) ? Array.Empty<string>() : card.discountTiles.Split('|');
            PendingDiscountTile = tiles.Length > 0 ? tiles[_rng.Next(tiles.Length)] : null;

            Debug.Log($"[Event] Year {year}: {card.displayName}");
            OnEventFired?.Invoke(card);
        }

        /// <summary>
        /// What an option actually does this year: halved after the card that protects
        /// against it, and with losses cut when sustainability is high on a resilient card.
        /// The card shows this, and <see cref="Resolve"/> applies exactly this.
        /// </summary>
        public ResourceVector EffectOf(EventDefinition card, EventDefinition.Option option)
        {
            ResourceVector effect = option.effect;

            if (IsHalved(card)) effect = effect * 0.5f;

            if (IsCushioned(card))
            {
                foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                {
                    float value = effect.Get(type);
                    if (value < 0f) effect = effect.With(type, value * (1f - ResilienceCut));
                }
            }

            return effect;
        }

        public bool IsHalved(EventDefinition card) =>
            !string.IsNullOrEmpty(card.halvedAfter) && _drawn.Contains(card.halvedAfter);

        public bool IsCushioned(EventDefinition card) =>
            card.resilient && resourceManager != null && resourceManager.Current.sustainability >= ResilientAbove;

        /// <summary>Applies the player's answer to the pending card and clears it.</summary>
        public void Resolve(int optionIndex)
        {
            EventDefinition card = Pending;
            if (card == null) return;

            EventDefinition.Option option = card.options[Mathf.Clamp(optionIndex, 0, card.options.Count - 1)];
            ResourceVector effect = EffectOf(card, option);

            if (card.startsWarYears > 0) WarUntilYear = PendingYear + card.startsWarYears;

            if (PendingDiscountTile != null && card.discount > 0f)
            {
                int until = card.discountYears > 0 ? PendingYear + card.discountYears : int.MaxValue;
                _discounts.Add(new Discount(PendingDiscountTile, 1f - card.discount, until));
            }

            for (int i = 0; i < option.territory && gridManager != null; i++) gridManager.AddTerritory(_rng);

            _drawn.Add(card.name);
            LastEvent = card;
            LastEventYear = PendingYear;
            FiredCount++;
            Pending = null;

            // Applied last: it raises the HUD refresh, which should see the discount too.
            if (resourceManager != null) resourceManager.ApplyInstant(effect);

            OnEventResolved?.Invoke();
        }

        /// <summary>Build cost multiplier from every event discount still running on this tile.</summary>
        public float CostMultiplier(string tileName)
        {
            float multiplier = 1f;
            foreach (Discount d in _discounts)
            {
                if (d.Tile == tileName && _year <= d.UntilYear) multiplier *= d.Multiplier;
            }
            return multiplier;
        }
    }
}
