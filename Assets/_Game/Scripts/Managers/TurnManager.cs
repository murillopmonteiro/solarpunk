using System;
using System.Collections.Generic;
using Solarpunk.Core;
using Solarpunk.Events;
using Solarpunk.Grid;
using Solarpunk.Research;
using Solarpunk.Tiles;
using UnityEngine;

namespace Solarpunk.Managers
{
    /// <summary>
    /// Drives the turn loop. One turn = one in-game year (design doc §"Resolvido
    /// nessa rodada"). Match ends in victory at turn 300 if the player hasn't
    /// already lost.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public const int VictoryTurn = 300;

        /// <summary>Years of a tile's energy flow the stock can hold, before storage research.</summary>
        public const float BaseStorageYears = 3f;

        /// <summary>The cap never drops below the starting stock, so an empty board doesn't throw it away.</summary>
        public const float MinEnergyCap = 20f;

        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private CityGrowth cityGrowth;
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private RandomEventManager randomEvents;

        public int CurrentTurn { get; private set; }
        public event Action<int> OnTurnAdvanced;

        private void Awake() => resourceManager.EnergyCapSource = EnergyCap;

        /// <summary>
        /// The most energy the stock can hold: every tile's yearly energy, produced or
        /// consumed, times the years it may be stored (3, raised by storage research).
        /// A bigger grid gets a bigger buffer, and a balanced one still gets one.
        /// </summary>
        public float EnergyCap()
        {
            float cap = 0f;

            foreach (HexCell hex in gridManager.Cells.Values)
            {
                if (hex.builtTile == null) continue;

                float flow = Mathf.Abs(TileEconomy.PerTurn(hex, cityGrowth, researchManager).energy);
                float years = BaseStorageYears;
                if (researchManager != null)
                    years += researchManager.StorageYears(TileEconomy.Facts(hex.builtTile, hex.cityLevel, hex));
                cap += flow * years;
            }

            return Mathf.Max(MinEnergyCap, cap);
        }

        /// <summary>
        /// Levels summed across every city on the board. Research income is priced
        /// off this — it is the one thing city level currently buys.
        /// </summary>
        public int TotalCityLevels()
        {
            int total = 0;
            foreach (int level in CityLevels()) total += level;
            return total;
        }

        /// <summary>The level of every city on the board, for research priced per city.</summary>
        public List<int> CityLevels()
        {
            var levels = new List<int>();

            foreach (var pair in gridManager.Cells)
            {
                HexCell hex = pair.Value;
                if (hex.builtTile != null && hex.builtTile.category == TileCategory.City) levels.Add(hex.cityLevel);
            }

            return levels;
        }

        /// <summary>
        /// Sum of every built tile's per-turn effect, research included, plus the
        /// yearly costs research carries on its own. Also the HUD's preview.
        /// </summary>
        public ResourceVector CalculateTurnDelta()
        {
            ResourceVector delta = researchManager != null ? researchManager.PerYear() : ResourceVector.Zero;

            foreach (var pair in gridManager.Cells)
            {
                if (pair.Value.builtTile != null)
                    delta += TileEconomy.PerTurn(pair.Value, cityGrowth, researchManager);
            }

            // Energy Exports: whatever the year would push above the storage cap is sold.
            if (researchManager != null)
                delta += researchManager.Export(resourceManager.Current.energy + delta.energy - EnergyCap());

            return delta;
        }

        private readonly System.Random _rng = new();

        /// <summary>
        /// Ages every standing tile a year, then fires the periodic rules research hangs
        /// on those timers: Biomass felling its nearest forest, Rewilding seeding the
        /// next hex. Collected first and applied after, so a forest seeded this year is
        /// not aged or fired in the same pass.
        /// </summary>
        private void TickHexTimers()
        {
            var fells = new List<HexCell>();
            var spreads = new List<HexCell>();

            foreach (HexCell hex in gridManager.Cells.Values)
            {
                if (hex.builtTile == null) continue;
                hex.age++;
                if (researchManager == null) continue;

                TileFacts facts = TileEconomy.Facts(hex.builtTile, hex.cityLevel, hex);
                if (Fires(researchManager.Period(EffectKind.FellForest, facts), hex.age)) fells.Add(hex);
                if (Fires(researchManager.Period(EffectKind.SpreadForest, facts), hex.age)) spreads.Add(hex);
            }

            foreach (HexCell hex in fells) gridManager.FellNearestForest(hex);

            // A forest felled for fuel this year seeds nothing.
            foreach (HexCell hex in spreads)
            {
                if (hex.builtTile != null) gridManager.SpreadForest(hex, _rng);
            }
        }

        private static bool Fires(int period, int age) => period > 0 && age % period == 0;

        public void AdvanceTurn()
        {
            if (resourceManager.GameOver) return;

            // An event card is waiting for an answer; the year does not turn under it.
            if (randomEvents != null && randomEvents.Pending != null) return;

            // CurrentTurn is still last year's number until the increment below, so the
            // year being resolved is one ahead. The event roll needs the real year, or
            // "every 5th year" lands on 4, 9, 14.
            int year = CurrentTurn + 1;

            resourceManager.ApplyTurn(CalculateTurnDelta());

            // Priced on the levels the cities held all year, before this year's growth,
            // so the figure matches what the panel was previewing. Null only in a scene
            // generated before the research system; the HUD logs that case.
            if (researchManager != null) researchManager.ApplyTurn(CityLevels());

            // Rolled after the year's production so an event acts on the stats the
            // player is about to see. Null only in a scene generated before events.
            if (randomEvents != null) randomEvents.RollForYear(year);

            TickHexTimers();

            foreach (var pair in gridManager.Cells)
            {
                HexCell hex = pair.Value;
                if (hex.builtTile != null && hex.builtTile.category == TileCategory.City)
                {
                    cityGrowth.TryAutoTick(hex);
                }
            }

            CurrentTurn++;
            OnTurnAdvanced?.Invoke(CurrentTurn);

            if (CurrentTurn >= VictoryTurn) resourceManager.DeclareVictory();
        }
    }
}
