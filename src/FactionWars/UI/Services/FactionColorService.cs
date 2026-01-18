using System;
using System.Collections.Generic;
using FactionWars.Factions.Models;
using FactionWars.UI.Interfaces;
using FactionWars.UI.Models;

namespace FactionWars.UI.Services
{
    /// <summary>
    /// Service for managing faction color assignments.
    /// Provides predefined colors for each faction type and conversion utilities.
    /// </summary>
    public class FactionColorService : IFactionColorService
    {
        /// <summary>
        /// Michael's faction color - a distinct blue representing calculated control.
        /// </summary>
        public static readonly FactionColor MichaelBlue = new FactionColor(64, 100, 255);

        /// <summary>
        /// Trevor's faction color - a fiery orange representing aggression.
        /// </summary>
        public static readonly FactionColor TrevorOrange = new FactionColor(255, 150, 0);

        /// <summary>
        /// Franklin's faction color - a vibrant green representing opportunity.
        /// </summary>
        public static readonly FactionColor FranklinGreen = new FactionColor(0, 200, 50);

        /// <summary>
        /// Neutral color - a gray for unowned or contested zones.
        /// </summary>
        public static readonly FactionColor NeutralGray = new FactionColor(180, 180, 180);

        private readonly Dictionary<FactionType, FactionColor> _factionColors;
        private readonly Dictionary<FactionType, BoundaryColor> _boundaryColors;

        public FactionColorService()
        {
            _factionColors = new Dictionary<FactionType, FactionColor>
            {
                { FactionType.Michael, MichaelBlue },
                { FactionType.Trevor, TrevorOrange },
                { FactionType.Franklin, FranklinGreen }
            };

            _boundaryColors = new Dictionary<FactionType, BoundaryColor>
            {
                { FactionType.Michael, BoundaryColor.Michael },
                { FactionType.Trevor, BoundaryColor.Trevor },
                { FactionType.Franklin, BoundaryColor.Franklin }
            };
        }

        public FactionColor GetColorForFactionType(FactionType factionType)
        {
            return _factionColors.TryGetValue(factionType, out var color) ? color : NeutralGray;
        }

        public BoundaryColor GetBoundaryColorForFactionType(FactionType factionType)
        {
            return _boundaryColors.TryGetValue(factionType, out var color) ? color : BoundaryColor.Neutral;
        }

        public FactionColor GetNeutralColor()
        {
            return NeutralGray;
        }

        public BoundaryColor GetNeutralBoundaryColor()
        {
            return BoundaryColor.Neutral;
        }

        public FactionColor GetFactionColorForBoundaryColor(BoundaryColor boundaryColor)
        {
            switch (boundaryColor)
            {
                case BoundaryColor.Michael:
                    return MichaelBlue;
                case BoundaryColor.Trevor:
                    return TrevorOrange;
                case BoundaryColor.Franklin:
                    return FranklinGreen;
                case BoundaryColor.Neutral:
                default:
                    return NeutralGray;
            }
        }

        public FactionColor GetColorWithAlpha(FactionType factionType, int alpha)
        {
            var baseColor = GetColorForFactionType(factionType);
            var clampedAlpha = Math.Max(0, Math.Min(255, alpha));
            return new FactionColor(baseColor.R, baseColor.G, baseColor.B, clampedAlpha);
        }

        public bool TryGetFactionTypeFromColor(FactionColor color, out FactionType factionType)
        {
            // Check exact matches against known faction colors
            foreach (var kvp in _factionColors)
            {
                if (kvp.Value.R == color.R && kvp.Value.G == color.G && kvp.Value.B == color.B)
                {
                    factionType = kvp.Key;
                    return true;
                }
            }

            factionType = default;
            return false;
        }
    }
}
