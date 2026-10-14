using System;
using System.Collections;
using System.IO;
using System.Linq;
using Solarpunk.Core;
using Solarpunk.Events;
using Solarpunk.Grid;
using Solarpunk.Managers;
using Solarpunk.Research;
using Solarpunk.Tiles;
using UnityEngine;

namespace Solarpunk.Dev
{
    /// <summary>
    /// Development-only capture harness. Dormant unless the player is launched
    /// with "-autoshot &lt;outputDir&gt;", in which case it drives the game through
    /// its main UI states and writes a PNG of each, then quits. Lets the UI be
    /// reviewed without a human having to sit in front of the Editor.
    /// </summary>
    public class DevScreenshots : MonoBehaviour
    {
        [SerializeField] private HexGridManager gridManager;
        [SerializeField] private SelectionController selectionController;
        [SerializeField] private BuildController buildController;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private ResearchManager researchManager;
        [SerializeField] private RandomEventManager randomEvents;

        private IEnumerator Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.IndexOf(args, "-autoshot");
            if (flag < 0) yield break;

            string outputDir = flag + 1 < args.Length ? args[flag + 1] : ".";
            Directory.CreateDirectory(outputDir);

            // Unity Personal forces a splash screen; wait it out before capturing.
            yield return new WaitForSecondsRealtime(6f);

            yield return Capture(outputDir, "01-initial");

            // The island starts wooded: the one clearing is the only hex free to build on.
            HexCell clearing = NextOpenHex();
            selectionController.Select(clearing);
            yield return Capture(outputDir, "02-the-clearing");

            buildController.TryBuild(clearing, Tile(p => p.category == TileCategory.City));

            // What a forest yields, and what felling it costs.
            HexCell woods = NextForestHex();
            selectionController.Select(woods);
            yield return Capture(outputDir, "03-forest-selected");

            // Cleared, the hex offers everything — City included, so the single-city cap
            // is visibly gone — but coal is held back until a coal extraction exists.
            buildController.Demolish(woods);
            selectionController.Select(woods);
            yield return Capture(outputDir, "04-coal-gated");

            // Coal comes out of grass or rock, so the coast refuses the mine outright.
            selectionController.Select(HexOf(TerrainRelief.Coast));
            yield return Capture(outputDir, "05-coast-refuses-mine");

            // A mountain seam is the cheaper, richer place to dig — $400 against $500,
            // and +24 money against +20. Both figures are on the row.
            HexCell mountain = HexOf(TerrainRelief.Mountain);
            selectionController.Select(mountain);
            yield return Capture(outputDir, "06-mountain-bonus");

            buildController.TryBuild(mountain, Tile(p => p.producedFuel == FuelType.Coal));
            selectionController.Select(woods);
            yield return Capture(outputDir, "07-coal-unlocked");

            buildController.TryBuild(woods, Tile(p => p.displayName == "Carvao"));

            // The waterfall is the only hex hydro will take, and hydro is the only thing
            // that hex will take. Building it keeps the dam in the board capture.
            buildController.TryBuild(HexOf(TerrainRelief.Waterfall),
                Tile(p => p.displayName == "Hidreletrica"));

            // City + coal extraction + coal thermal is the reference triad from CLAUDE.md,
            // so the HUD's per-year figures here are worth reading against it — money runs
            // 4 above the triad because the mine sits on a mountain.
            // A card drawn on the way blocks the year until answered; take its first answer.
            for (int i = 0; i < 12; i++)
            {
                if (randomEvents.Pending != null) randomEvents.Resolve(0);
                turnManager.AdvanceTurn();
            }
            if (randomEvents.Pending != null) randomEvents.Resolve(0);
            yield return Capture(outputDir, "08-developed-board");

            // The event card, both shapes: a blow with one answer, then a dilemma with two.
            // Answered through the real buttons, so the capture proves the wiring too.
            randomEvents.Force(Card("Desastre"), turnManager.CurrentTurn);
            yield return Capture(outputDir, "08b-event-disaster");
            ClickHudButton("EventOption0");

            randomEvents.Force(Card("Textil"), turnManager.CurrentTurn);
            yield return Capture(outputDir, "08c-event-dilemma");
            ClickHudButton("EventOption1");

            // Buying land grows the island by one hex. Harness-only money, so the $200
            // price is affordable whatever the year's cards did to the treasury.
            FindFirstObjectByType<ResourceManager>().Refund(300f);
            randomEvents.Force(Card("OfertaTerritorio"), turnManager.CurrentTurn);
            int before = gridManager.Cells.Count;
            ClickHudButton("EventOption0");
            if (gridManager.Cells.Count != before + 1)
                Debug.LogError($"[Autoshot] Territory purchase left {gridManager.Cells.Count} hexes, expected {before + 1}.");
            yield return Capture(outputDir, "08d-territory-bought");

            // A few nodes bought outright (harness-only points), so the capture shows
            // researched cards, lit lines and what is affordable next.
            foreach (string id in new[] { "research", "cityDev", "storage", "thermo", "coalPow" })
            {
                ResearchNode node = researchManager.Tree.FirstOrDefault(n => n != null && n.name == id);
                if (node == null) { Debug.LogError($"[Autoshot] No research node {id}."); continue; }
                researchManager.AddPoints(node.cost);
                researchManager.TryUnlock(node);
            }

            ClickHudButton("Research");
            yield return Capture(outputDir, "09-research-tree");

            // A node with an effect, a tradeoff and a pending part, in the detail panel.
            ClickHudButton("Node_nuclear");
            yield return Capture(outputDir, "09b-research-node");

            // Switching language rebuilds the whole HUD mid-game. Flipped back after,
            // so the saved preference ends on the language the run started in.
            ClickHudButton("Language");
            yield return Capture(outputDir, "10-language-switched");

            // Node names are translated; the longer Portuguese ones have to fit the cards.
            ClickHudButton("Research");
            yield return Capture(outputDir, "11-research-switched");
            ClickHudButton("Close");
            ClickHudButton("Language");

            // Late game: the whole tree owned, so every tile and board rule is live.
            // Passes until nothing new unlocks, since prerequisites gate the order.
            for (bool progress = true; progress;)
            {
                progress = false;
                foreach (ResearchNode node in researchManager.Tree)
                {
                    if (node == null || researchManager.IsUnlocked(node)) continue;
                    researchManager.AddPoints(node.cost);
                    progress |= researchManager.TryUnlock(node);
                }
            }

            // Coastal Engineering: with nothing selected, the panel sells a coast hex.
            selectionController.Select(null);
            yield return Capture(outputDir, "12-territory-offer");
            int beforeBuy = gridManager.Cells.Count;
            ClickHudButton(Loc.T("build.buyTerritory"));
            if (gridManager.Cells.Count != beforeBuy + 1)
                Debug.LogError($"[Autoshot] Buying a coast hex left {gridManager.Cells.Count} hexes, expected {beforeBuy + 1}.");

            // Every tile open ground can take, at once: the longest the build list gets.
            HexCell open = NextOpenHex() ?? NextForestHex();
            if (open != null && !open.IsEmpty) buildController.Demolish(open);
            selectionController.Select(open);
            yield return Capture(outputDir, "13-late-palette");

            // Biomass fells its nearest forest at 20 years; Sustainable Biomass (owned) stops it,
            // so the forest count should only go up, by Rewilding at 25.
            buildController.TryBuild(open, Tile(p => p.name == "Biomassa"));

            // Rewilding needs room: free one open-ground hex beside a forest, through the neighbour graph.
            HexCell room = gridManager.Cells.Values
                .Where(c => c.builtTile != null && c.builtTile.category == TileCategory.Forest)
                .SelectMany(c => c.Neighbours)
                .FirstOrDefault(n => n.relief == TerrainRelief.Mutable && n.builtTile != null
                                     && n.builtTile.category != TileCategory.Forest);
            if (room != null) buildController.Demolish(room);

            int forests = gridManager.Cells.Values.Count(c => c.builtTile != null && c.builtTile.category == TileCategory.Forest);
            for (int i = 0; i < 25; i++)
            {
                if (randomEvents.Pending != null) randomEvents.Resolve(0);
                turnManager.AdvanceTurn();
            }
            if (randomEvents.Pending != null) randomEvents.Resolve(0);
            int forestsAfter = gridManager.Cells.Values.Count(c => c.builtTile != null && c.builtTile.category == TileCategory.Forest);
            Debug.Log($"[Autoshot] Forests {forests} -> {forestsAfter} over 25 years; " +
                      $"hex {open.coordinates} has {open.Neighbours.Count} neighbours.");
            if (room != null && forestsAfter <= forests)
                Debug.LogError($"[Autoshot] Rewilding had room at {room.coordinates} but no forest spread in 25 years.");
            selectionController.Select(null);
            yield return Capture(outputDir, "14-after-25-years");

            Application.Quit();
        }

        /// <summary>
        /// Presses a HUD button the way the player does. Driving the real button
        /// rather than the panel behind it means the capture proves the wiring, not
        /// just the layout.
        /// </summary>
        private void ClickHudButton(string buttonName)
        {
            UnityEngine.UI.Button button = FindFirstObjectByType<Canvas>()
                ?.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                .FirstOrDefault(b => b.name == buttonName);

            if (button != null) button.onClick.Invoke();
            else Debug.LogError($"[Autoshot] No {buttonName} button in the HUD.");
        }

        private EventDefinition Card(string assetName) =>
            randomEvents.Deck.FirstOrDefault(e => e != null && e.name == assetName);

        private TileDefinition Tile(Func<TileDefinition, bool> match) =>
            buildController.Palette.FirstOrDefault(p => p != null && match(p));

        /// <summary>First buildable open-ground hex, so the walkthrough never fights relief rules.</summary>
        private HexCell NextOpenHex() =>
            gridManager.Cells.Values.FirstOrDefault(c => c.IsEmpty && c.relief == TerrainRelief.Mutable);

        /// <summary>First free hex of a given relief, for inspecting terrain rules.</summary>
        private HexCell HexOf(TerrainRelief relief) =>
            gridManager.Cells.Values.FirstOrDefault(c => c.IsEmpty && c.relief == relief);

        /// <summary>First hex still under trees — what the walkthrough pays to clear.</summary>
        private HexCell NextForestHex() =>
            gridManager.Cells.Values.FirstOrDefault(
                c => c.builtTile != null && c.builtTile.category == TileCategory.Forest);

        private IEnumerator Capture(string dir, string label)
        {
            // A full frame first, then the end of the next one. End-of-frame alone
            // captured the research overlay one frame before it appeared, giving a
            // screenshot of a panel that was provably open.
            yield return null;
            yield return new WaitForEndOfFrame();

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, $"{label}.png"), texture.EncodeToPNG());
            Destroy(texture);

            yield return null;
        }
    }
}
