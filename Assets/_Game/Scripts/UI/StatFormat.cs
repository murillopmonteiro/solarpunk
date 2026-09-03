using System.Collections.Generic;
using Solarpunk.Core;
using UnityEngine;

namespace Solarpunk.UI
{
    /// <summary>Turns resource vectors into the short strings the HUD shows.</summary>
    public static class StatFormat
    {
        public static string Signed(float value) => $"{(value > 0f ? "+" : "")}{value:0.#}";

        /// <summary>e.g. "Energy +40   Sust +1   Money -5" — what a tile does per year.</summary>
        public static string EffectSummary(ResourceVector v)
        {
            var parts = new List<string>();

            if (Mathf.Abs(v.energy) > 0.01f) parts.Add($"Energy {Signed(v.energy)}");
            if (Mathf.Abs(v.sustainability) > 0.01f) parts.Add($"Sust {Signed(v.sustainability)}");
            if (Mathf.Abs(v.money) > 0.01f) parts.Add($"Money {Signed(v.money)}");
            if (Mathf.Abs(v.population) > 0.01f) parts.Add($"Pop {Signed(v.population)}");
            if (Mathf.Abs(v.happiness) > 0.01f) parts.Add($"Happy {Signed(v.happiness)}");

            return parts.Count == 0 ? "No yearly effect" : string.Join("   ", parts);
        }
    }
}
