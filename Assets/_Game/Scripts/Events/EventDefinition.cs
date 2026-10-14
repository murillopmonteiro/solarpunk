using System;
using System.Collections.Generic;
using Solarpunk.Core;
using UnityEngine;

namespace Solarpunk.Events
{
    /// <summary>
    /// One yearly event card (design doc: "A cada turno, evento aleatório dispara").
    ///
    /// Data only, exactly like TileDefinition and ResearchNode: the manager decides
    /// whether a card may be drawn and applies the option the player picks, so a new
    /// card is an entry in <see cref="Solarpunk.EditorTools.EventDeckFactory"/>, not code.
    ///
    /// Text is keyed by asset name in <see cref="Loc"/>: event.&lt;name&gt;.title and
    /// .text, plus one key per option label.
    /// </summary>
    [CreateAssetMenu(menuName = "Solarpunk/Event Definition", fileName = "NewEvent")]
    public class EventDefinition : ScriptableObject
    {
        /// <summary>A choice on the card. A card with one option is a notice with an OK button.</summary>
        [Serializable]
        public class Option
        {
            [Tooltip("Loc key for the button.")]
            public string labelKey = "event.ok";

            public ResourceVector effect;

            [Tooltip("Hexes added to the island when this option is picked.")]
            public int territory;
        }

        [Tooltip("Stable English identifier; the fallback title when Loc has no entry.")]
        public string displayName;

        [TextArea(2, 3)]
        public string description;

        public List<Option> options = new();

        [Header("When it can be drawn")]
        [Tooltip("Relative chance among the cards that are eligible this year.")]
        public float weight = 1f;

        [Tooltip("Weight used instead while happiness is below 40. 0 keeps the normal weight.")]
        public float lowHappinessWeight;

        [Tooltip("Asset name of a tile that must stand on the board. Empty = no requirement.")]
        public string requiresTile;

        [Tooltip("Only drawn while the treasury holds at least this much.")]
        public float minMoney;

        [Tooltip("Only drawn while a war is on.")]
        public bool warOnly;

        [Tooltip("Never drawn twice in a run.")]
        public bool once;

        [Header("What it does besides the option's vector")]
        [Tooltip("Opens a war window of this many years. A war card is not drawn during a war.")]
        public int startsWarYears;

        [Tooltip("Asset name of a card that, once drawn, halves this one's effect.")]
        public string halvedAfter;

        [Tooltip("High sustainability absorbs 30% of this card's losses.")]
        public bool resilient;

        [Tooltip("Number of flavour lines (event.<name>.v0, v1…) one of which is drawn with the card.")]
        public int variants;

        [Tooltip("Tile asset names separated by '|'; one is drawn and gets cheaper. Empty = none.")]
        public string discountTiles;

        [Range(0f, 1f)] public float discount;

        [Tooltip("Years the discount lasts. 0 = permanent.")]
        public int discountYears;

        public bool IsChoice => options.Count > 1;

        public string LocalizedTitle => Loc.Asset($"event.{name}.title", displayName);
        public string LocalizedText => Loc.Asset($"event.{name}.text", description);
    }
}
