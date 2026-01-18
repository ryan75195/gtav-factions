using System;
using System.Collections.Generic;
using FactionWars.Economy.Interfaces;
using FactionWars.Economy.Models;

namespace FactionWars.Economy.Services
{
    /// <summary>
    /// Manages resource storage with capacity limits (caps) for each resource type.
    /// Provides methods to add, remove, and query resources while respecting storage caps.
    /// </summary>
    public class ResourceStorage : IResourceStorage
    {
        private readonly Dictionary<ResourceType, int> _amounts;
        private readonly Dictionary<ResourceType, int> _caps;

        /// <summary>
        /// Creates a new ResourceStorage with default caps from ResourceTypeInfo.
        /// </summary>
        public ResourceStorage()
        {
            _amounts = new Dictionary<ResourceType, int>
            {
                { ResourceType.Cash, 0 },
                { ResourceType.Recruitment, 0 },
                { ResourceType.Weapons, 0 }
            };

            _caps = new Dictionary<ResourceType, int>
            {
                { ResourceType.Cash, ResourceTypeInfo.GetInfo(ResourceType.Cash).DefaultCap },
                { ResourceType.Recruitment, ResourceTypeInfo.GetInfo(ResourceType.Recruitment).DefaultCap },
                { ResourceType.Weapons, ResourceTypeInfo.GetInfo(ResourceType.Weapons).DefaultCap }
            };
        }

        /// <summary>
        /// Creates a new ResourceStorage with custom caps for each resource type.
        /// </summary>
        /// <param name="cashCap">Maximum cash storage (must be positive).</param>
        /// <param name="recruitmentCap">Maximum recruitment points storage (must be positive).</param>
        /// <param name="weaponsCap">Maximum weapons storage (must be positive).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if any cap is not positive.</exception>
        public ResourceStorage(int cashCap, int recruitmentCap, int weaponsCap)
        {
            if (cashCap <= 0)
                throw new ArgumentOutOfRangeException(nameof(cashCap), "Cap must be positive.");
            if (recruitmentCap <= 0)
                throw new ArgumentOutOfRangeException(nameof(recruitmentCap), "Cap must be positive.");
            if (weaponsCap <= 0)
                throw new ArgumentOutOfRangeException(nameof(weaponsCap), "Cap must be positive.");

            _amounts = new Dictionary<ResourceType, int>
            {
                { ResourceType.Cash, 0 },
                { ResourceType.Recruitment, 0 },
                { ResourceType.Weapons, 0 }
            };

            _caps = new Dictionary<ResourceType, int>
            {
                { ResourceType.Cash, cashCap },
                { ResourceType.Recruitment, recruitmentCap },
                { ResourceType.Weapons, weaponsCap }
            };
        }

        /// <inheritdoc />
        public int GetAmount(ResourceType resourceType)
        {
            return _amounts[resourceType];
        }

        /// <inheritdoc />
        public int GetCap(ResourceType resourceType)
        {
            return _caps[resourceType];
        }

        /// <inheritdoc />
        public int GetRemainingCapacity(ResourceType resourceType)
        {
            return _caps[resourceType] - _amounts[resourceType];
        }

        /// <inheritdoc />
        public float GetFillPercentage(ResourceType resourceType)
        {
            int cap = _caps[resourceType];
            if (cap == 0) return 0f;
            return (_amounts[resourceType] / (float)cap) * 100f;
        }

        /// <inheritdoc />
        public bool IsAtCap(ResourceType resourceType)
        {
            return _amounts[resourceType] >= _caps[resourceType];
        }

        /// <inheritdoc />
        public bool HasAmount(ResourceType resourceType, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            return _amounts[resourceType] >= amount;
        }

        /// <inheritdoc />
        public int Add(ResourceType resourceType, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            if (amount == 0)
                return 0;

            int currentAmount = _amounts[resourceType];
            int cap = _caps[resourceType];
            int remainingCapacity = cap - currentAmount;

            int actualAdd = Math.Min(amount, remainingCapacity);
            _amounts[resourceType] = currentAmount + actualAdd;

            return actualAdd;
        }

        /// <inheritdoc />
        public bool Remove(ResourceType resourceType, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            if (amount == 0)
                return true;

            int currentAmount = _amounts[resourceType];
            if (currentAmount < amount)
                return false;

            _amounts[resourceType] = currentAmount - amount;
            return true;
        }

        /// <inheritdoc />
        public void Set(ResourceType resourceType, int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");

            int cap = _caps[resourceType];
            _amounts[resourceType] = Math.Min(amount, cap);
        }

        /// <inheritdoc />
        public void SetCap(ResourceType resourceType, int cap)
        {
            if (cap <= 0)
                throw new ArgumentOutOfRangeException(nameof(cap), "Cap must be positive.");

            _caps[resourceType] = cap;

            // Clamp current amount if it exceeds new cap
            if (_amounts[resourceType] > cap)
            {
                _amounts[resourceType] = cap;
            }
        }

        /// <inheritdoc />
        public void ModifyCap(ResourceType resourceType, float multiplier)
        {
            if (multiplier <= 0)
                throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be positive.");

            int currentCap = _caps[resourceType];
            int newCap = (int)(currentCap * multiplier);

            if (newCap <= 0)
                throw new ArgumentOutOfRangeException(nameof(multiplier),
                    "Resulting cap must be at least 1.");

            SetCap(resourceType, newCap);
        }

        /// <inheritdoc />
        public void Clear(ResourceType resourceType)
        {
            _amounts[resourceType] = 0;
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            _amounts[ResourceType.Cash] = 0;
            _amounts[ResourceType.Recruitment] = 0;
            _amounts[ResourceType.Weapons] = 0;
        }
    }
}
