using FactionWars.Core.Models;
using FactionWars.Factions.Interfaces;
using FactionWars.Factions.Models;
using System;
using System.Collections.Generic;

namespace FactionWars.Factions.Services
{
    /// <summary>
    /// Service providing faction-related business logic and operations.
    /// </summary>
    public class FactionService : IFactionService
    {
        private readonly IFactionRepository _repository;

        /// <summary>
        /// Creates a new FactionService instance.
        /// </summary>
        /// <param name="repository">The faction repository.</param>
        /// <exception cref="ArgumentNullException">Thrown if repository is null.</exception>
        public FactionService(IFactionRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public Faction? GetFaction(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id cannot be empty or whitespace.", nameof(id));

            return _repository.GetById(id);
        }

        /// <inheritdoc />
        public IEnumerable<Faction> GetAllFactions()
        {
            return _repository.GetAll();
        }

        /// <inheritdoc />
        public IEnumerable<Faction> GetActiveFactions()
        {
            return _repository.GetActive();
        }

        /// <inheritdoc />
        public FactionState? GetFactionState(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            return _repository.GetState(factionId);
        }

        /// <inheritdoc />
        public bool ActivateFaction(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var faction = _repository.GetById(factionId);
            if (faction == null)
                return false;

            faction.IsActive = true;
            _repository.Update(faction);
            return true;
        }

        /// <inheritdoc />
        public bool DeactivateFaction(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var faction = _repository.GetById(factionId);
            if (faction == null)
                return false;

            faction.IsActive = false;
            _repository.Update(faction);
            return true;
        }

        /// <inheritdoc />
        public bool InitializeFactionState(string factionId, int initialCash, int initialTroops)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var faction = _repository.GetById(factionId);
            if (faction == null)
                return false;

            var state = new FactionState(factionId, initialCash, initialTroops);
            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool AddZoneToFaction(string factionId, string zoneId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            state.AddZone(zoneId);
            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool RemoveZoneFromFaction(string factionId, string zoneId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (zoneId == null)
                throw new ArgumentNullException(nameof(zoneId));

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            if (!state.RemoveZone(zoneId))
                return false;

            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool AddCash(string factionId, int amount)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            state.AddCash(amount);
            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool SpendCash(string factionId, int amount)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            if (!state.SpendCash(amount))
                return false;

            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool RecruitTroops(string factionId, int count)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            state.RecruitTroops(count);
            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public bool LoseTroops(string factionId, int count)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

            var state = _repository.GetState(factionId);
            if (state == null)
                return false;

            state.LoseTroops(count);
            _repository.SetState(state);
            return true;
        }

        /// <inheritdoc />
        public int GetMilitaryStrength(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var state = _repository.GetState(factionId);
            return state?.MilitaryStrength ?? 0;
        }

        /// <inheritdoc />
        public int GetZoneCount(string factionId)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var state = _repository.GetState(factionId);
            return state?.ZoneCount ?? 0;
        }

        /// <inheritdoc />
        public bool CanAfford(string factionId, int amount)
        {
            if (factionId == null)
                throw new ArgumentNullException(nameof(factionId));

            var state = _repository.GetState(factionId);
            return state?.CanAfford(amount) ?? false;
        }

        /// <inheritdoc />
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
