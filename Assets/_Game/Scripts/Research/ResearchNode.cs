using System;
using System.Collections.Generic;
using Solarpunk.Core;
using UnityEngine;

namespace Solarpunk.Research
{
    /// <summary>
    /// One unlockable in the research tree (design doc §4, "Pesquisa").
    ///
    /// Data only, exactly like TileDefinition: the panel reads these to draw the
    /// tree, the manager reads them to price an unlock and to apply what it does.
    /// The tree itself is designed in the research tree artifact and lands here
    /// through docs/ResearchTree.json and ResearchTreeFactory.
    /// </summary>
    [CreateAssetMenu(menuName = "Solarpunk/Research Node", fileName = "NewResearchNode")]
    public class ResearchNode : ScriptableObject
    {
        public string displayName;

        [TextArea(2, 3)]
        [Tooltip("What the player gets for the points.")]
        public string description;

        [TextArea(2, 3)]
        [Tooltip("What it costs besides the points. Empty when it has no tradeoff.")]
        public string tradeoff;

        [Tooltip("Research points this node costs to unlock.")]
        public int cost;

        [Tooltip("Must be unlocked first. Empty = a root of the tree.")]
        public ResearchNode[] prerequisites = Array.Empty<ResearchNode>();

        [Tooltip("When set, any one prerequisite is enough instead of all of them.")]
        public bool requireAny;

        [Tooltip("Position in the tree view, in grid units around the hub. Negative y is up.")]
        public Vector2 position;

        [Tooltip("city, science, energy, industry or core. Decides how lines into this node are routed.")]
        public string branch;

        [Tooltip("What unlocking this actually does in the simulation.")]
        public List<ResearchEffect> effects = new();

        [Tooltip("Set when part or all of the node needs a system the game does not have yet. " +
                 "Shown on the card so an inert unlock is never a surprise.")]
        public string pendingNote;

        public bool IsPending => !string.IsNullOrEmpty(pendingNote);

        /// <summary>The name in the current language; see <see cref="Loc"/>.</summary>
        public string LocalizedName => Loc.Translated($"node.{name}", displayName);

        /// <summary>English only for now; see <see cref="Loc.Translated"/>.</summary>
        public string LocalizedDescription => description;
    }
}
