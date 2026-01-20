using System;
using System.Collections.Generic;
using FactionWars.Combat.Interfaces;
using FactionWars.Core.Interfaces;

namespace FactionWars.Combat.Services
{
    /// <summary>
    /// Service for calculating spawn positions for peds.
    /// Provides methods to calculate positions behind the player for natural spawning.
    /// </summary>
    public class SpawnPositionCalculator : ISpawnPositionCalculator
    {
        private readonly IGameBridge _gameBridge;

        /// <summary>
        /// Default minimum distance behind the player for natural spawns.
        /// </summary>
        private const float MinNaturalSpawnDistance = 20f;

        /// <summary>
        /// Default maximum distance behind the player for natural spawns.
        /// </summary>
        private const float MaxNaturalSpawnDistance = 30f;

        /// <summary>
        /// Horizontal spread distance for multiple spawns.
        /// </summary>
        private const float SpawnSpreadDistance = 3f;

        /// <summary>
        /// Creates a new SpawnPositionCalculator.
        /// </summary>
        /// <param name="gameBridge">The game bridge for getting player position and heading.</param>
        /// <exception cref="ArgumentNullException">Thrown if gameBridge is null.</exception>
        public SpawnPositionCalculator(IGameBridge gameBridge)
        {
            _gameBridge = gameBridge ?? throw new ArgumentNullException(nameof(gameBridge));
        }

        /// <inheritdoc />
        public Vector3 CalculateBehindPlayer(float distance)
        {
            var playerPosition = _gameBridge.GetPlayerPosition();
            var playerHeading = _gameBridge.GetPlayerHeading();

            // Convert heading to radians (GTA V uses degrees, 0 = north)
            // We need to calculate the opposite direction (behind the player)
            // In GTA V: 0 = North (+Y), 90 = East (+X), 180 = South (-Y), 270 = West (-X)
            var behindHeadingDegrees = playerHeading + 180f;
            var behindHeadingRadians = behindHeadingDegrees * (float)Math.PI / 180f;

            // Calculate offset using trigonometry
            // sin gives X offset, cos gives Y offset (adjusted for GTA V coordinate system)
            var offsetX = (float)Math.Sin(behindHeadingRadians) * distance;
            var offsetY = (float)Math.Cos(behindHeadingRadians) * distance;

            return new Vector3(
                playerPosition.X + offsetX,
                playerPosition.Y + offsetY,
                playerPosition.Z);
        }

        /// <inheritdoc />
        public Vector3 CalculateNaturalSpawnPosition()
        {
            // Use a distance in the natural range
            var distance = (MinNaturalSpawnDistance + MaxNaturalSpawnDistance) / 2f;
            return CalculateBehindPlayer(distance);
        }

        /// <inheritdoc />
        public IList<Vector3> CalculateNaturalSpawnPositions(int count)
        {
            var positions = new List<Vector3>();

            if (count <= 0)
            {
                return positions;
            }

            var playerPosition = _gameBridge.GetPlayerPosition();
            var playerHeading = _gameBridge.GetPlayerHeading();

            // Calculate the direction behind the player
            var behindHeadingDegrees = playerHeading + 180f;
            var behindHeadingRadians = behindHeadingDegrees * (float)Math.PI / 180f;

            // Calculate the perpendicular direction (for spreading peds out)
            var perpHeadingRadians = behindHeadingRadians + (float)Math.PI / 2f;

            // Calculate base spawn distance
            var baseDistance = (MinNaturalSpawnDistance + MaxNaturalSpawnDistance) / 2f;

            for (int i = 0; i < count; i++)
            {
                // Vary distance slightly for each ped
                var distance = baseDistance + (i % 3) * 2f;

                // Calculate spread offset (peds spread perpendicular to player facing direction)
                // Center the spread around 0
                var spreadIndex = i - (count - 1) / 2f;
                var spreadOffset = spreadIndex * SpawnSpreadDistance;

                // Calculate main offset (behind player)
                var mainOffsetX = (float)Math.Sin(behindHeadingRadians) * distance;
                var mainOffsetY = (float)Math.Cos(behindHeadingRadians) * distance;

                // Calculate perpendicular offset (spread)
                var spreadOffsetX = (float)Math.Sin(perpHeadingRadians) * spreadOffset;
                var spreadOffsetY = (float)Math.Cos(perpHeadingRadians) * spreadOffset;

                var position = new Vector3(
                    playerPosition.X + mainOffsetX + spreadOffsetX,
                    playerPosition.Y + mainOffsetY + spreadOffsetY,
                    playerPosition.Z);

                positions.Add(position);
            }

            return positions;
        }

        /// <inheritdoc />
        public IList<Vector3> CalculateSpreadPositions(Vector3 center, int count, float minRadius, float maxRadius)
        {
            var positions = new List<Vector3>();

            for (int i = 0; i < count; i++)
            {
                var angle = (2 * Math.PI * i) / Math.Max(count, 1);
                var distance = minRadius + (i % 3) * ((maxRadius - minRadius) / 2);

                positions.Add(new Vector3(
                    center.X + (float)(Math.Cos(angle) * distance),
                    center.Y + (float)(Math.Sin(angle) * distance),
                    center.Z));
            }

            return positions;
        }
    }
}
