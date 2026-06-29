using System.Collections.Generic;
using FactionWars.Combat.Models;

namespace FactionWars.UI.Models
{
    /// <summary>
    /// State machine for the bodyguard squad radial (weapon-wheel-style) menu. Holds which stance
    /// segment the pointer is on while the menu is open and yields the chosen stance on close.
    /// Pure and game-agnostic; the ScriptHookV layer feeds it pointer directions and renders it.
    /// </summary>
    public sealed class SquadRadialMenu
    {
        // Segment order, clockwise from the top. Index maps 1:1 to RadialSelector indices.
        private static readonly SquadStance[] SegmentStances =
        {
            SquadStance.Escort,
            SquadStance.HoldArea,
            SquadStance.SearchAndDestroy
        };

        private const float Deadzone = 0.25f;

        public bool IsOpen { get; private set; }

        public int SelectedIndex { get; private set; }

        /// <summary>The stance segments in clockwise order, for the renderer.</summary>
        public IReadOnlyList<SquadStance> Segments => SegmentStances;

        public SquadStance SelectedStance => SegmentStances[SelectedIndex];

        /// <summary>Opens the menu with the current stance pre-selected, so a tap-and-release with no
        /// direction leaves the stance unchanged.</summary>
        public void Open(SquadStance currentStance)
        {
            IsOpen = true;
            SelectedIndex = IndexOf(currentStance);
        }

        /// <summary>Updates the highlighted segment from a pointer direction. Directions inside the
        /// deadzone keep the previous selection, so a wavering pointer does not flicker.</summary>
        public void UpdatePointer(float dirX, float dirY)
        {
            if (!IsOpen)
            {
                return;
            }

            int index = RadialSelector.SelectIndex(SegmentStances.Length, dirX, dirY, Deadzone);
            if (index >= 0)
            {
                SelectedIndex = index;
            }
        }

        /// <summary>Closes the menu and returns the stance that was highlighted.</summary>
        public SquadStance Close()
        {
            IsOpen = false;
            return SegmentStances[SelectedIndex];
        }

        private static int IndexOf(SquadStance stance)
        {
            for (int i = 0; i < SegmentStances.Length; i++)
            {
                if (SegmentStances[i] == stance)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
