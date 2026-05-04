using System;
using FactionWars.Core.Models;
using FactionWars.Factions.Models;

namespace FactionWars.Factions.Services
{
    public partial class FactionService
    {
        public bool TransferZoneBetweenFactions(string zoneId, string sourceFactionId, string targetFactionId)
        {
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));
            if (sourceFactionId == null)
                throw new ArgumentNullException(nameof(sourceFactionId));
            if (targetFactionId == null)
                throw new ArgumentNullException(nameof(targetFactionId));

            var sourceState = _repository.GetState(sourceFactionId);
            var targetState = _repository.GetState(targetFactionId);

            if (sourceState == null || targetState == null)
                return false;

            if (!sourceState.OwnsZone(zoneId))
                return false;

            sourceState.RemoveZone(zoneId);
            targetState.AddZone(zoneId);

            _repository.SetState(sourceState);
            _repository.SetState(targetState);

            return true;
        }

        /// <inheritdoc />
        public bool AddWeapons(string factionId, int count)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            state.Weapons += count;
            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool AddRecruitmentPoints(string factionId, int amount)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            state.RecruitmentPoints += amount;
            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool AddReserveTroops(string factionId, DefenderTier tier, int count)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            state.AddReserveTroops(tier, count);
            _repository.SetState(state);
            return true;
        }
    }
}
