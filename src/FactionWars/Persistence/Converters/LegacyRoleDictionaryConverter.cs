using System;
using System.Collections.Generic;
using FactionWars.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FactionWars.Persistence.Converters
{
    /// <summary>
    /// Deserializes a role-keyed troop dictionary while tolerating legacy
    /// tier key names (Basic/Medium/Heavy/Elite) from saves written before the
    /// role rename. Serializes using canonical role names.
    /// </summary>
    public sealed class LegacyRoleDictionaryConverter : JsonConverter<Dictionary<DefenderRole, int>>
    {
        private static readonly Dictionary<string, DefenderRole> LegacyNames =
            new Dictionary<string, DefenderRole>(StringComparer.OrdinalIgnoreCase)
            {
                { "Basic", DefenderRole.Grunt },
                { "Medium", DefenderRole.Gunner },
                { "Heavy", DefenderRole.Rifleman },
                { "Elite", DefenderRole.Rocketeer }
            };

        public override Dictionary<DefenderRole, int> ReadJson(
            JsonReader reader,
            Type objectType,
            Dictionary<DefenderRole, int>? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var result = new Dictionary<DefenderRole, int>();
            var obj = JObject.Load(reader);
            foreach (var property in obj.Properties())
            {
                var role = ResolveRole(property.Name);
                result[role] = property.Value.Value<int>();
            }

            return result;
        }

        public override void WriteJson(
            JsonWriter writer,
            Dictionary<DefenderRole, int>? value,
            JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                writer.WriteValue(kvp.Value);
            }

            writer.WriteEndObject();
        }

        private static DefenderRole ResolveRole(string key)
        {
            if (LegacyNames.TryGetValue(key, out var legacy))
                return legacy;
            if (Enum.TryParse<DefenderRole>(key, ignoreCase: true, out var role))
                return role;
            if (int.TryParse(key, out var numeric) && Enum.IsDefined(typeof(DefenderRole), numeric))
                return (DefenderRole)numeric;

            throw new JsonSerializationException($"Unknown defender role key '{key}'.");
        }
    }
}
