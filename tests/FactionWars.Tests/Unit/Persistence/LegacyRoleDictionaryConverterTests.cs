using System.Collections.Generic;
using FactionWars.Core.Models;
using FactionWars.Persistence.Converters;
using Newtonsoft.Json;
using Xunit;

namespace FactionWars.Tests.Unit.Persistence
{
    public class LegacyRoleDictionaryConverterTests
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new LegacyRoleDictionaryConverter() }
        };

        [Fact]
        public void Read_LegacyTierNames_MapsToRoles()
        {
            var json = "{\"Basic\":3,\"Medium\":2,\"Heavy\":1,\"Elite\":4}";

            var result = JsonConvert.DeserializeObject<Dictionary<DefenderRole, int>>(json, Settings);

            Assert.Equal(3, result![DefenderRole.Grunt]);
            Assert.Equal(2, result[DefenderRole.Gunner]);
            Assert.Equal(1, result[DefenderRole.Rifleman]);
            Assert.Equal(4, result[DefenderRole.Rocketeer]);
        }

        [Fact]
        public void Read_NewRoleNames_RoundTrips()
        {
            var json = "{\"Grunt\":5,\"Rifleman\":2}";

            var result = JsonConvert.DeserializeObject<Dictionary<DefenderRole, int>>(json, Settings);

            Assert.Equal(5, result![DefenderRole.Grunt]);
            Assert.Equal(2, result[DefenderRole.Rifleman]);
        }

        [Fact]
        public void Write_EmitsNewRoleNames()
        {
            var dict = new Dictionary<DefenderRole, int> { { DefenderRole.Grunt, 1 } };

            var json = JsonConvert.SerializeObject(dict, Settings);

            Assert.Contains("\"Grunt\"", json);
            Assert.DoesNotContain("Basic", json);
        }
    }
}
