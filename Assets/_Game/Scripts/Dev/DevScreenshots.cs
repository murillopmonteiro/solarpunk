using System;
using System.Collections;
using System.IO;
using System.Linq;
using Solarpunk.Grid;
using Solarpunk.Managers;
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

            HexCell open = gridManager.Cells.Values.FirstOrDefault(c => c.relief == TerrainRelief.Mutable);
            selectionController.Select(open);
            yield return Capture(outputDir, "02-hex-selected");

            TileDefinition city = buildController.Palette.FirstOrDefault(p => p.category == TileCategory.City);
            buildController.TryBuild(open, city);
            yield return Capture(outputDir, "03-city-built");

            // A waterfall hex shows the relief-restricted options in their enabled state.
            HexCell waterfall = gridManager.Cells.Values.FirstOrDefault(c => c.relief == TerrainRelief.Waterfall);
            selectionController.Select(waterfall);
            yield return Capture(outputDir, "04-waterfall-selected");

            TileDefinition hydro = buildController.Palette.FirstOrDefault(p => p.displayName == "Hidreletrica");
            buildController.TryBuild(waterfall, hydro);

            foreach (var pair in gridManager.Cells.Where(c => c.Value.IsEmpty).Take(3))
            {
                TileDefinition solar = buildController.Palette.FirstOrDefault(p => p.displayName == "Solar");
                buildController.TryBuild(pair.Value, solar);
            }

            for (int i = 0; i < 12; i++) turnManager.AdvanceTurn();
            yield return Capture(outputDir, "05-developed-board");

            Application.Quit();
        }

        private IEnumerator Capture(string dir, string label)
        {
            yield return new WaitForEndOfFrame();

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(dir, $"{label}.png"), texture.EncodeToPNG());
            Destroy(texture);

            yield return null;
        }
    }
}
