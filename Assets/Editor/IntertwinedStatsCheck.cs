using Solarpunk.Core;
using UnityEditor;
using UnityEngine;

namespace Solarpunk.EditorTools
{
    /// <summary>
    /// Checks the intertwined rules and the win score headless, until the test suite
    /// in PR #1 lands: -executeMethod Solarpunk.EditorTools.IntertwinedStatsCheck.Run
    /// </summary>
    public static class IntertwinedStatsCheck
    {
        [MenuItem("Solarpunk/Check Intertwined Stats")]
        public static void Run()
        {
            int failures = 0;
            void Check(string name, bool ok)
            {
                if (ok) return;
                failures++;
                Debug.LogError($"[IntertwinedStatsCheck] FAIL: {name}");
            }

            var healthy = new ResourceVector { energy = 20, money = 2000, sustainability = 1000, population = 10, happiness = 100 };
            var triad = new ResourceVector { energy = 2, money = 15, sustainability = -5, population = 5 };

            Check("healthy year untouched", IntertwinedStats.Apply(healthy, triad, 100).Equals(triad));

            Check("debt: -5% population",
                Mathf.Approximately(IntertwinedStats.Apply(healthy.With(ResourceType.Money, -1), triad, 1).population, 5 - 0.5f));

            Check("S 50: growth += (50-100)/2",
                Mathf.Approximately(IntertwinedStats.Apply(healthy.With(ResourceType.Sustainability, 50), triad, 1).population, 5 - 25));

            Check("H 20: growth floored at 0",
                IntertwinedStats.Apply(healthy.With(ResourceType.Happiness, 20), triad, 1).population == 0);
            Check("H 20: already-negative growth left alone",
                IntertwinedStats.Apply(healthy.With(ResourceType.Happiness, 20), triad.With(ResourceType.Population, -3), 1).population == -3);
            Check("H 5: floored, then -10 more",
                IntertwinedStats.Apply(healthy.With(ResourceType.Happiness, 5), triad, 1).population == -10);

            ResourceVector deficit = triad.With(ResourceType.Energy, -1);
            Check("energy deficit ignored before year 50", IntertwinedStats.Apply(healthy, deficit, 49).happiness == 0);
            Check("energy deficit -5 happiness from year 50", IntertwinedStats.Apply(healthy, deficit, 50).happiness == -5);

            // 2000 / 10 * 1000/1000 * 100/100 * 20 = 4000
            Check("win score of the starting stats", Mathf.Approximately(WinStats.Score(healthy), 4000));
            Check("win score with no population", WinStats.Score(healthy.With(ResourceType.Population, 0)) == 0);

            Debug.Log($"[IntertwinedStatsCheck] {(failures == 0 ? "all passed" : failures + " failed")}");
            if (Application.isBatchMode) EditorApplication.Exit(failures == 0 ? 0 : 1);
        }
    }
}
