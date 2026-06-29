namespace FactionWars.Telemetry.Services
{
    public sealed partial class TelemetryService
    {
        private string BuildPlayerPositionDetails()
        {
            if (_getPlayerPosition == null) return string.Empty;

            var position = _getPlayerPosition();
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                new PlayerPositionDetails(position.X, position.Y, position.Z));
        }

        // Death details: position plus what killed the player (killer_weapon / killer_handle).
        private string BuildPlayerDeathDetails()
        {
            if (_getPlayerPosition == null) return string.Empty;

            var position = _getPlayerPosition();
            var cause = _getPlayerDeathCause?.Invoke() ?? default;
            return Newtonsoft.Json.JsonConvert.SerializeObject(new PlayerDeathDetails(
                position.X, position.Y, position.Z, cause.WeaponName, cause.KillerHandle));
        }
    }
}
